using LiteraHub.Core.Settings;
using LiteraHub.Data.Bootstrap;
using LiteraHub.Services.Security;
using Microsoft.Extensions.Configuration;

namespace LiteraHub.WinForms;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var options = new DatabaseOptions
            {
                Server = config["Database:Server"] ?? ".\\SQLEXPRESS",
                FallbackServer = config["Database:FallbackServer"],
                Name = config["Database:Name"] ?? "LiteraHub"
            };

            var result = Task.Run(() => DatabaseBootstrapper.RunAsync(options, new BcryptPasswordHasher()))
                .GetAwaiter().GetResult();

            var applied = result.AppliedVersions.Count == 0
                ? "none (already up to date)"
                : string.Join(", ", result.AppliedVersions);

            MessageBox.Show(
                $"Server: {result.ServerUsed}\n" +
                $"Migrations applied now: {applied}\n" +
                $"Tables: {result.Summary.TableCount} | Roles: {result.Summary.RoleCount} | " +
                $"Statuses: {result.Summary.StatusCount} | Categories: {result.Summary.CategoryCount} | " +
                $"Users: {result.Summary.UserCount}",
                "LiteraHub: database ready");
        }
        catch (DatabaseBootstrapException ex)
        {
            MessageBox.Show(ex.Message, "LiteraHub: database error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message + "\n\nIf this says appsettings.json was not found, set its 'Copy to Output Directory' to 'Copy if newer'.",
                "LiteraHub: startup error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        Application.Run(new Form1());
    }
}