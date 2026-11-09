
namespace LiteraHub.Core.Models;

/// <summary>
/// Represents a category stored in dbo.Categories.
/// </summary>
public sealed class Category
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }
}

/// <summary>
/// Input data for creating or updating a category.
/// Does not contain database-generated or system-managed fields.
/// </summary>
public sealed record CategoryInput(
    string Name,
    string? Description
);
