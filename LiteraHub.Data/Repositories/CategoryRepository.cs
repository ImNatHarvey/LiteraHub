
using Dapper;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;

namespace LiteraHub.Data.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly IDbConnectionFactory _factory;
    private readonly ISessionContext _session;

    private const string SelectSql = """
        SELECT
            CategoryId,
            Name,
            Description,
            IsActive
        FROM dbo.Categories
        """;

    public CategoryRepository(IDbConnectionFactory factory, ISessionContext session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
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
        await using var tx = await conn.BeginTransactionAsync();

        const string sql = """
            INSERT INTO dbo.Categories
                (Name, Description, IsActive)
            OUTPUT INSERTED.CategoryId
            VALUES
                (@Name, @Description, 1);
            """;

        int createdId = await conn.QuerySingleAsync<int>(
            sql,
            new
            {
                input.Name,
                input.Description
            }, tx);
        await AuditWriter.WriteAsync(conn, tx, _session.Current, new AuditEvent(AuditModule.Categories, AuditAction.Created, createdId));
        await tx.CommitAsync();
        return createdId;
    }

    public async Task<bool> UpdateAsync(
        int categoryId,
        CategoryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

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
            }, tx);

        if (affectedRows > 0)
            await AuditWriter.WriteAsync(conn, tx, _session.Current, new AuditEvent(AuditModule.Categories, AuditAction.Updated, categoryId));
        await tx.CommitAsync();
        return affectedRows > 0;
    }

    public async Task<bool> SetActiveAsync(
        int categoryId,
        bool isActive)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

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
            }, tx);

        if (affectedRows > 0)
            await AuditWriter.WriteAsync(conn, tx, _session.Current, new AuditEvent(AuditModule.Categories, (isActive ? AuditAction.Restored : AuditAction.Archived), categoryId));
        await tx.CommitAsync();
        return affectedRows > 0;
    }

    public async Task<bool> DeleteIfUnusedAsync(
        int categoryId)
    {
        if (categoryId <= 0)
            return false;

        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        // The FK constraint remains the final protection
        // against concurrent references.
        const string sql = """
            DELETE FROM dbo.Categories
            WHERE CategoryId = @CategoryId
              AND NOT EXISTS
              (
                  SELECT 1
                  FROM dbo.Books
                  WHERE CategoryId = @CategoryId
              )
              AND NOT EXISTS
              (
                  SELECT 1
                  FROM dbo.BookCategories
                  WHERE CategoryId = @CategoryId
              );
            """;

        int affectedRows = await conn.ExecuteAsync(
            sql,
            new { CategoryId = categoryId }, tx);

        if (affectedRows > 0)
            await AuditWriter.WriteAsync(conn, tx, _session.Current, new AuditEvent(AuditModule.Categories, AuditAction.Deleted, categoryId));
        await tx.CommitAsync();
        return affectedRows > 0;
    }
}
