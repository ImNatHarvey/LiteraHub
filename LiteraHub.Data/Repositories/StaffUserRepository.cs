using System.Data;
using Dapper;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.Core.Security;

namespace LiteraHub.Data.Repositories;

public sealed class StaffUserRepository : IStaffUserRepository
{
    private readonly IDbConnectionFactory _factory;
    private sealed class StaffState
    {
        public string RoleName { get; set; } = "";
        public bool IsActive { get; set; }
    }
    private const string ListSql = """
        SELECT u.UserId, u.Username, r.Name AS RoleName, u.IsActive,
               u.MustChangePassword, u.CreatedAt
        FROM dbo.Users u
        INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
        WHERE r.Name <> N'Borrower'
        """;

    public StaffUserRepository(IDbConnectionFactory factory) =>
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));

    public async Task<IReadOnlyList<StaffUser>> GetAllAsync()
    {
        await using var db = _factory.CreateConnection();
        await db.OpenAsync();
        return (await db.QueryAsync<StaffUser>(ListSql + " ORDER BY u.Username")).ToList();
    }

    public async Task<StaffUser?> GetByIdAsync(int userId)
    {
        await using var db = _factory.CreateConnection();
        await db.OpenAsync();
        return await db.QuerySingleOrDefaultAsync<StaffUser>(
            ListSql + " AND u.UserId = @UserId", new { UserId = userId });
    }

    public async Task<bool> UsernameExistsAsync(string username, int? exceptUserId = null)
    {
        await using var db = _factory.CreateConnection();
        await db.OpenAsync();
        return await db.ExecuteScalarAsync<int>("""
            SELECT COUNT(1) FROM dbo.Users
            WHERE Username = @Username AND (@ExceptUserId IS NULL OR UserId <> @ExceptUserId)
            """, new { Username = username, ExceptUserId = exceptUserId }) > 0;
    }

    public async Task<int> CreateAsync(StaffUserInput input, string passwordHash, UserSession actor)
    {
        await using var db = _factory.CreateConnection();
        await db.OpenAsync();
        await using var tx = await db.BeginTransactionAsync(IsolationLevel.Serializable);
        var guard = await DelegationGuard.LoadAsync(db, tx, actor, RolePermission.UsersAdd, RolePermission.UsersView);
        await guard.CheckRoleAsync(null, input.RoleName, roleMutation: false, requireActive: true);
        int id = await db.ExecuteScalarAsync<int>("""
            INSERT INTO dbo.Users (Username, PasswordHash, RoleId, MustChangePassword, IsActive)
            OUTPUT INSERTED.UserId
            SELECT @Username, @PasswordHash, RoleId, 1, 1
            FROM dbo.Roles WITH (UPDLOCK, HOLDLOCK)
            WHERE Name = @Role AND IsActive = 1 AND Name <> N'Borrower';
            """, new { input.Username, PasswordHash = passwordHash, Role = input.RoleName }, tx);
        await tx.CommitAsync();
        return id;
    }

    public Task<bool> UpdateAsync(int userId, StaffUserInput input, UserSession actor) =>
        ChangeAsync(userId, input, null, actor);

    public Task<bool> SetActiveAsync(int userId, bool isActive, UserSession actor) =>
        ChangeAsync(userId, null, isActive, actor);

    public async Task<bool> DeleteAsync(int userId, UserSession actor)
    {
        await using var db = _factory.CreateConnection();
        await db.OpenAsync();
        await using var tx = await db.BeginTransactionAsync(IsolationLevel.Serializable);
        var guard = await DelegationGuard.LoadAsync(db, tx, actor, RolePermission.UsersDelete, RolePermission.UsersView);
        await guard.CheckStaffAsync(userId, actor.UserId);
        // Only archived staff accounts are eligible; foreign keys protect referenced records.
        int deleted = await db.ExecuteAsync("""
            DELETE u FROM dbo.Users u JOIN dbo.Roles r ON r.RoleId = u.RoleId
            WHERE u.UserId = @UserId AND u.IsActive = 0 AND r.Name <> N'Borrower';
            """, new { UserId = userId }, tx);
        await tx.CommitAsync();
        return deleted == 1;
    }

    // Serializable transaction protects the last active Admin against simultaneous changes.
    private async Task<bool> ChangeAsync(int userId, StaffUserInput? input, bool? active, UserSession actor)
    {
        await using var db = _factory.CreateConnection();
        await db.OpenAsync();
        await using var tx = await db.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var guard = await DelegationGuard.LoadAsync(db, tx, actor, input is null ? RolePermission.UsersArchiveRestore : RolePermission.UsersEdit, RolePermission.UsersView);
            await guard.CheckStaffAsync(userId, actor.UserId);
            if (input is not null) await guard.CheckRoleAsync(null, input.RoleName, roleMutation: false, requireActive: true);
            var current = await db.QuerySingleOrDefaultAsync<StaffState>("""
                SELECT r.Name AS RoleName, u.IsActive
                FROM dbo.Users u WITH (UPDLOCK, HOLDLOCK)
                JOIN dbo.Roles r ON r.RoleId = u.RoleId
                WHERE u.UserId = @UserId AND r.Name <> N'Borrower'
                """, new { UserId = userId }, tx);
            if (current is null)
            {
                await tx.RollbackAsync();
                return false;
            }
            string newRole = input?.RoleName ?? current.RoleName;
            bool newActive = active ?? current.IsActive;
            if (input is not null || newActive)
            {
                int validRole = await db.ExecuteScalarAsync<int>("""
                    SELECT COUNT(1) FROM dbo.Roles WITH (UPDLOCK, HOLDLOCK)
                    WHERE Name = @Role AND IsActive = 1 AND Name <> N'Borrower';
                    """, new { Role = newRole }, tx);
                if (validRole == 0)
                    throw new InvalidOperationException("Select an active staff role or restore the assigned role first.");
            }
            if (current.RoleName == "Admin" && current.IsActive &&
                (newRole != "Admin" || !newActive))
            {
                int remaining = await db.ExecuteScalarAsync<int>("""
                    SELECT COUNT(1) FROM dbo.Users u WITH (UPDLOCK, HOLDLOCK)
                    JOIN dbo.Roles r ON r.RoleId = u.RoleId
                    WHERE u.IsActive = 1 AND r.Name = 'Admin' AND u.UserId <> @UserId
                    """, new { UserId = userId }, tx);
                if (remaining == 0)
                    throw new InvalidOperationException("The last active administrator cannot be deactivated or demoted.");
            }
            int updated = await db.ExecuteAsync("""
                UPDATE dbo.Users
                SET Username = COALESCE(@Username, Username),
                    RoleId = CASE WHEN @Role IS NULL THEN RoleId ELSE
                        (SELECT RoleId FROM dbo.Roles WHERE Name = @Role AND IsActive = 1 AND Name <> N'Borrower') END,
                    IsActive = @IsActive
                WHERE UserId = @UserId
                """, new { UserId = userId, Username = input?.Username, Role = input?.RoleName, IsActive = newActive }, tx);
            await tx.CommitAsync();
            return updated == 1;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
