using LiteraHub.Core.Models;

namespace LiteraHub.Core.Interfaces;

public interface IStaffUserRepository
{
    Task<IReadOnlyList<StaffUser>> GetAllAsync();
    Task<StaffUser?> GetByIdAsync(int userId);
    Task<bool> UsernameExistsAsync(string username, int? exceptUserId = null);
    Task<int> CreateAsync(StaffUserInput input, string passwordHash);
    Task<bool> UpdateAsync(int userId, StaffUserInput input);
    Task<bool> SetActiveAsync(int userId, bool isActive);
}
