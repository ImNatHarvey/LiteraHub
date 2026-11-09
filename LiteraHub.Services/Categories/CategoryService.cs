
using System.Data.Common;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.Core.Security;

namespace LiteraHub.Services.Categories;

/// <summary>
/// Validates category data and enforces role permissions.
/// Database operations are delegated to ICategoryRepository.
/// </summary>
public sealed class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repository;
    private readonly IPermissionService _permissions;

    public CategoryService(
        ICategoryRepository repository,
        IPermissionService permissions)
    {
        _repository = repository ??
            throw new ArgumentNullException(nameof(repository));

        _permissions = permissions ??
            throw new ArgumentNullException(nameof(permissions));
    }

    public Task<IReadOnlyList<Category>> GetAllAsync(
        bool includeInactive = false)
    {
        _permissions.Demand(Permission.ViewCatalog);

        // Inactive records are administrative data.
        if (includeInactive)
            _permissions.Demand(Permission.ManageLookups);

        return _repository.GetAllAsync(includeInactive);
    }

    public Task<Category?> GetByIdAsync(int categoryId)
    {
        _permissions.Demand(Permission.ViewCatalog);

        if (categoryId <= 0)
            return Task.FromResult<Category?>(null);

        return GetVisibleCategoryAsync(categoryId);
    }

    private async Task<Category?> GetVisibleCategoryAsync(int categoryId)
    {
        var category = await _repository.GetByIdAsync(categoryId);

        if (category is null)
            return null;

        // Non-managers must not retrieve inactive categories.
        if (!category.IsActive &&
            !_permissions.Can(Permission.ManageLookups))
        {
            return null;
        }

        return category;
    }

    public async Task<ServiceResult> CreateAsync(CategoryInput input)
    {
        _permissions.Demand(Permission.CreateLookups);

        var validation = Validate(input);

        if (!validation.Success)
            return validation;

        var normalized = Normalize(input);

        var duplicate = await _repository.GetByNameAsync(normalized.Name);

        if (duplicate is not null)
            return ServiceResult.Fail(
                "A category with this name already exists.");

        try
        {
            await _repository.CreateAsync(normalized);
            return ServiceResult.Ok();
        }
        catch (DbException)
        {
            // The database may reject a concurrent duplicate insert
            // or report another database error.
            return ServiceResult.Fail(
                "The category could not be saved. " +
                "Check for a duplicate name or database issue.");
        }
    }

    public async Task<ServiceResult> UpdateAsync(
        int categoryId,
        CategoryInput input)
    {
        _permissions.Demand(Permission.ManageLookups);

        if (categoryId <= 0)
            return ServiceResult.Fail("Invalid category ID.");

        var validation = Validate(input);

        if (!validation.Success)
            return validation;

        var normalized = Normalize(input);

        var existing = await _repository.GetByIdAsync(categoryId);

        if (existing is null)
            return ServiceResult.Fail("Category not found.");

        var duplicate = await _repository.GetByNameAsync(normalized.Name);

        if (duplicate is not null &&
            duplicate.CategoryId != categoryId)
        {
            return ServiceResult.Fail(
                "A category with this name already exists.");
        }

        try
        {
            var updated = await _repository.UpdateAsync(
                categoryId,
                normalized);

            return updated
                ? ServiceResult.Ok()
                : ServiceResult.Fail("Category not found.");
        }
        catch (DbException)
        {
            return ServiceResult.Fail(
                "The category could not be updated. " +
                "Check for a duplicate name or database issue.");
        }
    }

    public async Task<ServiceResult> SetActiveAsync(
        int categoryId,
        bool isActive)
    {
        _permissions.Demand(Permission.ManageLookups);

        if (categoryId <= 0)
            return ServiceResult.Fail("Invalid category ID.");

        var existing = await _repository.GetByIdAsync(categoryId);

        if (existing is null)
            return ServiceResult.Fail("Category not found.");

        if (existing.IsActive == isActive)
            return ServiceResult.Ok();

        try
        {
            var updated = await _repository.SetActiveAsync(
                categoryId,
                isActive);

            return updated
                ? ServiceResult.Ok()
                : ServiceResult.Fail("Category not found.");
        }
        catch (DbException)
        {
            return ServiceResult.Fail(
                "The category status could not be updated " +
                "because of a database issue.");
        }
    }

    private static ServiceResult Validate(CategoryInput? input)
    {
        if (input is null)
            return ServiceResult.Fail("Category data is required.");

        var name = input.Name?.Trim() ?? string.Empty;

        if (name.Length == 0)
            return ServiceResult.Fail("Category name is required.");

        if (name.Length > 100)
            return ServiceResult.Fail(
                "Category name must not exceed 100 characters.");

        if (input.Description?.Trim().Length > 500)
            return ServiceResult.Fail(
                "Description must not exceed 500 characters.");

        return ServiceResult.Ok();
    }

    private static CategoryInput Normalize(CategoryInput input)
    {
        var name = input.Name.Trim();

        var description = input.Description?.Trim();

        if (string.IsNullOrEmpty(description))
            description = null;

        return new CategoryInput(name, description);
    }
}
