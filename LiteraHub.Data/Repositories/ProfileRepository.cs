using Dapper;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
namespace LiteraHub.Data.Repositories;

public sealed class ProfileRepository(IDbConnectionFactory factory) : IProfileRepository
{
    public async Task<ProfileInfo?> GetAsync(int userId)
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();
        return await conn.QuerySingleOrDefaultAsync<ProfileInfo>("""
            SELECT u.UserId, u.Username, r.Name AS RoleName, u.CreatedAt,
                   u.FirstName, u.MiddleName, u.LastName, u.RecoveryPasskeyCreatedAt,
                   CAST(CASE WHEN u.RecoveryPasskeyHash IS NULL THEN 0 ELSE 1 END AS BIT) AS HasRecoveryPasskey
            FROM dbo.Users u JOIN dbo.Roles r ON r.RoleId = u.RoleId
            WHERE u.UserId = @userId AND u.IsActive = 1 AND r.IsActive = 1 AND u.MustChangePassword = 0;
            """, new { userId });
    }
    public async Task<bool> ReplaceIdentityAsync(int userId, string expectedHash, string oldUsername, string username, string firstName, string? middleName, string lastName)
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();
        return await conn.ExecuteAsync("""
            UPDATE u SET Username = @username, FirstName = @firstName, MiddleName = @middleName, LastName = @lastName
            FROM dbo.Users u JOIN dbo.Roles r ON r.RoleId = u.RoleId
            WHERE u.UserId = @userId AND u.IsActive = 1 AND r.IsActive = 1
              AND u.MustChangePassword = 0 AND u.Username COLLATE Latin1_General_100_BIN2 = @oldUsername
              AND u.PasswordHash COLLATE Latin1_General_100_BIN2 = @expectedHash;
            """, new { userId, expectedHash, oldUsername, username, firstName, middleName, lastName }) == 1;
    }
    public async Task<bool> ReplaceNameAsync(int userId, string expectedHash, string firstName, string? middleName, string lastName)
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();
        return await conn.ExecuteAsync("""
            UPDATE u SET FirstName = @firstName, MiddleName = @middleName, LastName = @lastName
            FROM dbo.Users u JOIN dbo.Roles r ON r.RoleId = u.RoleId
            WHERE u.UserId = @userId AND u.IsActive = 1 AND r.IsActive = 1
              AND u.MustChangePassword = 0
              AND u.PasswordHash COLLATE Latin1_General_100_BIN2 = @expectedHash;
            """, new { userId, expectedHash, firstName, middleName, lastName }) == 1;
    }
    public async Task<bool> ReplaceUsernameAsync(int userId, string expectedHash, string oldUsername, string newUsername)
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();
        return await conn.ExecuteAsync("""
            UPDATE u SET Username = @newUsername
            FROM dbo.Users u JOIN dbo.Roles r ON r.RoleId = u.RoleId
            WHERE u.UserId = @userId AND u.IsActive = 1 AND r.IsActive = 1
              AND u.MustChangePassword = 0 AND u.Username COLLATE Latin1_General_100_BIN2 = @oldUsername
              AND u.PasswordHash COLLATE Latin1_General_100_BIN2 = @expectedHash;
            """, new { userId, expectedHash, oldUsername, newUsername }) == 1;
    }
    public async Task<bool> ReplacePasswordAsync(int userId, string expectedHash, string newHash)
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();
        return await conn.ExecuteAsync("""
            UPDATE u SET PasswordHash = @newHash, MustChangePassword = 0, LoginFailures = 0, LoginLockedUntil = NULL
            FROM dbo.Users u JOIN dbo.Roles r ON r.RoleId = u.RoleId
            WHERE u.UserId = @userId AND u.IsActive = 1 AND r.IsActive = 1
              AND u.MustChangePassword = 0
              AND u.PasswordHash COLLATE Latin1_General_100_BIN2 = @expectedHash;
            """, new { userId, expectedHash, newHash }) == 1;
    }
    public async Task<bool> ReplacePasskeyAsync(int userId, string expectedHash, string passkeyHash)
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        int changed = await conn.ExecuteAsync("""
            UPDATE u SET RecoveryPasskeyHash = @passkeyHash, RecoveryPasskeyCreatedAt = SYSUTCDATETIME(), RecoveryFailures = 0, RecoveryLockedUntil = NULL
            FROM dbo.Users u JOIN dbo.Roles r ON r.RoleId = u.RoleId
            WHERE u.UserId = @userId AND u.IsActive = 1 AND r.IsActive = 1
              AND u.MustChangePassword = 0
              AND u.PasswordHash COLLATE Latin1_General_100_BIN2 = @expectedHash;
            """, new { userId, expectedHash, passkeyHash }, tx);
        if (changed == 1) await conn.ExecuteAsync("INSERT dbo.RecoveryAudit(UserId,ActorUserId,EventType) VALUES (@userId,@userId,N'SelfPasskeyRegenerated')", new { userId }, tx);
        await tx.CommitAsync();
        return changed == 1;
    }
}
