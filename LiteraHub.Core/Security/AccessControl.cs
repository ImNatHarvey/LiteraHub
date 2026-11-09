namespace LiteraHub.Core.Security;

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Librarian = "Librarian";
    public const string Borrower = "Borrower";
}

public enum Permission
{
    ViewCatalog,
    ManageBooks,
    ManageStudents,
    CreateLookups,
    ManageLookups,
    ProcessBorrowReturn,
    ViewReports,
    ManageUsers,
    HardDelete,
    ViewOwnLoans
}

public sealed class PermissionDeniedException : Exception
{
    public PermissionDeniedException(Permission permission)
        : base($"You do not have permission to do this ({permission}).")
    {
        Permission = permission;
    }

    public Permission Permission { get; }
}