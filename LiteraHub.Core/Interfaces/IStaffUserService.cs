using LiteraHub.Core.Models;

namespace LiteraHub.Core.Interfaces;

public interface IStaffUserService
{
    Task<IReadOnlyList<StaffUser>> GetAllAsync();
    Task<StaffUser?> GetByIdAsync(int userId);
    Task<StaffCreationResult> CreateAsync(StaffUserInput input);
    Task<ServiceResult> UpdateAsync(int userId, StaffUserInput input);
    Task<ServiceResult> SetActiveAsync(int userId, bool isActive);
    Task<ServiceResult> DeleteAsync(int userId);
}
