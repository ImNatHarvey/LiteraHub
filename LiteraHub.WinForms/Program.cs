
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.Core.Security;
using LiteraHub.Core.Settings;
using LiteraHub.Data;
using LiteraHub.Data.Bootstrap;
using LiteraHub.Data.Repositories;
using LiteraHub.Services.Authors;
using LiteraHub.Services.Books;
using LiteraHub.Services.Categories;
using LiteraHub.Services.Security;
using LiteraHub.Services.Students;
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
                    DatabaseBootstrapper.RunAsync(options, hasher, ConfirmInitialCredential))
                .GetAwaiter()
                .GetResult();
        }
        catch (DatabaseBootstrapException ex)
        {
            ThemedMessageDialog.ShowAlert(null, "LiteraHub: database error", ex.Message, true);
            return;
        }
        catch (Exception ex)
        {
            ThemedMessageDialog.ShowAlert(null, "LiteraHub: startup error",
                ex.Message +
                "\n\nIf this says appsettings.json was not found, " +
                "set its 'Copy to Output Directory' to 'Copy if newer'.", true);
            return;
        }

        var connectionFactory =
            new SqlConnectionFactory(bootstrap.ConnectionString);

        var session = new SessionContext();
        var roleRepository = new RoleRepository(connectionFactory);
        var permissions = new PermissionService(session, roleRepository);
        IRoleService roles = new RoleService(roleRepository, permissions, session);

        var users = new UserRepository(connectionFactory);
        var auth = new AuthService(users, hasher, session);
        IProfileService profile = new ProfileService(new ProfileRepository(connectionFactory, session), users, hasher, session);

        var categoryRepository =
            new CategoryRepository(connectionFactory, session);

        ICategoryService categories =
            new CategoryService(categoryRepository, permissions);

        var bookRepository =
            new BookRepository(connectionFactory, session);

        IBookService books =
            new BookService(bookRepository, permissions);

        var authorRepository =
            new AuthorCreationRepository(connectionFactory, session);

        IAuthorCreationService authors =
            new AuthorCreationService(authorRepository, permissions);

        IStudentService students = new StudentService(
            new StudentRepository(connectionFactory, session), permissions);

        IStaffUserService staffUsers = new StaffUserService(
            new StaffUserRepository(connectionFactory, hasher), permissions, session, hasher, roleRepository);

        IBookMetadataService metadata =
            new OpenLibraryMetadataService();

        var studentAccounts = new StudentAccountRepository(connectionFactory, hasher, session);
        var studentPortal = new StudentPortalRepository(connectionFactory, session);
        RunSessions(
            new RecoveryService(new RecoveryRepository(connectionFactory), hasher),
            auth,
            permissions,
            session,
            categories,
            books,
            authors,
            metadata,
            students,
            staffUsers, roles, profile, studentAccounts, studentPortal);
    }

    // Bootstrap runs on a worker. The credential dialog requires an STA thread for Clipboard.
    private static bool ConfirmInitialCredential(string username, string password)
    {
        bool confirmed = false;
        Exception? failure = null;
        var dialogThread = new Thread(() =>
        {
            try { confirmed = ThemedMessageDialog.ConfirmInitialCredential(username, password); }
            catch (Exception ex) { failure = ex; }
        });
        dialogThread.SetApartmentState(ApartmentState.STA);
        dialogThread.Start();
        dialogThread.Join();
        if (failure is not null)
            throw new DatabaseBootstrapException("Unable to display the initial account setup dialog.", failure);
        return confirmed;
    }

    private static void RunSessions(
        IRecoveryService recovery,
        IAuthService auth,
        IPermissionService permissions,
        ISessionContext session,
        ICategoryService categories,
        IBookService books,
        IAuthorCreationService authors,
        IBookMetadataService metadata,
        IStudentService students,
        IStaffUserService staffUsers, IRoleService roles, IProfileService profile, StudentAccountRepository studentAccounts, StudentPortalRepository studentPortal)
    {
        while (true)
        {
            AuthResult? login;

            using (var loginForm = new LoginForm(auth, recovery, studentAccounts))
            {
                if (loginForm.ShowDialog() != DialogResult.OK)
                    return;

                login = loginForm.Result;
            }

            if (login?.Session is null)
                return;

            // Defense in depth: LoginForm must complete the forced change first.
            if (login.MustChangePassword || session.Current != login.Session || !StudentAccountPolicy.IsValidIdentity(login.Session.RoleName, login.Session.StudentId))
            {
                auth.Logout();
                continue;
            }

            if (StudentAccountPolicy.IsStudentRole(login.Session.RoleName))
            {
                using var studentShell = new StudentShellForm(session, auth, profile, studentPortal);
                studentShell.ShowDialog(); auth.Logout();
                if (!studentShell.LoggedOut) return;
                continue;
            }

            using var shell = new ShellForm(
                session,
                permissions,
                auth,
                categories,
                books,
                authors,
                metadata,
                students,
                staffUsers, roles, profile, studentAccounts);

            shell.ShowDialog();
            auth.Logout();

            if (!shell.LoggedOut)
                return;
        }
    }
}
