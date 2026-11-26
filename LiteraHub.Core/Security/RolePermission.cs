namespace LiteraHub.Core.Security;

// Stable database keys. Do not rename persisted members.
public enum RolePermission
{
    BooksView,
    BooksAdd,
    BooksEdit,
    BooksArchiveRestore,
    BooksDelete,
    AuthorsView,
    AuthorsAdd,
    AuthorsEdit,
    AuthorsArchiveRestore,
    AuthorsDelete,
    CategoriesView,
    CategoriesAdd,
    CategoriesEdit,
    CategoriesArchiveRestore,
    CategoriesDelete,
    StudentsView,
    StudentsAdd,
    StudentsEdit,
    StudentsArchiveRestore,
    StudentsDelete,
    UsersView,
    UsersAdd,
    UsersEdit,
    UsersArchiveRestore,
    UsersDelete,
    RolesView,
    RolesAdd,
    RolesEdit,
    RolesArchiveRestore,
    RolesDelete,
    BorrowReturnView,
    BorrowReturnBorrow,
    BorrowReturnReturn,
    ReportsView,
    ReportsExport,
    ActivityLogView,
}


/// <summary>Raised when a granular role permission is not granted.</summary>
public sealed class RolePermissionDeniedException : UnauthorizedAccessException
{
    public RolePermissionDeniedException(RolePermission permission)
        : base($"You do not have permission to do this ({permission}).")
    {
        Permission = permission;
    }

    public RolePermission Permission { get; }
}
