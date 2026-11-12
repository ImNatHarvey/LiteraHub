
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;

namespace LiteraHub.Data.Repositories;

public sealed class BookRepository : IBookRepository
{
    private readonly IDbConnectionFactory _factory;

    private const string SelectBooks = """
        SELECT
            b.BookId, b.Isbn, b.Title, b.Description,
            b.CategoryId, c.Name AS CategoryName,
            b.StatusId, s.Name AS StatusName,
            b.TotalCopies, b.AvailableCopies,
            b.CoverPath, b.IsActive, b.CreatedAt,
            COALESCE((
                SELECT STRING_AGG(
                    CONCAT(a.FirstName, ' ', a.LastName), ', ')
                FROM dbo.BookAuthors ba
                INNER JOIN dbo.Authors a
                    ON a.AuthorId = ba.AuthorId
                WHERE ba.BookId = b.BookId
            ), '') AS Authors
        FROM dbo.Books b
        INNER JOIN dbo.Categories c
            ON c.CategoryId = b.CategoryId
        INNER JOIN dbo.BookStatuses s
            ON s.StatusId = b.StatusId
        """;

    public BookRepository(IDbConnectionFactory factory)
    {
        _factory = factory ??
            throw new ArgumentNullException(nameof(factory));
    }

    public async Task<IReadOnlyList<Book>> GetAllAsync(
        bool includeInactive = false)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        string sql = SelectBooks +
            (includeInactive
                ? " ORDER BY b.Title, b.BookId;"
                : " WHERE b.IsActive = 1 " +
                  "ORDER BY b.Title, b.BookId;");

        var books = (await conn.QueryAsync<Book>(sql)).ToList();

        if (books.Count > 0)
        {
            var ids = books.Select(b => b.BookId).ToArray();

            var links = await conn.QueryAsync<AuthorLink>(
                """
                SELECT BookId, AuthorId
                FROM dbo.BookAuthors
                WHERE BookId IN @Ids;
                """,
                new { Ids = ids });

            var groups = links
                .GroupBy(x => x.BookId)
                .ToDictionary(
                    x => x.Key,
                    x => x.Select(y => y.AuthorId).ToList());

            foreach (var book in books)
            {
                if (groups.TryGetValue(book.BookId, out var authors))
                    book.AuthorIds = authors;
            }
        }

        return books;
    }

    public async Task<Book?> GetByIdAsync(int bookId)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        var book = await conn.QuerySingleOrDefaultAsync<Book>(
            SelectBooks + " WHERE b.BookId = @BookId;",
            new { BookId = bookId });

        if (book is null)
            return null;

        var ids = await conn.QueryAsync<int>(
            """
            SELECT AuthorId
            FROM dbo.BookAuthors
            WHERE BookId = @BookId
            ORDER BY AuthorId;
            """,
            new { BookId = bookId });

        book.AuthorIds = ids.ToList();
        return book;
    }

    public async Task<Book?> GetByIsbnAsync(string isbn)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        return await conn.QuerySingleOrDefaultAsync<Book>(
            SelectBooks + " WHERE b.Isbn = @Isbn;",
            new { Isbn = isbn });
    }

    public async Task<IReadOnlyList<BookLookupOption>> GetAuthorsAsync()
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        var rows = await conn.QueryAsync<BookLookupOption>(
            """
            SELECT AuthorId AS Id,
                   CONCAT(FirstName, ' ', LastName) AS Name,
                   IsActive
            FROM dbo.Authors
            ORDER BY LastName, FirstName, AuthorId;
            """);

        return rows.AsList();
    }

    public async Task<IReadOnlyList<BookLookupOption>> GetStatusesAsync()
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        var rows = await conn.QueryAsync<BookLookupOption>(
            """
            SELECT StatusId AS Id, Name, IsActive
            FROM dbo.BookStatuses
            ORDER BY Name;
            """);

        return rows.AsList();
    }

    public async Task<int> CreateAsync(BookInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        try
        {
            var resolvedAuthors = await ResolveAuthorsAsync(conn, tx, input);

            const string sql = """
                INSERT INTO dbo.Books
                    (Isbn, Title, Description, CategoryId,
                     StatusId, TotalCopies, AvailableCopies,
                     CoverPath, IsActive)
                OUTPUT INSERTED.BookId
                VALUES
                    (@Isbn, @Title, @Description, @CategoryId,
                     @StatusId, @TotalCopies, @AvailableCopies,
                     @CoverPath, 1);
                """;

            int id = await conn.QuerySingleAsync<int>(
                sql, input, tx);

            await InsertAuthorsAsync(
                conn, tx, id, resolvedAuthors);

            await tx.CommitAsync();
            return id;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> UpdateAsync(
        int bookId, BookInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        try
        {
            var resolvedAuthors = await ResolveAuthorsAsync(conn, tx, input);

            // Lock the book row until the transaction completes.
            // Existing borrowed copies cannot be silently erased.
            var current = await conn.QuerySingleOrDefaultAsync<CopyState>(
                """
                SELECT TotalCopies, AvailableCopies
                FROM dbo.Books WITH (UPDLOCK, HOLDLOCK)
                WHERE BookId = @BookId;
                """,
                new { BookId = bookId }, tx);

            if (current is null)
            {
                await tx.RollbackAsync();
                return false;
            }

            int outstanding =
                current.TotalCopies - current.AvailableCopies;

            if (input.TotalCopies < outstanding ||
                input.AvailableCopies >
                    input.TotalCopies - outstanding)
            {
                throw new InvalidOperationException(
                    "Copy counts conflict with existing loans.");
            }

            // Available copies are computed from the current
            // outstanding count rather than trusting stale UI data.
            int available = input.TotalCopies - outstanding;

            int affected = await conn.ExecuteAsync(
                """
                UPDATE dbo.Books
                SET Isbn = @Isbn,
                    Title = @Title,
                    Description = @Description,
                    CategoryId = @CategoryId,
                    StatusId = @StatusId,
                    TotalCopies = @TotalCopies,
                    AvailableCopies = @AvailableCopies,
                    CoverPath = @CoverPath
                WHERE BookId = @BookId;
                """,
                new
                {
                    BookId = bookId,
                    input.Isbn,
                    input.Title,
                    input.Description,
                    input.CategoryId,
                    input.StatusId,
                    input.TotalCopies,
                    AvailableCopies = available,
                    input.CoverPath
                },
                tx);

            if (affected == 0)
            {
                await tx.RollbackAsync();
                return false;
            }

            await conn.ExecuteAsync(
                """
                DELETE FROM dbo.BookAuthors
                WHERE BookId = @BookId;
                """,
                new { BookId = bookId },
                tx);

            await InsertAuthorsAsync(
                conn, tx, bookId, resolvedAuthors);

            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> SetActiveAsync(
        int bookId, bool isActive)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        int affected = await conn.ExecuteAsync(
            """
            UPDATE dbo.Books
            SET IsActive = @IsActive
            WHERE BookId = @BookId;
            """,
            new { BookId = bookId, IsActive = isActive });

        return affected > 0;
    }

    public async Task<bool> DeleteIfUnusedAsync(int bookId)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        try
        {
            // Serialize deletion against other operations
            // that acquire locks on the same book.
            int exists = await conn.ExecuteScalarAsync<int>(
                """
                SELECT COUNT(*)
                FROM dbo.Books WITH (UPDLOCK, HOLDLOCK)
                WHERE BookId = @BookId;
                """,
                new { BookId = bookId }, tx);

            if (exists == 0)
            {
                await tx.RollbackAsync();
                return false;
            }

            int references = await conn.ExecuteScalarAsync<int>(
                """
                SELECT COUNT(*)
                FROM dbo.BorrowReturns
                WHERE BookId = @BookId;
                """,
                new { BookId = bookId }, tx);

            if (references > 0)
            {
                await tx.RollbackAsync();
                return false;
            }

            await conn.ExecuteAsync(
                """
                DELETE FROM dbo.BookAuthors
                WHERE BookId = @BookId;
                """,
                new { BookId = bookId }, tx);

            int affected = await conn.ExecuteAsync(
                """
                DELETE FROM dbo.Books
                WHERE BookId = @BookId
                  AND NOT EXISTS (
                      SELECT 1
                      FROM dbo.BorrowReturns
                      WHERE BookId = @BookId
                  );
                """,
                new { BookId = bookId }, tx);

            await tx.CommitAsync();
            return affected > 0;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static async Task<int[]> ResolveAuthorsAsync(
        System.Data.Common.DbConnection conn,
        System.Data.Common.DbTransaction tx,
        BookInput input)
    {
        var ids = input.AuthorIds.Distinct().ToList();
        var pending = input.PendingAuthorNames ?? Array.Empty<string>();

        foreach (string raw in pending.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            string name = raw.Trim();
            string[] parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                throw new InvalidOperationException(
                    $"Author '{name}' requires a first and last name.");

            string first = string.Join(" ", parts, 0, parts.Length - 1);
            string last = parts[^1];
            if (first.Length > 100 || last.Length > 100)
                throw new InvalidOperationException("Author name exceeds 100 characters.");

            // Serialize author resolution for concurrent book saves.
            // Transaction-owned application lock avoids duplicates even without
            // a unique constraint on the Authors table.
            int lockResult = await conn.ExecuteScalarAsync<int>(
                """
                DECLARE @result int;
                EXEC @result = sp_getapplock
                    @Resource = N'LiteraHub:AuthorName',
                    @LockMode = N'Exclusive',
                    @LockOwner = N'Transaction',
                    @LockTimeout = 10000;
                SELECT @result;
                """, transaction: tx);
            if (lockResult < 0)
                throw new InvalidOperationException("Could not lock author records. Try again.");

            // Compare complete display names, not only split first/last fields:
            // the existing author may have different whitespace or capitalization.
            var matches = (await conn.QueryAsync<int>(
                """
                SELECT AuthorId
                FROM dbo.Authors WITH (UPDLOCK, HOLDLOCK)
                WHERE UPPER(LTRIM(RTRIM(CONCAT(FirstName, N' ', LastName))))
                      = UPPER(@Name) AND IsActive = 1;
                """,
                new { Name = name }, tx)).ToArray();

            if (matches.Length > 1)
                throw new InvalidOperationException(
                    $"Multiple authors match '{name}'. Select the correct author manually.");

            int id;
            if (matches.Length == 1)
            {
                id = matches[0];
            }
            else
            {
                id = await conn.QuerySingleAsync<int>(
                    """
                    INSERT INTO dbo.Authors
                        (FirstName, MiddleName, LastName, Gender, BirthDate, IsActive)
                    OUTPUT INSERTED.AuthorId
                    VALUES (@First, NULL, @Last, N'Unknown', NULL, 1);
                    """,
                    new { First = first, Last = last }, tx);
            }
            ids.Add(id);
        }

        var validated = input with { AuthorIds = ids.Distinct().ToArray() };
        await ValidateReferencesAsync(conn, tx, validated);
        return validated.AuthorIds.ToArray();
    }

    private static async Task ValidateReferencesAsync(
        System.Data.Common.DbConnection conn,
        System.Data.Common.DbTransaction tx,
        BookInput input)
    {
        int valid = await conn.ExecuteScalarAsync<int>(
            """
            SELECT CASE WHEN
                EXISTS (
                    SELECT 1 FROM dbo.Categories
                    WHERE CategoryId = @CategoryId
                      AND IsActive = 1
                )
                AND EXISTS (
                    SELECT 1 FROM dbo.BookStatuses
                    WHERE StatusId = @StatusId
                      AND IsActive = 1
                )
            THEN 1 ELSE 0 END;
            """,
            new
            {
                input.CategoryId,
                input.StatusId
            },
            tx);

        if (valid != 1)
            throw new InvalidOperationException(
                "Select an active category and book status.");

        int[] ids = input.AuthorIds.Distinct().ToArray();

        int count = await conn.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM dbo.Authors
            WHERE AuthorId IN @Ids
              AND IsActive = 1;
            """,
            new { Ids = ids },
            tx);

        if (count != ids.Length)
            throw new InvalidOperationException(
                "One or more selected authors are unavailable.");
    }

    private static async Task InsertAuthorsAsync(
        System.Data.Common.DbConnection conn,
        System.Data.Common.DbTransaction tx,
        int bookId,
        IReadOnlyList<int> authorIds)
    {
        foreach (int authorId in authorIds.Distinct())
        {
            await conn.ExecuteAsync(
                """
                INSERT INTO dbo.BookAuthors (BookId, AuthorId)
                VALUES (@BookId, @AuthorId);
                """,
                new
                {
                    BookId = bookId,
                    AuthorId = authorId
                },
                tx);
        }
    }

    private sealed class AuthorLink
    {
        public int BookId { get; set; }
        public int AuthorId { get; set; }
    }

    private sealed class CopyState
    {
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
    }
}
