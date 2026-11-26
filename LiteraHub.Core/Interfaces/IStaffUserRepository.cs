using LiteraHub.Core.Models;

namespace LiteraHub.Core.Interfaces;

public interface IStaffUserRepository
{
    Task<IReadOnlyList<StaffUser>> GetAllAsync();
    Task<StaffUser?> GetByIdAsync(int userId);
    Task<bool> UsernameExistsAsync(string username, int? exceptUserId = null);
    Task<int> CreateAsync(StaffUserInput input, string passwordHash, string passkeyHash, UserSession actor);
    Task<bool> RegeneratePasskeyAsync(int userId, string passkeyHash, UserSession actor, string? ownerPassword = null);
    Task<bool> UpdateAsync(int userId, StaffUserInput input, UserSession actor, string? ownerPassword = null);
    Task<bool> SetActiveAsync(int userId, bool isActive, UserSession actor, string? ownerPassword = null);
    Task<bool> DeleteAsync(int userId, UserSession actor, string? ownerPassword = null);
}
