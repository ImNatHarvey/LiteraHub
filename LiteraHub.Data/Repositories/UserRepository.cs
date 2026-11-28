using System.Data;
using LiteraHub.Core.Security;
using Dapper;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;

namespace LiteraHub.Data.Repositories;

public sealed class UserRepository : IUserRepository
{
    private static readonly string SelectSql = $$"""
        SELECT u.UserId, u.Username, u.PasswordHash, u.RoleId, r.Name AS RoleName, u.StudentId, u.MustChangePassword,
               u.SessionVersion, CAST(CASE WHEN {{AccountEligibility.Sql}} THEN 1 ELSE 0 END AS BIT) AS IsActive
        FROM dbo.Users u
        INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
        """;

    internal const string LoginIdentifierSql = "(u.Username = @Username OR (r.Name=N'Borrower' AND EXISTS (SELECT 1 FROM dbo.Students loginStudent WHERE loginStudent.StudentId=u.StudentId AND loginStudent.StudentNo=@Username)))";

    private readonly IDbConnectionFactory _factory;

    public UserRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<UserAccount?> GetByUsernameAsync(string username)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        var matches = (await conn.QueryAsync<UserAccount>(SelectSql + " WHERE " + LoginIdentifierSql, new { Username = username })).Take(2).ToList();
        return matches.Count == 1 ? matches[0] : null;
    }

    public async Task<UserAccount?> GetByIdAsync(int userId)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        return await conn.QuerySingleOrDefaultAsync<UserAccount>(
            SelectSql + " WHERE u.UserId = @UserId",
            new { UserId = userId });
    }

    private sealed class AttemptRow
    {
        public int LoginFailures { get; set; }
        public DateTime? LoginLockedUntil { get; set; }
        public DateTime NowUtc { get; set; }
    }

    public async Task<(UserAccount? User, int RetryAfterSeconds)> AuthenticateAsync(string username, string password,
        Func<string, string, bool> verify, string dummyHash)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync(IsolationLevel.Serializable);
        var matches = (await conn.QueryAsync<UserAccount>(
            SelectSql.Replace("FROM dbo.Users u", "FROM dbo.Users u WITH (UPDLOCK, HOLDLOCK)")
                + " WHERE " + LoginIdentifierSql, new { Username = username }, tx)).Take(2).ToList();
        var user = matches.Count == 1 ? matches[0] : null;
        if (user is null)
        {
            await AuditWriter.WriteAsync(conn, tx, null, new AuditEvent(AuditModule.Authentication, AuditAction.LoginRejected, null, AuditOutcome.Denied));
            await tx.CommitAsync();
            await Task.Run(() => verify(password, dummyHash));
            return (null, 0);
        }

        var row = await conn.QuerySingleAsync<AttemptRow>("""
            SELECT LoginFailures, LoginLockedUntil, SYSUTCDATETIME() AS NowUtc
            FROM dbo.Users WHERE UserId = @UserId;
            """, new { user.UserId }, tx);
        var state = new LoginAttemptState(row.LoginFailures, row.LoginLockedUntil);
        if (LoginAttemptPolicy.IsLocked(state, row.NowUtc))
        {
            // Reveal the countdown only to a caller who proves knowledge of the password.
            // No session is granted and the lockout is not changed.
            bool confirmed = await Task.Run(() => verify(password, user.PasswordHash));
            var now = await conn.ExecuteScalarAsync<DateTime>("SELECT SYSUTCDATETIME();", transaction: tx);
            int remaining = confirmed && user.IsActive && StudentAccountPolicy.IsValidIdentity(user.RoleName, user.StudentId)
                ? Math.Max(0, (int)Math.Ceiling((row.LoginLockedUntil!.Value - now).TotalSeconds)) : 0;
            await AuditWriter.WriteAsync(conn, tx, null, new AuditEvent(AuditModule.Authentication, AuditAction.LoginRejected, user.UserId, AuditOutcome.Denied));
            await tx.CommitAsync();
            return (null, remaining);
        }

        bool valid = await Task.Run(() => verify(password, user.PasswordHash));
        if (user.IsActive)
        {
            // The account row stays locked until this transaction commits.
            var now = await conn.ExecuteScalarAsync<DateTime>("SELECT SYSUTCDATETIME();", transaction: tx);
            var next = valid ? LoginAttemptPolicy.Clear : LoginAttemptPolicy.RegisterFailure(state, now);
            await conn.ExecuteAsync("""
                UPDATE dbo.Users SET LoginFailures = @Failures, LoginLockedUntil = @LockedUntilUtc
                WHERE UserId = @UserId;
                """, new { next.Failures, next.LockedUntilUtc, user.UserId }, tx);
            if (!valid && next.LockedUntilUtc.HasValue)
                await AuditWriter.WriteAsync(conn, tx, null, new AuditEvent(AuditModule.Authentication, AuditAction.LoginLocked, user.UserId, AuditOutcome.Denied));
        }
        bool eligible = valid && user.IsActive && StudentAccountPolicy.IsValidIdentity(user.RoleName, user.StudentId);
        UserSession? actor = eligible ? new(user.UserId, user.Username, user.RoleName, user.StudentId) { SessionVersion = user.SessionVersion } : null;
        await AuditWriter.WriteAsync(conn, tx, actor, new AuditEvent(AuditModule.Authentication,
            eligible ? AuditAction.CredentialsVerified : AuditAction.LoginRejected, user.UserId,
            eligible ? AuditOutcome.Success : AuditOutcome.Denied));
        await tx.CommitAsync();
        return (valid ? user : null, 0);
    }

    public async Task<bool> UpdatePasswordAsync(int userId, string expectedHash, string passwordHash, bool mustChangePassword, long expectedSessionVersion = 0)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync(IsolationLevel.Serializable);
        int affected = await conn.ExecuteAsync($$"""
            UPDATE u SET PasswordHash = @PasswordHash, MustChangePassword = @MustChangePassword,
                LoginFailures = 0, LoginLockedUntil = NULL
            FROM dbo.Users u JOIN dbo.Roles r ON r.RoleId = u.RoleId
            WHERE u.UserId = @UserId AND {{AccountEligibility.Sql}}
              AND u.SessionVersion = @expectedSessionVersion
              AND u.PasswordHash COLLATE Latin1_General_100_BIN2 = @ExpectedHash
              AND (u.LoginLockedUntil IS NULL OR u.LoginLockedUntil <= SYSUTCDATETIME());
            """, new
        {
            PasswordHash = passwordHash,
            MustChangePassword = mustChangePassword,
            UserId = userId,
            ExpectedHash = expectedHash,
            expectedSessionVersion
        }, tx);
        if (affected == 1)
        {
            var user = await conn.QuerySingleAsync<UserAccount>(SelectSql + " WHERE u.UserId = @UserId", new { UserId = userId }, tx);
            await AuditWriter.WriteAsync(conn, tx, new UserSession(user.UserId, user.Username, user.RoleName, user.StudentId),
                new AuditEvent(AuditModule.Authentication, AuditAction.PasswordChanged, userId));
        }
        await tx.CommitAsync();
        return affected == 1;
    }
}