namespace LiteraHub.Core.Models;

public sealed class UserAccount
{
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public int RoleId { get; set; }
    public string RoleName { get; set; } = "";
    public int? StudentId { get; set; }
    public bool MustChangePassword { get; set; }

    // True only when both the user and the role are active.
    public bool IsActive { get; set; }
}

public sealed record UserSession(int UserId, string Username, string RoleName, int? StudentId);