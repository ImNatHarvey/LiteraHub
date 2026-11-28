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

    // Check the requested key and its View prerequisite using one database read.
    // Unknown values and inconsistent grants fail closed.
    public bool Can(RolePermission permission) =>
        IsGranted(permission, LoadKeys());

    public void Demand(RolePermission permission)
    {
        if (!Can(permission)) throw new RolePermissionDeniedException(permission);
    }

    public IReadOnlyList<RolePermission> GrantedRolePermissions()
    {
        var keys = LoadKeys();
        return Enum.GetValues<RolePermission>().Where(p => IsGranted(p, keys)).ToList();
    }

    private static bool IsGranted(RolePermission permission, HashSet<string> keys)
    {
        if (!Enum.IsDefined(permission) || !keys.Contains(permission.ToString()))
            return false;

        RolePermission? requiredView = permission switch
        {
            RolePermission.BooksView or RolePermission.BooksAdd or RolePermission.BooksEdit or
                RolePermission.BooksArchiveRestore or RolePermission.BooksDelete => RolePermission.BooksView,
            RolePermission.AuthorsView or RolePermission.AuthorsAdd or RolePermission.AuthorsEdit or
                RolePermission.AuthorsArchiveRestore or RolePermission.AuthorsDelete => RolePermission.AuthorsView,
            RolePermission.CategoriesView or RolePermission.CategoriesAdd or RolePermission.CategoriesEdit or
                RolePermission.CategoriesArchiveRestore or RolePermission.CategoriesDelete => RolePermission.CategoriesView,
            RolePermission.StudentsView or RolePermission.StudentsAdd or RolePermission.StudentsEdit or
                RolePermission.StudentsArchiveRestore or RolePermission.StudentsDelete or RolePermission.StudentsManageAccounts => RolePermission.StudentsView,
            RolePermission.UsersView or RolePermission.UsersAdd or RolePermission.UsersEdit or
                RolePermission.UsersArchiveRestore or RolePermission.UsersDelete => RolePermission.UsersView,
            RolePermission.RolesView or RolePermission.RolesAdd or RolePermission.RolesEdit or
                RolePermission.RolesArchiveRestore or RolePermission.RolesDelete => RolePermission.RolesView,
            RolePermission.BorrowReturnView or RolePermission.BorrowReturnBorrow or
                RolePermission.BorrowReturnReturn => RolePermission.BorrowReturnView,
            RolePermission.ReportsView or RolePermission.ReportsExport => RolePermission.ReportsView,
            RolePermission.ActivityLogView => RolePermission.ActivityLogView,
            _ => null
        };
        return requiredView.HasValue && keys.Contains(requiredView.Value.ToString());
    }
}
