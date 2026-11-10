
using System.Threading;
using System.Threading.Tasks;
using LiteraHub.Core.Models;

namespace LiteraHub.Core.Interfaces;

public interface IBookMetadataService
{
    Task<BookLookupResult> LookupByIsbnAsync(
        string isbn,
        CancellationToken cancellationToken = default);
}
