
using System;
using System.Collections.Generic;

namespace LiteraHub.Core.Models;

public sealed class Book
{
    public int BookId { get; set; }
    public string Isbn { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public int StatusId { get; set; }
    public string StatusName { get; set; } = "";
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public string? CoverPath { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<int> AuthorIds { get; set; } = new();
    public string Authors { get; set; } = "";
}

public sealed record BookInput(
    string Isbn,
    string Title,
    string? Description,
    int CategoryId,
    int StatusId,
    int TotalCopies,
    int AvailableCopies,
    string? CoverPath,
    IReadOnlyList<int> AuthorIds,
    IReadOnlyList<string>? PendingAuthorNames = null
);

public sealed class BookLookupOption
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
}
