
namespace LiteraHub.Core.Models;

public sealed class AuthorRecord
{
    public int AuthorId { get; set; }

    public string FirstName { get; set; } = "";

    public string? MiddleName { get; set; }

    public string LastName { get; set; } = "";

    public string Gender { get; set; } = "Unknown";

    public DateTime? BirthDate { get; set; }

    public bool IsActive { get; set; }

    public string FullName =>
        string.Join(" ", new[]
        {
            FirstName,
            MiddleName,
            LastName
        }.Where(value =>
            !string.IsNullOrWhiteSpace(value)));
}
