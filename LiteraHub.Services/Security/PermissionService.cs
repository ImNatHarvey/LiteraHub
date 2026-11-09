using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Security;

namespace LiteraHub.Services.Security;

public sealed class PermissionService : IPermissionService
{
    private static readonly Dictionary<string, HashSet<Permission>> Matrix =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [RoleNames.Admin] = new HashSet<Permission>(Enum.GetValues<Permission>()),
            [RoleNames.Librarian] = new HashSet<Permission>
            {
                Permission.ViewCatalog,
                Permission.ManageBooks,
                Permission.ManageStudents,
                Permission.CreateLookups,
                Permission.ProcessBorrowReturn,
                Permission.ViewReports
            },
            [RoleNames.Borrower] = new HashSet<Permission>
            {
                Permission.ViewCatalog,
                Permission.ViewOwnLoans
            }
        };

    private readonly ISessionContext _session;

    public PermissionService(ISessionContext session)
    {
        _session = session;
    }

    public bool Can(Permission permission)
    {
        var current = _session.Current;
        return current is not null
            && Matrix.TryGetValue(current.RoleName, out var granted)
            && granted.Contains(permission);
    }

    public void Demand(Permission permission)
    {
        if (!Can(permission))
            throw new PermissionDeniedException(permission);
    }

    public IReadOnlyList<Permission> GrantedPermissions()
    {
        var current = _session.Current;
        if (current is null || !Matrix.TryGetValue(current.RoleName, out var granted))
            return Array.Empty<Permission>();

        return granted.OrderBy(p => p).ToList();
    }
}