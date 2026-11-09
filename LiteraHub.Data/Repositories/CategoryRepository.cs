
using Dapper;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;

namespace LiteraHub.Data.Repositories;

/// <summary>
/// Dapper implementation of category database operations.
/// Business validation and authorization belong in the service layer.
/// </summary>
public sealed class CategoryRepository : ICategoryRepository
{
    private readonly IDbConnectionFactory _factory;

    private const string SelectSql = """
        SELECT
            CategoryId,
            Name,
            Description,
            IsActive
        FROM dbo.Categories
        """;

    public CategoryRepository(IDbConnectionFactory factory)
    {
        _factory = factory ??
            throw new ArgumentNullException(nameof(factory));
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(
        bool includeInactive = false)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        var sql = SelectSql +
            (includeInactive
                ? " ORDER BY Name, CategoryId;"
                : " WHERE IsActive = 1 ORDER BY Name, CategoryId;");

        var categories = await conn.QueryAsync<Category>(sql);

        return categories.AsList();
    }

    public async Task<Category?> GetByIdAsync(int categoryId)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        return await conn.QuerySingleOrDefaultAsync<Category>(
            SelectSql + " WHERE CategoryId = @CategoryId;",
            new { CategoryId = categoryId });
    }

    public async Task<Category?> GetByNameAsync(string name)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        return await conn.QuerySingleOrDefaultAsync<Category>(
            SelectSql + " WHERE Name = @Name;",
            new { Name = name });
    }

    public async Task<int> CreateAsync(CategoryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        const string sql = """
            INSERT INTO dbo.Categories
                (Name, Description, IsActive)
            OUTPUT INSERTED.CategoryId
            VALUES
                (@Name, @Description, 1);
            """;

        return await conn.QuerySingleAsync<int>(
            sql,
            new
            {
                input.Name,
                input.Description
            });
    }

    public async Task<bool> UpdateAsync(
        int categoryId,
        CategoryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        const string sql = """
            UPDATE dbo.Categories
            SET
                Name = @Name,
                Description = @Description
            WHERE CategoryId = @CategoryId;
            """;

        var affectedRows = await conn.ExecuteAsync(
            sql,
            new
            {
                CategoryId = categoryId,
                input.Name,
                input.Description
            });

        return affectedRows > 0;
    }

    public async Task<bool> SetActiveAsync(
        int categoryId,
        bool isActive)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        const string sql = """
            UPDATE dbo.Categories
            SET IsActive = @IsActive
            WHERE CategoryId = @CategoryId;
            """;

        var affectedRows = await conn.ExecuteAsync(
            sql,
            new
            {
                CategoryId = categoryId,
                IsActive = isActive
            });

        return affectedRows > 0;
    }
}
