
using LiteraHub.Core.Models;

namespace LiteraHub.Core.Interfaces;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(
        bool includeInactive = false);

    Task<Category?> GetByIdAsync(int categoryId);

    Task<Category?> GetByNameAsync(string name);

    Task<int> CreateAsync(CategoryInput input);

    Task<bool> UpdateAsync(
        int categoryId,
        CategoryInput input);

    Task<bool> SetActiveAsync(
        int categoryId,
        bool isActive);

    // Returns false if the category does not exist
    // or is referenced by a book.
    Task<bool> DeleteIfUnusedAsync(int categoryId);
}
