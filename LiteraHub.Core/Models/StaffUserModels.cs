namespace LiteraHub.Core.Models;

// Never expose PasswordHash in account-management view models.
public sealed class StaffUser
{
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string FullName => string.Join(" ", new[] { FirstName, MiddleName, LastName }.Where(n => !string.IsNullOrWhiteSpace(n)));
    public string RoleName { get; set; } = "";
    public bool IsActive { get; set; }
    public bool MustChangePassword { get; set; }
    public bool HasRecoveryPasskey { get; set; }
    public bool RoleIsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed record StaffUserInput(string Username, string RoleName, string? FirstName = null, string? MiddleName = null, string? LastName = null);

public sealed record StaffCreationResult(bool Success, string Message, string? TemporaryPassword = null, string? RecoveryPasskey = null)
{
    public static StaffCreationResult Fail(string message) => new(false, message);
    public static StaffCreationResult Ok(string temporaryPassword, string passkey) =>
        new(true, "Staff account created.", temporaryPassword, passkey);
}