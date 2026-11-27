using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.Core.Security;

namespace LiteraHub.Services.Security;

public sealed class AuthService : IAuthService
{
    private const string InvalidMessage = "Invalid credentials or temporarily locked. Try again later.";

    // MVP: only staff accounts can sign in. Flip to true when the borrower portal (bonus) is built.
    private const bool BorrowerPortalEnabled = false;

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly ISessionContext _session;
    private readonly Lazy<string> _dummyHash;
    private int? _pendingPasswordChangeUserId;

    public AuthService(IUserRepository users, IPasswordHasher hasher, ISessionContext session)
    {
        _users = users;
        _hasher = hasher;
        _session = session;
        _dummyHash = new Lazy<string>(() => hasher.Hash("not-a-real-password-1"));
    }

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        _pendingPasswordChangeUserId = null;
        _session.SignOut();
        username = (username ?? "").Trim();
        if (username.Length == 0 || string.IsNullOrEmpty(password))
            return AuthResult.Fail(AuthFailure.InvalidCredentials, "Enter your username and password.");

        var dummyHash = await Task.Run(() => _dummyHash.Value);
        var (user, retryAfterSeconds) = await _users.AuthenticateAsync(username, password, _hasher.Verify, dummyHash);
        if (retryAfterSeconds > 0)
            return AuthResult.Fail(AuthFailure.TemporarilyLocked, "Login temporarily restricted.") with { RetryAfterSeconds = retryAfterSeconds };

        if (user is null)
            return AuthResult.Fail(AuthFailure.InvalidCredentials, InvalidMessage);

        if (!user.IsActive)
            return AuthResult.Fail(AuthFailure.AccountDisabled, "This account is disabled. Contact the administrator.");

        if (!IsRoleAllowed(user.RoleName))
            return AuthResult.Fail(AuthFailure.RoleNotAllowed, "Borrower accounts cannot sign in yet. Please see the librarian.");

        var session = ToSession(user);
        if (user.MustChangePassword)
        {
            _pendingPasswordChangeUserId = user.UserId;
            return AuthResult.Ok(session, mustChangePassword: true);
        }

        _session.SignIn(session);
        return AuthResult.Ok(session, mustChangePassword: false);
    }

    public async Task<ServiceResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        if (_session.Current?.UserId != userId && _pendingPasswordChangeUserId != userId)
            return ServiceResult.Fail("Sign in before changing your password.");
        var user = await _users.GetByIdAsync(userId);
        if (user is null || !user.IsActive)
            return ServiceResult.Fail("Account not found.");

        var currentOk = await Task.Run(() => _hasher.Verify(currentPassword ?? "", user.PasswordHash));
        if (!currentOk)
            return ServiceResult.Fail("The current password is incorrect.");

        var problem = PasswordPolicy.Validate(newPassword, currentPassword ?? "", user.Username);
        if (problem is not null)
            return ServiceResult.Fail(problem);

        var newHash = await Task.Run(() => _hasher.Hash(newPassword));
        if (!await _users.UpdatePasswordAsync(user.UserId, user.PasswordHash, newHash, mustChangePassword: false))
            return ServiceResult.Fail("Account changed or is temporarily locked. Sign in again and retry.");
        _pendingPasswordChangeUserId = null;

        // Completing a forced change signs the user in.
        if (_session.Current is null && IsRoleAllowed(user.RoleName))
            _session.SignIn(ToSession(user));

        return ServiceResult.Ok();
    }

    public void Logout()
    {
        _pendingPasswordChangeUserId = null;
        _session.SignOut();
    }

    private static bool IsRoleAllowed(string roleName) =>
        BorrowerPortalEnabled || !string.Equals(roleName, RoleNames.Borrower, StringComparison.OrdinalIgnoreCase);

    private static UserSession ToSession(UserAccount user) =>
        new(user.UserId, user.Username, user.RoleName, user.StudentId);
}