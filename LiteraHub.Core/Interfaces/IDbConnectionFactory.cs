using System.Data.Common;

namespace LiteraHub.Core.Interfaces;

public interface IDbConnectionFactory
{
    // Returns a new, unopened connection. The caller opens and disposes it.
    DbConnection CreateConnection();
}