
namespace LiteraHub.Core.Models;

public sealed record AuthorInput(
    string FirstName,
    string? MiddleName,
    string LastName,
    string Gender,
    DateTime? BirthDate = null
);

public sealed class AuthorCreationResult
{
    public bool Success { get; init; }

    public int? AuthorId { get; init; }

    public string? Error { get; init; }

    public static AuthorCreationResult Created(int authorId) =>
        new()
        {
            Success = true,
            AuthorId = authorId
        };

    public static AuthorCreationResult Failed(string error) =>
        new()
        {
            Success = false,
            Error = error
        };
}
