using System.Data;
using Dapper;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.Core.Security;

namespace LiteraHub.Data.Repositories;

public sealed class ProfileRepository(IDbConnectionFactory factory, ISessionContext session) : IProfileRepository
{
    private UserSession? OwnActor(int userId)
    {
        var actor = session.Current;
        return actor is not null && actor.UserId == userId && StudentAccountPolicy.IsValidIdentity(actor.RoleName, actor.StudentId) ? actor : null;
    }
    private static DynamicParameters Parameters(UserSession actor, object? values = null)
    {
        var args = new DynamicParameters(values);
        args.Add("userId", actor.UserId); args.Add("sessionRole", actor.RoleName);
        args.Add("sessionVersion", actor.SessionVersion); args.Add("sessionStudentId", actor.StudentId, DbType.Int32);
        return args;
    }
    public async Task<ProfileInfo?> GetAsync(int userId)
    {
        var actor = OwnActor(userId); if (actor is null) return null;
        await using var conn = factory.CreateConnection(); await conn.OpenAsync();
        var info = await conn.QuerySingleOrDefaultAsync<ProfileInfo>($$"""
            SELECT u.UserId,u.Username,r.Name AS RoleName,u.CreatedAt,
                CASE WHEN r.Name=N'Borrower' THEN s.FirstName ELSE u.FirstName END AS FirstName,
                CASE WHEN r.Name=N'Borrower' THEN s.MiddleName ELSE u.MiddleName END AS MiddleName,
                CASE WHEN r.Name=N'Borrower' THEN s.LastName ELSE u.LastName END AS LastName,
                u.RecoveryPasskeyCreatedAt,CAST(CASE WHEN u.RecoveryPasskeyHash IS NULL THEN 0 ELSE 1 END AS BIT) AS HasRecoveryPasskey
            FROM dbo.Users u JOIN dbo.Roles r ON r.RoleId=u.RoleId LEFT JOIN dbo.Students s ON s.StudentId=u.StudentId
            WHERE {{AccountEligibility.Sql}} AND {{AccountEligibility.OwnSession}};
            """, Parameters(actor));
        return session.Current == actor ? info : null;
    }
    public Task<bool> ReplaceIdentityAsync(int userId, string expectedHash, string oldUsername, string username, string firstName, string? middleName, string lastName) =>
        MutateAsync(userId, expectedHash, "Username=@username,FirstName=@firstName,MiddleName=@middleName,LastName=@lastName", new { oldUsername, username, firstName, middleName, lastName }, AuditAction.Updated, true, true);
    public Task<bool> ReplaceNameAsync(int userId, string expectedHash, string firstName, string? middleName, string lastName) =>
        MutateAsync(userId, expectedHash, "FirstName=@firstName,MiddleName=@middleName,LastName=@lastName", new { firstName, middleName, lastName }, AuditAction.Updated, true);
    public Task<bool> ReplaceUsernameAsync(int userId, string expectedHash, string oldUsername, string newUsername) =>
        MutateAsync(userId, expectedHash, "Username=@newUsername", new { oldUsername, newUsername }, AuditAction.Updated, false, true);
    public Task<bool> ReplacePasswordAsync(int userId, string expectedHash, string newHash) =>
        MutateAsync(userId, expectedHash, "PasswordHash=@newHash,MustChangePassword=0,LoginFailures=0,LoginLockedUntil=NULL", new { newHash }, AuditAction.PasswordChanged);
    public Task<bool> ReplacePasskeyAsync(int userId, string expectedHash, string passkeyHash) =>
        MutateAsync(userId, expectedHash, "RecoveryPasskeyHash=@passkeyHash,RecoveryPasskeyCreatedAt=SYSUTCDATETIME(),RecoveryFailures=0,RecoveryLockedUntil=NULL", new { passkeyHash }, AuditAction.PasskeyRegenerated);

    // The SET clause and additional predicates are private, fixed strings, never user input.
    private async Task<bool> MutateAsync(int userId, string expectedHash, string setSql, object values, AuditAction action, bool staffOnly = false, bool compareUsername = false)
    {
        var actor = OwnActor(userId);
        if (actor is null || (staffOnly && StudentAccountPolicy.IsStudentRole(actor.RoleName))) return false;
        await using var conn = factory.CreateConnection(); await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync(IsolationLevel.Serializable);
        var args = Parameters(actor, values); args.Add("expectedHash", expectedHash);
        if (session.Current != actor) return false;
        string sql = $$"""
            UPDATE u SET {{setSql}}
            FROM dbo.Users u JOIN dbo.Roles r ON r.RoleId=u.RoleId
            WHERE {{AccountEligibility.Sql}} AND {{AccountEligibility.OwnSession}}
              AND u.PasswordHash COLLATE Latin1_General_100_BIN2=@expectedHash
            """;
        if (staffOnly) sql += " AND u.StudentId IS NULL AND r.Name<>N'Borrower'";
        if (compareUsername)
        {
            sql += " AND u.Username COLLATE Latin1_General_100_BIN2=@oldUsername";
            string parameter = staffOnly ? "@username" : "@newUsername";
            sql += $" AND ({parameter} NOT LIKE N'STU-%' OR {parameter}=u.Username)";
        }
        int changed = await conn.ExecuteAsync(sql, args, tx);
        if (changed != 1 || session.Current != actor) return false;
        if (action == AuditAction.PasskeyRegenerated)
            await conn.ExecuteAsync("INSERT dbo.RecoveryAudit(UserId,ActorUserId,EventType) VALUES (@userId,@userId,N'SelfPasskeyRegenerated')", new { userId }, tx);
        await AuditWriter.WriteAsync(conn, tx, actor, new(AuditModule.Profile, action, userId));
        await tx.CommitAsync(); return true;
    }
}
