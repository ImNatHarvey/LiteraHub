using LiteraHub.Core.Models;
namespace LiteraHub.Core.Interfaces;

public interface IProfileService
{
    Task<ProfileInfo?> GetAsync();
    Task<ServiceResult> UpdateIdentityAsync(string currentPassword, string username, string firstName, string? middleName, string lastName);
    Task<ServiceResult> ChangeNameAsync(string currentPassword, string firstName, string? middleName, string lastName);
    Task<ServiceResult> ChangeUsernameAsync(string currentPassword, string newUsername);
    Task<ServiceResult> ChangePasswordAsync(string currentPassword, string newPassword);
    Task<RecoveryPasskeyResult> RegeneratePasskeyAsync(string currentPassword);
}
public interface IProfileRepository
{
    Task<ProfileInfo?> GetAsync(int userId);
    Task<bool> ReplaceIdentityAsync(int userId, string expectedHash, string oldUsername, string username, string firstName, string? middleName, string lastName);
    Task<bool> ReplaceNameAsync(int userId, string expectedHash, string firstName, string? middleName, string lastName);
    Task<bool> ReplaceUsernameAsync(int userId, string expectedHash, string oldUsername, string newUsername);
    Task<bool> ReplacePasswordAsync(int userId, string expectedHash, string newHash);
    Task<bool> ReplacePasskeyAsync(int userId, string expectedHash, string passkeyHash);
}