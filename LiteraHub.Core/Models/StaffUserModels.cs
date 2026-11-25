namespace LiteraHub.Core.Models;

// Never expose PasswordHash in account-management view models.
public sealed class StaffUser
{
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string RoleName { get; set; } = "";
    public bool IsActive { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed record StaffUserInput(string Username, string RoleName);

public sealed record StaffCreationResult(bool Success, string Message, string? TemporaryPassword = null)
{
    public static StaffCreationResult Fail(string message) => new(false, message);
    public static StaffCreationResult Ok(string temporaryPassword) =>
        new(true, "Staff account created.", temporaryPassword);
}
