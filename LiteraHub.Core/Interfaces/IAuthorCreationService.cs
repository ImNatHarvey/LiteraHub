
using LiteraHub.Core.Models;

namespace LiteraHub.Core.Interfaces;

public interface IAuthorCreationService
{
    Task<AuthorCreationResult> CreateAsync(
        AuthorInput input);

    Task<AuthorRecord?> GetByIdAsync(int authorId);

    Task<ServiceResult> UpdateAsync(
        int authorId,
        AuthorInput input);

    Task<ServiceResult> SetActiveAsync(
        int authorId,
        bool isActive);

    Task<ServiceResult> DeleteAsync(int authorId);

    Task<IReadOnlyList<AuthorRecord>> GetAllAsync(
    bool includeInactive = false);
}
