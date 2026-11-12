
using Dapper;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;

namespace LiteraHub.Data.Repositories;

public sealed class AuthorCreationRepository
{
    private readonly IDbConnectionFactory _factory;

    public AuthorCreationRepository(
        IDbConnectionFactory factory)
    {
        _factory = factory ??
            throw new ArgumentNullException(nameof(factory));
    }

    public async Task<int> CreateAsync(AuthorInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        await using var connection =
            _factory.CreateConnection();

        await connection.OpenAsync();

        return await connection.QuerySingleAsync<int>(
            """
            INSERT INTO dbo.Authors
                (FirstName, MiddleName, LastName,
                 Gender, BirthDate, IsActive)
            OUTPUT INSERTED.AuthorId
            VALUES
                (@FirstName, @MiddleName, @LastName,
                 @Gender, @BirthDate, 1);
            """,
            input);
    }

    public async Task<AuthorRecord?> GetByIdAsync(
        int authorId)
    {
        await using var connection =
            _factory.CreateConnection();

        await connection.OpenAsync();

        return await connection.QuerySingleOrDefaultAsync<AuthorRecord>(
            """
            SELECT AuthorId, FirstName, MiddleName,
                   LastName, Gender, BirthDate, IsActive
            FROM dbo.Authors
            WHERE AuthorId = @authorId;
            """,
            new { authorId });
    }

    public async Task<AuthorRecord?> GetByNameAsync(
        AuthorInput input)
    {
        await using var connection =
            _factory.CreateConnection();

        await connection.OpenAsync();

        return await connection.QueryFirstOrDefaultAsync<AuthorRecord>(
            """
            SELECT TOP (1)
                   AuthorId, FirstName, MiddleName,
                   LastName, Gender, BirthDate, IsActive
            FROM dbo.Authors
            WHERE LOWER(LTRIM(RTRIM(FirstName))) =
                  LOWER(@FirstName)
              AND LOWER(LTRIM(RTRIM(LastName))) =
                  LOWER(@LastName)
              AND LOWER(
                    LTRIM(RTRIM(ISNULL(MiddleName, '')))
                  ) = LOWER(ISNULL(@MiddleName, ''));
            """,
            input);
    }

    public async Task<bool> UpdateAsync(
        int authorId,
        AuthorInput input)
    {
        await using var connection =
            _factory.CreateConnection();

        await connection.OpenAsync();

        int affected = await connection.ExecuteAsync(
            """
            UPDATE dbo.Authors
            SET FirstName = @FirstName,
                MiddleName = @MiddleName,
                LastName = @LastName,
                Gender = @Gender,
                BirthDate = @BirthDate
            WHERE AuthorId = @AuthorId;
            """,
            new
            {
                AuthorId = authorId,
                input.FirstName,
                input.MiddleName,
                input.LastName,
                input.Gender,
                input.BirthDate
            });

        return affected > 0;
    }

    public async Task<bool> SetActiveAsync(
        int authorId,
        bool isActive)
    {
        await using var connection =
            _factory.CreateConnection();

        await connection.OpenAsync();

        int affected = await connection.ExecuteAsync(
            """
            UPDATE dbo.Authors
            SET IsActive = @isActive
            WHERE AuthorId = @authorId;
            """,
            new { authorId, isActive });

        return affected > 0;
    }

    public async Task<bool> DeleteIfUnusedAsync(
        int authorId)
    {
        await using var connection =
            _factory.CreateConnection();

        await connection.OpenAsync();

        int affected = await connection.ExecuteAsync(
            """
            DELETE FROM dbo.Authors
            WHERE AuthorId = @authorId
              AND NOT EXISTS
              (
                  SELECT 1
                  FROM dbo.BookAuthors
                  WHERE AuthorId = @authorId
              );
            """,
            new { authorId });

        return affected > 0;
    }

    public async Task<IReadOnlyList<AuthorRecord>> GetAllAsync(
    bool includeInactive = false)
    {
        await using var connection =
            _factory.CreateConnection();

        await connection.OpenAsync();

        var authors = await connection.QueryAsync<AuthorRecord>(
            """
        SELECT AuthorId, FirstName, MiddleName,
               LastName, Gender, BirthDate, IsActive
        FROM dbo.Authors
        WHERE @includeInactive = 1 OR IsActive = 1
        ORDER BY LastName, FirstName, AuthorId;
        """,
            new { includeInactive });

        return authors.AsList();
    }

}
