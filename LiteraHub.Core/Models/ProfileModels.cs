namespace LiteraHub.Core.Models;

public sealed class ProfileInfo
{
    public int UserId { get; set; }
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string Username { get; set; } = "";
    public string RoleName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? RecoveryPasskeyCreatedAt { get; set; }
    public bool HasRecoveryPasskey { get; set; }
}
public sealed record RecoveryPasskeyResult(bool Success, string? Passkey, string? Error);
