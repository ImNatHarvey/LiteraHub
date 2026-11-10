
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;

namespace LiteraHub.Services.Books;

public sealed class OpenLibraryMetadataService : IBookMetadataService
{
    private static readonly HttpClient Client = CreateClient();

    private const string ApiBase = "https://openlibrary.org";

    private static HttpClient CreateClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(12)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "LiteraHub/1.0 (LibraryManagement; metadata lookup)");

        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/json"));

        return client;
    }

    public async Task<BookLookupResult> LookupByIsbnAsync(
        string isbn,
        CancellationToken cancellationToken = default)
    {
        string normalized = IsbnValidator.Normalize(isbn);

        if (!IsbnValidator.IsValid(normalized))
        {
            return BookLookupResult.Failed(
                "Invalid ISBN. Enter a valid ISBN-10 or ISBN-13.");
        }

        try
        {
            string url =
                $"{ApiBase}/isbn/{Uri.EscapeDataString(normalized)}.json";

            using var response = await Client.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return BookLookupResult.Failed(
                    "No book was found for this ISBN. " +
                    "You can enter its details manually.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return BookLookupResult.Failed(
                    "Open Library is temporarily unavailable. " +
                    "Please try again or enter the book manually.");
            }

            await using var stream =
                await response.Content.ReadAsStreamAsync(
                    cancellationToken);

            using var document =
                await JsonDocument.ParseAsync(
                    stream,
                    cancellationToken: cancellationToken);

            JsonElement root = document.RootElement;

            string title = GetString(root, "title") ?? "";

            if (string.IsNullOrWhiteSpace(title))
            {
                return BookLookupResult.Failed(
                    "The catalog returned incomplete book data. " +
                    "Please enter the details manually.");
            }

            if (!EditionMatchesIsbn(root, normalized))
            {
                return BookLookupResult.Failed(
                    "The catalog returned a different ISBN edition. " +
                    "Please verify the book manually.");
            }

            var authors = await GetAuthorsAsync(
                root,
                cancellationToken);

            var metadata = new BookMetadata
            {
                Isbn = normalized,
                Title = title.Trim(),
                Description = GetDescription(root),
                Authors = authors,
                Publisher = GetFirstString(root, "publishers"),
                PublishedDate = GetString(root, "publish_date"),
                CoverUrl = GetCoverUrl(root),
                Source = "Open Library"
            };

            return BookLookupResult.Found(metadata);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return BookLookupResult.Failed(
                "The ISBN lookup timed out. " +
                "Please try again or enter the details manually.");
        }
        catch (HttpRequestException)
        {
            return BookLookupResult.Failed(
                "Unable to connect to Open Library. " +
                "Check your internet connection or enter manually.");
        }
        catch (JsonException)
        {
            return BookLookupResult.Failed(
                "The book catalog returned an invalid response. " +
                "Please enter the details manually.");
        }
    }

    private static bool EditionMatchesIsbn(
        JsonElement root,
        string requestedIsbn)
    {
        bool hasIsbn = false;
        bool matches = false;

        foreach (string propertyName in new[]
                 { "isbn_10", "isbn_13" })
        {
            if (!root.TryGetProperty(
                    propertyName, out var values) ||
                values.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var item in values.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                    continue;

                string? value = item.GetString();

                if (string.IsNullOrWhiteSpace(value))
                    continue;

                hasIsbn = true;

                if (IsbnValidator.Normalize(value) == requestedIsbn)
                    matches = true;
            }
        }

        // Some Open Library editions omit ISBN arrays.
        // The direct ISBN endpoint itself is edition-specific.
        return !hasIsbn || matches;
    }

    private static string? GetString(
        JsonElement root,
        string propertyName)
    {
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty(
                propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string? GetDescription(JsonElement root)
    {
        if (!root.TryGetProperty(
                "description", out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String)
            return value.GetString();

        if (value.ValueKind == JsonValueKind.Object)
            return GetString(value, "value");

        return null;
    }

    private static string? GetFirstString(
        JsonElement root,
        string propertyName)
    {
        if (!root.TryGetProperty(
                propertyName, out var values) ||
            values.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var item in values.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
                return item.GetString();
        }

        return null;
    }

    private static string? GetCoverUrl(JsonElement root)
    {
        if (!root.TryGetProperty(
                "covers", out var covers) ||
            covers.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var cover in covers.EnumerateArray())
        {
            if (cover.ValueKind == JsonValueKind.Number &&
                cover.TryGetInt64(out long coverId) &&
                coverId > 0)
            {
                return
                    $"https://covers.openlibrary.org/b/id/{coverId}-L.jpg";
            }
        }

        return null;
    }

    private static async Task<IReadOnlyList<string>> GetAuthorsAsync(
        JsonElement root,
        CancellationToken cancellationToken)
    {
        var names = new List<string>();

        if (!root.TryGetProperty(
                "authors", out var authors) ||
            authors.ValueKind != JsonValueKind.Array)
        {
            return names;
        }

        foreach (var author in authors.EnumerateArray().Take(5))
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? key = GetString(author, "key");

            if (key is null &&
                author.TryGetProperty(
                    "author", out var nested))
            {
                key = GetString(nested, "key");
            }

            if (string.IsNullOrWhiteSpace(key) ||
                !key.StartsWith("/authors/", StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                string url = ApiBase + key + ".json";

                using var response = await Client.GetAsync(
                    url,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                    continue;

                await using var stream =
                    await response.Content.ReadAsStreamAsync(
                        cancellationToken);

                using var document =
                    await JsonDocument.ParseAsync(
                        stream,
                        cancellationToken: cancellationToken);

                string? name = GetString(
                    document.RootElement, "name");

                if (!string.IsNullOrWhiteSpace(name) &&
                    !names.Contains(
                        name,
                        StringComparer.OrdinalIgnoreCase))
                {
                    names.Add(name.Trim());
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (HttpRequestException)
            {
                // Author lookup is optional.
                // Preserve the edition metadata.
            }
            catch (JsonException)
            {
                // A malformed author response should not
                // prevent the book from being displayed.
            }
        }

        return names;
    }
}
