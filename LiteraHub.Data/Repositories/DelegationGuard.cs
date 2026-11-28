using LiteraHub.Core.Interfaces;
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
    public bool IsOwner { get; }
    private sealed class OwnerState { public int UserId { get; set; } public string PasswordHash { get; set; } = ""; }
    private sealed class LimitState { public int Failures { get; set; } public bool Locked { get; set; } }
    private DelegationGuard(DbConnection db, DbTransaction tx, int roleId, bool admin, bool owner, HashSet<string> keys)
    { _db = db; _tx = tx; RoleId = roleId; IsAdmin = admin; IsOwner = owner; _keys = keys; }

    public static async Task<DelegationGuard> LoadAsync(DbConnection db, DbTransaction tx, UserSession actor, RolePermission action, RolePermission view)
    {
        if (actor.StudentId.HasValue || !StudentAccountPolicy.IsValidIdentity(actor.RoleName, actor.StudentId) || StudentAccountPolicy.IsStudentRole(actor.RoleName))
            throw new RolePermissionDeniedException(action);
        // Hold locks until commit; permission changes and account reassignment cannot race this decision.
        var role = await db.QuerySingleOrDefaultAsync<RoleDefinition>("""
            SELECT r.RoleId, r.Name, r.IsSystem, r.IsActive
            FROM dbo.Users u WITH (UPDLOCK, HOLDLOCK)
            JOIN dbo.Roles r WITH (UPDLOCK, HOLDLOCK) ON r.RoleId = u.RoleId
            WHERE u.UserId = @UserId AND u.IsActive = 1 AND u.MustChangePassword = 0
              AND r.IsActive = 1 AND r.Name = @RoleName AND u.StudentId IS NULL AND r.Name NOT IN(N'Borrower',N'Student');
            """, new { actor.UserId, actor.RoleName }, tx);
        if (role is null) throw new RolePermissionDeniedException(action);
        var keys = (await ReadKeysAsync(db, tx, role.RoleId)).ToHashSet(StringComparer.Ordinal);
        if (!keys.Contains(action.ToString()) || !keys.Contains(view.ToString()))
            throw new RolePermissionDeniedException(action);
        bool owner = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Users WITH (UPDLOCK,HOLDLOCK) WHERE UserId=@UserId AND IsSystemOwner=1", new { actor.UserId }, tx) == 1;
        return new DelegationGuard(db, tx, role.RoleId,
            role.IsSystem && string.Equals(role.Name, RoleNames.Admin, StringComparison.OrdinalIgnoreCase), owner, keys);
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
        if ((StudentAccountPolicy.IsStudentRole(role.Name) || !StudentAccountPolicy.IsValidIdentity(role.Name, null)) && !roleMutation)
            throw new InvalidOperationException("Borrower is not a staff role.");
        if (IsOwner) return role;
        if ((!IsAdmin && role.IsSystem) || (roleMutation && role.RoleId == RoleId))
            throw new InvalidOperationException("You cannot manage a protected role or change your own role.");
        var keys = await ReadKeysAsync(_db, _tx, role.RoleId);
        if (keys.Any(k => !_keys.Contains(k)))
            throw new InvalidOperationException("You cannot manage a role with permissions beyond your own.");
        if (roleMutation && _keys.SetEquals(keys))
            throw new InvalidOperationException("Only the System Owner can modify roles with equivalent authority.");
        return role;
    }

    public async Task CheckAssignmentAsync(string name, bool allowEquivalent = false)
    {
        var role = await CheckRoleAsync(null, name, false, true);
        if (!IsOwner && !allowEquivalent && _keys.SetEquals(await ReadKeysAsync(_db, _tx, role.RoleId)))
            throw new InvalidOperationException("Only the System Owner can assign a role with equivalent authority without approval.");
    }

    public void CheckNewPermissions(IEnumerable<RolePermission> permissions)
    {
        if (permissions.Any(p => !Enum.IsDefined(p) || !_keys.Contains(p.ToString())))
            throw new InvalidOperationException("You cannot grant permissions beyond your own access.");
        if (!IsOwner && _keys.SetEquals(permissions.Select(p => p.ToString())))
            throw new InvalidOperationException("Only the System Owner can create or grant an equivalent permission set.");
    }

    public async Task<bool> CheckStaffAsync(int targetId, int actorId, string action, IPasswordHasher hasher, string? ownerPassword)
    {
        if (targetId == actorId) throw new InvalidOperationException("You cannot manage your own account here.");
        if (await _db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Users WITH (UPDLOCK, HOLDLOCK) WHERE UserId=@targetId AND IsSystemOwner=1", new { targetId }, _tx) > 0)
            throw new InvalidOperationException("The System Owner is protected. Only the owner can manage personal information through Profile & Settings.");
        var roleId = await _db.QuerySingleOrDefaultAsync<int?>("""
            SELECT RoleId FROM dbo.Users WITH (UPDLOCK, HOLDLOCK) WHERE UserId = @targetId AND StudentId IS NULL;
            """, new { targetId }, _tx);
        if (roleId is null) throw new InvalidOperationException("Staff account was not found.");
        await CheckRoleAsync(roleId, null, roleMutation: false);
        if (IsOwner) return false;
        var targetKeys = await ReadKeysAsync(_db, _tx, roleId.Value);
        if (!_keys.SetEquals(targetKeys)) return false;
        if (ownerPassword is null) throw new InvalidOperationException("System Owner authorization required.");
        var owner = await _db.QuerySingleOrDefaultAsync<OwnerState>("""
            SELECT u.UserId,u.PasswordHash FROM dbo.Users u WITH (UPDLOCK,HOLDLOCK)
            JOIN dbo.Roles r WITH (UPDLOCK,HOLDLOCK) ON r.RoleId=u.RoleId
            WHERE u.IsSystemOwner=1 AND u.IsActive=1 AND u.MustChangePassword=0 AND r.IsActive=1;
            """, transaction: _tx);
        if (owner is null) throw new InvalidOperationException("System Owner authorization is unavailable.");
        var limit = await _db.QuerySingleAsync<LimitState>("""
            SELECT Failures,CAST(CASE WHEN LockedUntil>SYSUTCDATETIME() THEN 1 ELSE 0 END AS BIT) AS Locked
            FROM dbo.OwnerAuthorizationState WITH (UPDLOCK,HOLDLOCK) WHERE SingletonId=1;
            """, transaction: _tx);
        if (limit.Locked)
        {
            await AuditAsync(actorId, targetId, owner.UserId, action, "Locked");
            await _tx.CommitAsync();
            throw new InvalidOperationException("Owner authorization is temporarily locked. Try again later.");
        }
        bool valid = ownerPassword.Length > 0 && System.Text.Encoding.UTF8.GetByteCount(ownerPassword) <= 72 && hasher.Verify(ownerPassword, owner.PasswordHash);
        if (!valid)
        {
            await _db.ExecuteAsync("""
                UPDATE dbo.OwnerAuthorizationState SET
                    Failures=CASE WHEN LockedUntil IS NOT NULL THEN 1 ELSE Failures+1 END,
                    LockedUntil=CASE WHEN LockedUntil IS NULL AND Failures+1>=5 THEN DATEADD(MINUTE,15,SYSUTCDATETIME()) ELSE NULL END
                WHERE SingletonId=1;
                """, transaction: _tx);
            await AuditAsync(actorId, targetId, owner.UserId, action, "Denied");
            // Commit only the failed attempt, before any requested mutation has happened.
            await _tx.CommitAsync();
            throw new InvalidOperationException("System Owner authorization was denied.");
        }
        await _db.ExecuteAsync("UPDATE dbo.OwnerAuthorizationState SET Failures=0,LockedUntil=NULL WHERE SingletonId=1", transaction: _tx);
        await AuditAsync(actorId, targetId, owner.UserId, action, "Approved");
        return true;
    }
    private Task<int> AuditAsync(int actorId, int targetId, int ownerId, string action, string outcome) =>
        _db.ExecuteAsync("INSERT dbo.OwnerAuthorizationAudit(ActorUserId,TargetUserId,OwnerUserId,RequestedAction,Outcome) VALUES(@actorId,@targetId,@ownerId,@action,@outcome)",
            new { actorId, targetId, ownerId, action, outcome }, _tx);
}
