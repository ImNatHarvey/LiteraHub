
using System.Collections.Generic;
using System.Threading.Tasks;
using LiteraHub.Core.Models;

namespace LiteraHub.Core.Interfaces;

public interface IBookService
{
    Task<IReadOnlyList<Book>> GetAllAsync(
        bool includeInactive = false);

    Task<Book?> GetByIdAsync(int bookId);

    Task<IReadOnlyList<BookLookupOption>> GetAuthorsAsync();

    Task<IReadOnlyList<BookLookupOption>> GetStatusesAsync();

    Task<ServiceResult> CreateAsync(BookInput input);

    Task<ServiceResult> UpdateAsync(
        int bookId, BookInput input);

    Task<ServiceResult> SetActiveAsync(
        int bookId, bool isActive);

    Task<ServiceResult> DeleteAsync(int bookId);
}

