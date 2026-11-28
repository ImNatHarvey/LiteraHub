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

    public long SessionVersion { get; set; }

    // True only when the account, role and linked identity are eligible.
    public bool IsActive { get; set; }
}

public sealed record UserSession(int UserId, string Username, string RoleName, int? StudentId)
{
    public long SessionVersion { get; init; }
}