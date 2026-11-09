using System.Data.Common;
using LiteraHub.Core.Interfaces;
using Microsoft.Data.SqlClient;

namespace LiteraHub.Data;

public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public DbConnection CreateConnection() => new SqlConnection(_connectionString);
}