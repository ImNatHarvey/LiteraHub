using Dapper;
using System.Data;
using System.Data.Common;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;

namespace LiteraHub.Data.Repositories;

public sealed class RoleRepository : IRoleRepository
{
    private readonly IDbConnectionFactory _factory;
    public RoleRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<RoleDefinition>> GetAllAsync()
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        return (await conn.QueryAsync<RoleDefinition>("""
            SELECT RoleId, Name, Description, IsActive, IsSystem
            FROM dbo.Roles ORDER BY Name;
            """)).ToList();
    }

    public async Task<IReadOnlyList<string>> GetPermissionKeysAsync(int roleId)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        return (await conn.QueryAsync<string>("""
            SELECT PermissionKey FROM dbo.RolePermissions
            WHERE RoleId = @roleId ORDER BY PermissionKey;
            """, new { roleId })).ToList();
    }

    public IReadOnlyList<string> GetActiveUserPermissionKeys(int userId, string roleName)
    {
        using var conn = _factory.CreateConnection();
        conn.Open();
        return conn.Query<string>("""
            SELECT rp.PermissionKey
            FROM dbo.Users u
            JOIN dbo.Roles r ON r.RoleId = u.RoleId
            JOIN dbo.RolePermissions rp ON rp.RoleId = r.RoleId
            WHERE u.UserId = @userId AND u.IsActive = 1
              AND r.IsActive = 1 AND r.Name = @roleName;
            """, new { userId, roleName }).ToList();
    }
    public async Task<int> CreateAsync(RoleInput input)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync(IsolationLevel.Serializable);
        var id = await conn.ExecuteScalarAsync<int>("""
            INSERT INTO dbo.Roles (Name, Description, IsActive, IsSystem)
            OUTPUT INSERTED.RoleId VALUES (@Name, @Description, 1, 0);
            """, input, tx);
        await ReplacePermissionsAsync(conn, tx, id, input);
        await tx.CommitAsync();
        return id;
    }

    public async Task<bool> UpdateAsync(int roleId, RoleInput input)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync(IsolationLevel.Serializable);
        // System role updates may only change the description. Never replace their permissions.
        var systemUpdated = await conn.ExecuteAsync("""
            UPDATE dbo.Roles SET Description = @Description
            WHERE RoleId = @roleId AND IsSystem = 1 AND IsActive = 1;
            """, new { roleId, input.Description }, tx);
        if (systemUpdated == 1)
        {
            await tx.CommitAsync();
            return true;
        }
        var changed = await conn.ExecuteAsync("""
            UPDATE dbo.Roles SET Name = @Name, Description = @Description
            WHERE RoleId = @roleId AND IsSystem = 0;
            """, new { roleId, input.Name, input.Description }, tx);
        if (changed != 1) return false;
        await ReplacePermissionsAsync(conn, tx, roleId, input);
        await tx.CommitAsync();
        return true;
    }

    private static async Task ReplacePermissionsAsync(DbConnection conn, DbTransaction tx, int roleId, RoleInput input)
    {
        await conn.ExecuteAsync("DELETE FROM dbo.RolePermissions WHERE RoleId = @roleId;", new { roleId }, tx);
        foreach (var permission in input.Permissions.Distinct())
            await conn.ExecuteAsync("INSERT INTO dbo.RolePermissions (RoleId, PermissionKey) VALUES (@roleId, @key);",
                new { roleId, key = permission.ToString() }, tx);
    }

    public async Task<bool> SetActiveAsync(int roleId, bool active)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync(IsolationLevel.Serializable);
        var changed = await conn.ExecuteAsync("""
            UPDATE dbo.Roles SET IsActive = @active
            WHERE RoleId = @roleId AND IsSystem = 0 AND IsActive <> @active
            AND (@active = 1 OR NOT EXISTS (SELECT 1 FROM dbo.Users WHERE RoleId = @roleId AND IsActive = 1));
            """, new { roleId, active }, tx);
        await tx.CommitAsync();
        return changed == 1;
    }

    public async Task<bool> DeleteAsync(int roleId)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync(IsolationLevel.Serializable);
        var allowed = await conn.ExecuteScalarAsync<int>("""
            SELECT COUNT(*) FROM dbo.Roles WITH (UPDLOCK, HOLDLOCK)
            WHERE RoleId = @roleId AND IsSystem = 0 AND IsActive = 0
            AND NOT EXISTS (SELECT 1 FROM dbo.Users WHERE RoleId = @roleId);
            """, new { roleId }, tx);
        if (allowed != 1) return false;
        await conn.ExecuteAsync("DELETE FROM dbo.RolePermissions WHERE RoleId = @roleId;", new { roleId }, tx);
        var changed = await conn.ExecuteAsync("DELETE FROM dbo.Roles WHERE RoleId = @roleId AND IsSystem = 0 AND IsActive = 0;", new { roleId }, tx);
        if (changed != 1) return false;
        await tx.CommitAsync();
        return true;
    }
}
