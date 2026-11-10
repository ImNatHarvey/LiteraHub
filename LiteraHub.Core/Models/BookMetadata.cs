
using System;
using System.Collections.Generic;

namespace LiteraHub.Core.Models;

public sealed class BookMetadata
{
    public string Isbn { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public IReadOnlyList<string> Authors { get; init; }
        = Array.Empty<string>();

    public string? Publisher { get; init; }

    public string? PublishedDate { get; init; }

    public string? CoverUrl { get; init; }

    public string Source { get; init; } = string.Empty;
}

public sealed class BookLookupResult
{
    public bool Success { get; init; }

    public BookMetadata? Metadata { get; init; }

    public string? Error { get; init; }

    public static BookLookupResult Found(BookMetadata metadata)
    {
        return new BookLookupResult
        {
            Success = true,
            Metadata = metadata
        };
    }

    public static BookLookupResult Failed(string message)
    {
        return new BookLookupResult
        {
            Success = false,
            Error = message
        };
    }
}
