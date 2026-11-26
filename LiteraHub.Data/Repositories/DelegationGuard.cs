using System.Data.Common;
using Dapper;
using LiteraHub.Core.Models;
using LiteraHub.Core.Security;

namespace LiteraHub.Data.Repositories;

/// <summary>Checks security mutations against current database grants inside their transaction.</summary>
internal sealed class DelegationGuard
{
    private readonly DbConnection _db;
    private readonly DbTransaction _tx;
    private readonly HashSet<string> _keys;
    public int RoleId { get; }
    public bool IsAdmin { get; }
    private DelegationGuard(DbConnection db, DbTransaction tx, int roleId, bool admin, HashSet<string> keys)
    { _db = db; _tx = tx; RoleId = roleId; IsAdmin = admin; _keys = keys; }

    public static async Task<DelegationGuard> LoadAsync(DbConnection db, DbTransaction tx, UserSession actor, RolePermission action, RolePermission view)
    {
        // Hold locks until commit; permission changes and account reassignment cannot race this decision.
        var role = await db.QuerySingleOrDefaultAsync<RoleDefinition>("""
            SELECT r.RoleId, r.Name, r.IsSystem, r.IsActive
            FROM dbo.Users u WITH (UPDLOCK, HOLDLOCK)
            JOIN dbo.Roles r WITH (UPDLOCK, HOLDLOCK) ON r.RoleId = u.RoleId
            WHERE u.UserId = @UserId AND u.IsActive = 1 AND u.MustChangePassword = 0
              AND r.IsActive = 1 AND r.Name = @RoleName;
            """, new { actor.UserId, actor.RoleName }, tx);
        if (role is null) throw new RolePermissionDeniedException(action);
        var keys = (await ReadKeysAsync(db, tx, role.RoleId)).ToHashSet(StringComparer.Ordinal);
        if (!keys.Contains(action.ToString()) || !keys.Contains(view.ToString()))
            throw new RolePermissionDeniedException(action);
        return new DelegationGuard(db, tx, role.RoleId,
            role.IsSystem && string.Equals(role.Name, RoleNames.Admin, StringComparison.OrdinalIgnoreCase), keys);
    }

    private static Task<IEnumerable<string>> ReadKeysAsync(DbConnection db, DbTransaction tx, int roleId) =>
        db.QueryAsync<string>("SELECT PermissionKey FROM dbo.RolePermissions WITH (UPDLOCK, HOLDLOCK) WHERE RoleId = @roleId;", new { roleId }, tx);

    public async Task<RoleDefinition> CheckRoleAsync(int? id, string? name, bool roleMutation, bool requireActive = false)
    {
        var role = await _db.QuerySingleOrDefaultAsync<RoleDefinition>("""
            SELECT RoleId, Name, IsSystem, IsActive
            FROM dbo.Roles WITH (UPDLOCK, HOLDLOCK)
            WHERE (@id IS NOT NULL AND RoleId = @id) OR (@name IS NOT NULL AND Name = @name);
            """, new { id, name }, _tx);
        if (role is null || (requireActive && !role.IsActive))
            throw new InvalidOperationException("The selected role is unavailable.");
        if (string.Equals(role.Name, RoleNames.Borrower, StringComparison.OrdinalIgnoreCase) && !roleMutation)
            throw new InvalidOperationException("Borrower is not a staff role.");
        if (IsAdmin) return role;
        if (role.IsSystem || (roleMutation && role.RoleId == RoleId))
            throw new InvalidOperationException("You cannot manage a protected role or change your own role.");
        var keys = await ReadKeysAsync(_db, _tx, role.RoleId);
        if (keys.Any(k => !_keys.Contains(k)))
            throw new InvalidOperationException("You cannot manage a role with permissions beyond your own.");
        return role;
    }

    public void CheckNewPermissions(IEnumerable<RolePermission> permissions)
    {
        if (permissions.Any(p => !Enum.IsDefined(p) || !_keys.Contains(p.ToString())))
            throw new InvalidOperationException("You cannot grant permissions beyond your own access.");
    }

    public async Task CheckStaffAsync(int targetId, int actorId)
    {
        if (targetId == actorId) throw new InvalidOperationException("You cannot manage your own account here.");
        if (await _db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Users WITH (UPDLOCK, HOLDLOCK) WHERE UserId=@targetId AND IsSystemOwner=1", new { targetId }, _tx) > 0)
            throw new InvalidOperationException("The System Owner is protected. Only the owner can manage personal information through Profile & Settings.");
        var roleId = await _db.QuerySingleOrDefaultAsync<int?>("""
            SELECT RoleId FROM dbo.Users WITH (UPDLOCK, HOLDLOCK) WHERE UserId = @targetId;
            """, new { targetId }, _tx);
        if (roleId is null) throw new InvalidOperationException("Staff account was not found.");
        await CheckRoleAsync(roleId, null, roleMutation: false);
    }
}
