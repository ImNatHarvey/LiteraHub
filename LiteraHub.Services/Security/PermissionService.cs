using System.Data.Common;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Security;

namespace LiteraHub.Services.Security;

public sealed class PermissionService : IPermissionService
{
    private readonly ISessionContext _session;
    private readonly IRoleRepository _roles;

    public PermissionService(ISessionContext session, IRoleRepository roles)
    {
        _session = session;
        _roles = roles;
    }

    private HashSet<string> LoadKeys()
    {
        var current = _session.Current;
        if (current is null) return new(StringComparer.Ordinal);
        try
        {
            return new(_roles.GetActiveUserPermissionKeys(current.UserId, current.RoleName),
                StringComparer.Ordinal);
        }
        catch (DbException ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            // Never fall back to a permissive matrix when the database is unavailable.
            return new(StringComparer.Ordinal);
        }
    }

    public bool Can(Permission permission) =>
        Enum.IsDefined(permission) && LoadKeys().Contains(permission.ToString());

    public void Demand(Permission permission)
    {
        if (!Can(permission)) throw new PermissionDeniedException(permission);
    }

    public IReadOnlyList<Permission> GrantedPermissions()
    {
        var keys = LoadKeys();
        return Enum.GetValues<Permission>().Where(p => keys.Contains(p.ToString())).ToList();
    }

    // Granular checks are available for the next batch, but do not imply legacy grants.
    public bool Can(RolePermission permission) =>
        Enum.IsDefined(permission) && LoadKeys().Contains(permission.ToString());

    public IReadOnlyList<RolePermission> GrantedRolePermissions()
    {
        var keys = LoadKeys();
        return Enum.GetValues<RolePermission>().Where(p => keys.Contains(p.ToString())).ToList();
    }
}
