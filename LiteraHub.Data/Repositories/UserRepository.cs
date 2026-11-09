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

    public async Task UpdatePasswordAsync(int userId, string passwordHash, bool mustChangePassword)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        await conn.ExecuteAsync(
            "UPDATE dbo.Users SET PasswordHash = @PasswordHash, MustChangePassword = @MustChangePassword WHERE UserId = @UserId",
            new { PasswordHash = passwordHash, MustChangePassword = mustChangePassword, UserId = userId });
    }
}