
using System.Collections.Generic;
using System.Threading.Tasks;
using LiteraHub.Core.Models;

namespace LiteraHub.Core.Interfaces;

public interface IBookRepository
{
    Task<IReadOnlyList<Book>> GetAllAsync(
        bool includeInactive = false);

    Task<Book?> GetByIdAsync(int bookId);

    Task<Book?> GetByIsbnAsync(string isbn);

    Task<IReadOnlyList<BookLookupOption>> GetAuthorsAsync();

    Task<IReadOnlyList<BookLookupOption>> GetStatusesAsync();

    Task<int> CreateAsync(BookInput input);

    Task<bool> UpdateAsync(int bookId, BookInput input);

    Task<bool> SetActiveAsync(int bookId, bool isActive);

    Task<bool> DeleteIfUnusedAsync(int bookId);
}
