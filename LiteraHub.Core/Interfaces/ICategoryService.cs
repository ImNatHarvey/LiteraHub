
using LiteraHub.Core.Models;

namespace LiteraHub.Core.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<Category>> GetAllAsync(
        bool includeInactive = false);

    Task<Category?> GetByIdAsync(int categoryId);

    Task<ServiceResult> CreateAsync(CategoryInput input);

    Task<ServiceResult> UpdateAsync(
        int categoryId,
        CategoryInput input);

    Task<ServiceResult> SetActiveAsync(
        int categoryId,
        bool isActive);

    Task<ServiceResult> DeleteAsync(int categoryId);
}
