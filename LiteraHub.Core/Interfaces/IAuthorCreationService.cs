
using LiteraHub.Core.Models;

namespace LiteraHub.Core.Interfaces;

public interface IAuthorCreationService
{
    Task<AuthorCreationResult> CreateAsync(
        AuthorInput input);
}
