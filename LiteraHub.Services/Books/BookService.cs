
using System;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.Core.Security;

namespace LiteraHub.Services.Books;

public sealed class BookService : IBookService
{
    private readonly IBookRepository _repository;
    private readonly IPermissionService _permissions;

    public BookService(
        IBookRepository repository,
        IPermissionService permissions)
    {
        _repository = repository ??
            throw new ArgumentNullException(nameof(repository));

        _permissions = permissions ??
            throw new ArgumentNullException(nameof(permissions));
    }

    public Task<IReadOnlyList<Book>> GetAllAsync(
        bool includeInactive = false)
    {
        _permissions.Demand(Permission.ViewCatalog);

        if (includeInactive)
            _permissions.Demand(Permission.ManageBooks);

        return _repository.GetAllAsync(includeInactive);
    }

    public async Task<Book?> GetByIdAsync(int bookId)
    {
        _permissions.Demand(Permission.ViewCatalog);

        if (bookId <= 0)
            return null;

        var book = await _repository.GetByIdAsync(bookId);

        if (book is not null &&
            !book.IsActive &&
            !_permissions.Can(Permission.ManageBooks))
            return null;

        return book;
    }

    public Task<IReadOnlyList<BookLookupOption>> GetAuthorsAsync()
    {
        _permissions.Demand(Permission.ViewCatalog);
        return _repository.GetAuthorsAsync();
    }

    public Task<IReadOnlyList<BookLookupOption>> GetStatusesAsync()
    {
        _permissions.Demand(Permission.ViewCatalog);
        return _repository.GetStatusesAsync();
    }

    public async Task<ServiceResult> CreateAsync(BookInput input)
    {
        _permissions.Demand(Permission.ManageBooks);

        var validation = Validate(input);

        if (!validation.Success)
            return validation;

        var normalized = Normalize(input);

        var duplicate = await _repository.GetByIsbnAsync(
            normalized.Isbn);

        if (duplicate is not null)
            return ServiceResult.Fail(
                "A book with this ISBN already exists.");

        try
        {
            await _repository.CreateAsync(normalized);
            return ServiceResult.Ok();
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult.Fail(ex.Message);
        }
        catch (DbException)
        {
            return ServiceResult.Fail(
                "The book could not be saved. " +
                "Check its ISBN, references, and database connection.");
        }
    }

    public async Task<ServiceResult> UpdateAsync(
        int bookId,
        BookInput input)
    {
        _permissions.Demand(Permission.ManageBooks);

        if (bookId <= 0)
            return ServiceResult.Fail("Invalid book ID.");

        var validation = Validate(input);

        if (!validation.Success)
            return validation;

        var normalized = Normalize(input);

        var existing = await _repository.GetByIdAsync(bookId);

        if (existing is null)
            return ServiceResult.Fail("Book not found.");

        var duplicate = await _repository.GetByIsbnAsync(
            normalized.Isbn);

        if (duplicate is not null &&
            duplicate.BookId != bookId)
        {
            return ServiceResult.Fail(
                "Another book already uses this ISBN.");
        }

        try
        {
            bool updated = await _repository.UpdateAsync(
                bookId, normalized);

            return updated
                ? ServiceResult.Ok()
                : ServiceResult.Fail("Book not found.");
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult.Fail(ex.Message);
        }
        catch (DbException)
        {
            return ServiceResult.Fail(
                "The book could not be updated. " +
                "Check its ISBN, references, and copy counts.");
        }
    }

    public async Task<ServiceResult> SetActiveAsync(
        int bookId,
        bool isActive)
    {
        _permissions.Demand(Permission.ManageBooks);

        if (bookId <= 0)
            return ServiceResult.Fail("Invalid book ID.");

        var existing = await _repository.GetByIdAsync(bookId);

        if (existing is null)
            return ServiceResult.Fail("Book not found.");

        if (existing.IsActive == isActive)
            return ServiceResult.Ok();

        try
        {
            bool updated = await _repository.SetActiveAsync(
                bookId, isActive);

            return updated
                ? ServiceResult.Ok()
                : ServiceResult.Fail("Book not found.");
        }
        catch (DbException)
        {
            return ServiceResult.Fail(
                "The book status could not be changed.");
        }
    }

    public async Task<ServiceResult> DeleteAsync(int bookId)
    {
        _permissions.Demand(Permission.ManageBooks);
        _permissions.Demand(Permission.HardDelete);

        if (bookId <= 0)
            return ServiceResult.Fail("Invalid book ID.");

        try
        {
            bool deleted = await _repository.DeleteIfUnusedAsync(
                bookId);

            return deleted
                ? ServiceResult.Ok()
                : ServiceResult.Fail(
                    "The book cannot be permanently deleted. " +
                    "It may have borrowing history or no longer exist.");
        }
        catch (DbException)
        {
            return ServiceResult.Fail(
                "The book could not be deleted because " +
                "another record may reference it.");
        }
    }

    private static ServiceResult Validate(BookInput? input)
    {
        if (input is null)
            return ServiceResult.Fail("Book data is required.");

        if (!IsbnValidator.IsValid(input.Isbn))
            return ServiceResult.Fail(
                "Enter a valid ISBN-10 or ISBN-13.");

        string title = input.Title?.Trim() ?? "";

        if (title.Length == 0)
            return ServiceResult.Fail("Book title is required.");

        if (title.Length > 300)
            return ServiceResult.Fail(
                "Book title must not exceed 300 characters.");

        if (input.CategoryId <= 0)
            return ServiceResult.Fail(
                "Select a category.");

        if (input.StatusId <= 0)
            return ServiceResult.Fail(
                "Select a book status.");

        if (input.TotalCopies < 0)
            return ServiceResult.Fail(
                "Total copies cannot be negative.");

        if (input.AvailableCopies < 0 ||
            input.AvailableCopies > input.TotalCopies)
        {
            return ServiceResult.Fail(
                "Available copies must be between zero " +
                "and total copies.");
        }

        if (input.AuthorIds is null ||
            input.AuthorIds.Any(id => id <= 0) ||
            (input.AuthorIds.Count == 0 &&
             (input.PendingAuthorNames?.Count ?? 0) == 0))
        {
            return ServiceResult.Fail(
                "Select at least one valid author.");
        }

        if (input.CoverPath?.Trim().Length > 500)
            return ServiceResult.Fail(
                "Cover path must not exceed 500 characters.");

        return ServiceResult.Ok();
    }

    private static BookInput Normalize(BookInput input)
    {
        string? description = input.Description?.Trim();
        string? cover = input.CoverPath?.Trim();

        return input with
        {
            Isbn = IsbnValidator.Normalize(input.Isbn),
            Title = input.Title.Trim(),
            Description = string.IsNullOrEmpty(description)
                ? null : description,
            CoverPath = string.IsNullOrEmpty(cover)
                ? null : cover,
            AuthorIds = input.AuthorIds.Distinct().ToArray(),
            PendingAuthorNames = input.PendingAuthorNames?
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };
    }
}

