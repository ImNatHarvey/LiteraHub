using LiteraHub.Core.Models;

namespace LiteraHub.Core.Interfaces;

public interface IStudentService
{
    Task<IReadOnlyList<Student>> GetAllAsync(bool includeInactive = false);
    Task<Student?> GetByIdAsync(int studentId);
    Task<ServiceResult> CreateAsync(StudentInput input);
    Task<ServiceResult> UpdateAsync(int studentId, StudentInput input);
    Task<ServiceResult> SetActiveAsync(int studentId, bool isActive);
    Task<ServiceResult> DeleteAsync(int studentId);
}
