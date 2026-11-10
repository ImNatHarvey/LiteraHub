
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
}
