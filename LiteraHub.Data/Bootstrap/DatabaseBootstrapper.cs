using System.Text.RegularExpressions;
using Dapper;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Settings;
using Microsoft.Data.SqlClient;

namespace LiteraHub.Data.Bootstrap;

public static class DatabaseBootstrapper
{
    private const string AdminUsername = "admin";
    private const string AdminTempPassword = "Admin@123"; // forced change on first login (Step 3)

    private const string CreateVersionTableSql = """
        IF OBJECT_ID(N'dbo.SchemaVersion', N'U') IS NULL
        CREATE TABLE dbo.SchemaVersion (
            Version     INT NOT NULL CONSTRAINT PK_SchemaVersion PRIMARY KEY,
            Description NVARCHAR(200) NOT NULL,
            AppliedAt   DATETIME2(0) NOT NULL CONSTRAINT DF_SchemaVersion_AppliedAt DEFAULT SYSUTCDATETIME()
        );
        """;

    public static async Task<BootstrapResult> RunAsync(DatabaseOptions options, IPasswordHasher hasher)
    {
        if (!Regex.IsMatch(options.Name, "^[A-Za-z0-9_]+$"))
            throw new DatabaseBootstrapException(
                "Database name in appsettings.json may only contain letters, digits and underscores.");

        var candidates = new List<string> { options.Server };
        if (!string.IsNullOrWhiteSpace(options.FallbackServer) && options.FallbackServer != options.Server)
            candidates.Add(options.FallbackServer);

        // 1) Find a server we can reach (connect to master).
        string? server = null;
        var errors = new List<string>();
        foreach (var candidate in candidates)
        {
            try
            {
                await using var master = new SqlConnection(BuildConnectionString(candidate, "master"));
                await master.OpenAsync();
                server = candidate;
                break;
            }
            catch (SqlException ex)
            {
                errors.Add($"{candidate}: {ex.Message}");
            }
        }

        if (server is null)
            throw new DatabaseBootstrapException(
                "Could not connect to SQL Server. Check that SQL Server is running and fix the server name in appsettings.json." +
                Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine, errors));

        try
        {
            // 2) Create the database if missing.
            await using (var master = new SqlConnection(BuildConnectionString(server, "master")))
            {
                await master.OpenAsync();
                await master.ExecuteAsync(
                    $"IF DB_ID(N'{options.Name}') IS NULL CREATE DATABASE [{options.Name}] COLLATE SQL_Latin1_General_CP1_CI_AS;");
            }

            // 3) Migrate, seed admin, summarize.
            var connectionString = BuildConnectionString(server, options.Name);
            var applied = await ApplyMigrationsAsync(connectionString);
            await SeedAdminAsync(connectionString, hasher);
            var summary = await GetSummaryAsync(connectionString);

            return new BootstrapResult(server, connectionString, applied, summary);
        }
        catch (DatabaseBootstrapException)
        {
            throw;
        }
        catch (SqlException ex)
        {
            throw new DatabaseBootstrapException($"Database setup failed on {server}: {ex.Message}", ex);
        }
    }

    private static string BuildConnectionString(string server, string database)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database,
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            ConnectTimeout = 5
        };
        return builder.ConnectionString;
    }

    private static async Task<IReadOnlyList<int>> ApplyMigrationsAsync(string connectionString)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        await conn.ExecuteAsync(CreateVersionTableSql);

        var done = (await conn.QueryAsync<int>("SELECT Version FROM dbo.SchemaVersion")).ToHashSet();
        var appliedNow = new List<int>();

        foreach (var migration in Migrations.All.OrderBy(m => m.Version))
        {
            if (done.Contains(migration.Version)) continue;

            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                foreach (var statement in migration.Statements)
                    await conn.ExecuteAsync(statement, transaction: tx);

                await conn.ExecuteAsync(
                    "INSERT INTO dbo.SchemaVersion (Version, Description) VALUES (@Version, @Description)",
                    new { migration.Version, migration.Description },
                    tx);

                await tx.CommitAsync();
                appliedNow.Add(migration.Version);
            }
            catch (SqlException ex)
            {
                throw new DatabaseBootstrapException(
                    $"Migration {migration.Version} ({migration.Description}) failed: {ex.Message}", ex);
            }
        }

        return appliedNow;
    }

    private static async Task SeedAdminAsync(string connectionString, IPasswordHasher hasher)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var userCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Users");
        if (userCount > 0) return;

        var adminRoleId = await conn.ExecuteScalarAsync<int>("SELECT RoleId FROM dbo.Roles WHERE Name = N'Admin'");
        await conn.ExecuteAsync(
            "INSERT INTO dbo.Users (Username, PasswordHash, RoleId, MustChangePassword) VALUES (@Username, @Hash, @RoleId, 1)",
            new { Username = AdminUsername, Hash = hasher.Hash(AdminTempPassword), RoleId = adminRoleId });
    }

    private static async Task<BootstrapSummary> GetSummaryAsync(string connectionString)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        return await conn.QuerySingleAsync<BootstrapSummary>(
            """
            SELECT
                (SELECT COUNT(*) FROM sys.tables)        AS TableCount,
                (SELECT COUNT(*) FROM dbo.Roles)         AS RoleCount,
                (SELECT COUNT(*) FROM dbo.BookStatuses)  AS StatusCount,
                (SELECT COUNT(*) FROM dbo.Categories)    AS CategoryCount,
                (SELECT COUNT(*) FROM dbo.Users)         AS UserCount;
            """);
    }
}