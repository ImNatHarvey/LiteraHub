using System.Data;
using LiteraHub.Core.Security;
using Dapper;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;

namespace LiteraHub.Data.Repositories;

public sealed class UserRepository : IUserRepository
{
    private const string SelectSql = """
        SELECT u.UserId, u.Username, u.PasswordHash, u.RoleId, r.Name AS RoleName, u.StudentId, u.MustChangePassword,
               CAST(CASE WHEN u.IsActive = 1 AND r.IsActive = 1 THEN 1 ELSE 0 END AS BIT) AS IsActive
        FROM dbo.Users u
        INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
        """;

    private readonly IDbConnectionFactory _factory;

    public UserRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<UserAccount?> GetByUsernameAsync(string username)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        return await conn.QuerySingleOrDefaultAsync<UserAccount>(
            SelectSql + " WHERE u.Username = @Username",
            new { Username = username });
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
        var user = await conn.QuerySingleOrDefaultAsync<UserAccount>(
            SelectSql.Replace("FROM dbo.Users u", "FROM dbo.Users u WITH (UPDLOCK, HOLDLOCK)")
                + " WHERE u.Username = @Username", new { Username = username }, tx);
        if (user is null)
        {
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
            int remaining = confirmed && user.IsActive && !string.Equals(user.RoleName, "Borrower", StringComparison.OrdinalIgnoreCase)
                ? Math.Max(0, (int)Math.Ceiling((row.LoginLockedUntil!.Value - now).TotalSeconds)) : 0;
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
        }
        await tx.CommitAsync();
        return (valid ? user : null, 0);
    }

    public async Task<bool> UpdatePasswordAsync(int userId, string expectedHash, string passwordHash, bool mustChangePassword)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        return await conn.ExecuteAsync("""
            UPDATE u SET PasswordHash = @PasswordHash, MustChangePassword = @MustChangePassword,
                LoginFailures = 0, LoginLockedUntil = NULL
            FROM dbo.Users u JOIN dbo.Roles r ON r.RoleId = u.RoleId
            WHERE u.UserId = @UserId AND u.IsActive = 1 AND r.IsActive = 1
              AND u.PasswordHash COLLATE Latin1_General_100_BIN2 = @ExpectedHash
              AND (u.LoginLockedUntil IS NULL OR u.LoginLockedUntil <= SYSUTCDATETIME());
            """, new
        {
            PasswordHash = passwordHash,
            MustChangePassword = mustChangePassword,
            UserId = userId,
            ExpectedHash = expectedHash
        }) == 1;
    }
}