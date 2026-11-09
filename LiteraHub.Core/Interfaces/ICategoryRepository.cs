
using LiteraHub.Core.Models;

namespace LiteraHub.Core.Interfaces;

/// <summary>
/// Defines database operations for book categories.
/// Implementations belong in LiteraHub.Data.
/// </summary>
public interface ICategoryRepository
{
    /// <summary>
    /// Returns categories ordered by name.
    /// </summary>
    Task<IReadOnlyList<Category>> GetAllAsync(
        bool includeInactive = false);

    /// <summary>
    /// Returns one category or null if it does not exist.
    /// </summary>
    Task<Category?> GetByIdAsync(int categoryId);

    /// <summary>
    /// Finds a category by its name.
    /// Used for duplicate-name validation.
    /// </summary>
    Task<Category?> GetByNameAsync(string name);

    /// <summary>
    /// Inserts a new active category and returns its ID.
    /// </summary>
    Task<int> CreateAsync(CategoryInput input);

    /// <summary>
    /// Updates an existing category.
    /// Returns false when the record does not exist.
    /// </summary>
    Task<bool> UpdateAsync(
        int categoryId,
        CategoryInput input);

    /// <summary>
    /// Changes the active state without deleting the record.
    /// Returns false when the record does not exist.
    /// </summary>
    Task<bool> SetActiveAsync(
        int categoryId,
        bool isActive);
}
