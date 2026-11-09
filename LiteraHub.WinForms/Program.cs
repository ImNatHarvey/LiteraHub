
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.Core.Settings;
using LiteraHub.Data;
using LiteraHub.Data.Bootstrap;
using LiteraHub.Data.Repositories;
using LiteraHub.Services.Categories;
using LiteraHub.Services.Security;
using LiteraHub.WinForms.Forms;
using LiteraHub.WinForms.Theming;
using Microsoft.Extensions.Configuration;

namespace LiteraHub.WinForms;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        ThemePreferences.Load();

        BootstrapResult bootstrap;
        var hasher = new BcryptPasswordHasher();

        try
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var options = new DatabaseOptions
            {
                Server =
                    config["Database:Server"] ?? ".\\SQLEXPRESS",

                FallbackServer =
                    config["Database:FallbackServer"],

                Name =
                    config["Database:Name"] ?? "LiteraHub"
            };

            bootstrap = Task.Run(() =>
                    DatabaseBootstrapper.RunAsync(
                        options,
                        hasher))
                .GetAwaiter()
                .GetResult();
        }
        catch (DatabaseBootstrapException ex)
        {
            MessageBox.Show(
                ex.Message,
                "LiteraHub: database error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            return;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message +
                "\n\nIf this says appsettings.json was not found, " +
                "set its 'Copy to Output Directory' to 'Copy if newer'.",
                "LiteraHub: startup error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            return;
        }

        // Composition root.
        var connectionFactory =
            new SqlConnectionFactory(
                bootstrap.ConnectionString);

        var session = new SessionContext();

        var permissions =
            new PermissionService(session);

        var users =
            new UserRepository(connectionFactory);

        var auth =
            new AuthService(users, hasher, session);

        var categoryRepository =
            new CategoryRepository(connectionFactory);

        ICategoryService categories =
            new CategoryService(
                categoryRepository,
                permissions);

        RunSessions(
            auth,
            permissions,
            session,
            categories);
    }

    private static void RunSessions(
        IAuthService auth,
        IPermissionService permissions,
        ISessionContext session,
        ICategoryService categories)
    {
        while (true)
        {
            AuthResult? login;

            using (var loginForm = new LoginForm(auth))
            {
                if (loginForm.ShowDialog() != DialogResult.OK)
                    return;

                login = loginForm.Result;
            }

            if (login?.Session is null)
                return;

            if (login.MustChangePassword)
            {
                using var change = new ChangePasswordForm(
                    auth,
                    login.Session.UserId,
                    login.Session.Username);

                if (change.ShowDialog() != DialogResult.OK)
                {
                    auth.Logout();
                    continue;
                }
            }

            using var shell = new ShellForm(
                session,
                permissions,
                auth,
                categories);

            shell.ShowDialog();

            auth.Logout();

            if (!shell.LoggedOut)
                return;
        }
    }
}
