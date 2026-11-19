using LiteraHub.Core.Models;

namespace LiteraHub.Core.Interfaces;

public interface IStudentRepository
{
    Task<IReadOnlyList<Student>> GetAllAsync(bool includeInactive = false);
    Task<Student?> GetByIdAsync(int studentId);
    Task<Student?> GetByStudentNoAsync(string studentNo);
    Task<int> CreateAsync(StudentInput input);
    Task<bool> UpdateAsync(int studentId, StudentInput input);
    Task<bool> SetActiveAsync(int studentId, bool isActive);
}
