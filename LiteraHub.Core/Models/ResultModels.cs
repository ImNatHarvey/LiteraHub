namespace LiteraHub.Core.Models;

public sealed record ServiceResult(bool Success, string? Error)
{
    public static ServiceResult Ok() => new(true, null);

    public static ServiceResult Fail(string error) => new(false, error);
}

public enum AuthFailure
{
    None,
    InvalidCredentials,
    AccountDisabled,
    RoleNotAllowed
}

public sealed record AuthResult(
    bool Success,
    AuthFailure Failure,
    string Message,
    UserSession? Session,
    bool MustChangePassword)
{
    public static AuthResult Ok(UserSession session, bool mustChangePassword) =>
        new(true, AuthFailure.None, "", session, mustChangePassword);

    public static AuthResult Fail(AuthFailure failure, string message) =>
        new(false, failure, message, null, false);
}