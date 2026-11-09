using LiteraHub.Core.Models;
using LiteraHub.Core.Security;

namespace LiteraHub.Core.Interfaces;

public interface ISessionContext
{
    UserSession? Current { get; }

    void SignIn(UserSession session);

    void SignOut();
}

public interface IPermissionService
{
    bool Can(Permission permission);

    // Throws PermissionDeniedException when the signed-in user lacks the permission.
    void Demand(Permission permission);

    IReadOnlyList<Permission> GrantedPermissions();
}

public interface IAuthService
{
    // Signs the user in unless a password change is still required.
    Task<AuthResult> LoginAsync(string username, string password);

    // Verifies the current password, applies the password policy, saves the new hash.
    // When this completes a forced change, it also signs the user in.
    Task<ServiceResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword);

    void Logout();
}