using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.Services.Security;
using Xunit;

namespace LiteraHub.Tests;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task InvalidCredentialsDoNotSignIn()
    {
        var (auth, session, _) = Create();
        Assert.False((await auth.LoginAsync("librarian", "Wrong123")).Success);
        Assert.Null(session.Current);
    }

    [Fact]
    public async Task ValidCredentialsSignIn()
    {
        var (auth, session, _) = Create();
        Assert.True((await auth.LoginAsync("librarian", "Previous123")).Success);
        Assert.Equal(10, session.Current?.UserId);
    }

    [Fact]
    public async Task RequiredPasswordChangeDoesNotSignInUntilCompleted()
    {
        var (auth, session, repo) = Create(mustChange: true);
        var login = await auth.LoginAsync("librarian", "Previous123");
        Assert.True(login.Success);
        Assert.True(login.MustChangePassword);
        Assert.Null(session.Current);
        Assert.True((await auth.ChangePasswordAsync(10, "Previous123", "NewPassword123")).Success);
        Assert.False(repo.Account.MustChangePassword);
        Assert.Equal(10, session.Current?.UserId);
    }

    [Fact]
    public async Task PasswordChangeRequiresAuthenticatedOrPendingAccount()
    {
        var (auth, session, repo) = Create(mustChange: true);
        Assert.False((await auth.ChangePasswordAsync(10, "Previous123", "NewPassword123")).Success);
        Assert.True(repo.Account.MustChangePassword);
        Assert.Null(session.Current);
        await auth.LoginAsync("librarian", "Previous123");
        auth.Logout();
        Assert.False((await auth.ChangePasswordAsync(10, "Previous123", "NewPassword123")).Success);
    }

    [Fact]
    public async Task VerifiedLockedAccountGetsCountdownButNoSession()
    {
        var (auth, session, repo) = Create();
        repo.RetryAfterSeconds = 179;
        var result = await auth.LoginAsync("librarian", "Previous123");
        Assert.False(result.Success);
        Assert.Equal(AuthFailure.TemporarilyLocked, result.Failure);
        Assert.Equal(179, result.RetryAfterSeconds);
        Assert.Null(session.Current);
    }

    [Fact]
    public async Task IncorrectPasswordDoesNotRevealLockout()
    {
        var (auth, session, repo) = Create();
        repo.RetryAfterSeconds = 179;
        var result = await auth.LoginAsync("librarian", "Wrong123");
        Assert.Equal(AuthFailure.InvalidCredentials, result.Failure);
        Assert.Equal(0, result.RetryAfterSeconds);
        Assert.Null(session.Current);
    }

    private static (AuthService Auth, TestSession Session, TestRepository Repo) Create(bool mustChange = false)
    {
        var repo = new TestRepository(mustChange);
        var session = new TestSession();
        return (new AuthService(repo, new TestHasher(), session), session, repo);
    }

    // Deliberately fake hashing and storage. These tests exercise service control flow only.
    private sealed class TestHasher : IPasswordHasher
    {
        public string Hash(string password) => "test:" + password;
        public bool Verify(string password, string hash) => Hash(password) == hash;
    }

    private sealed class TestSession : ISessionContext
    {
        public UserSession? Current { get; private set; }
        public void SignIn(UserSession session) => Current = session;
        public void SignOut() => Current = null;
    }

    private sealed class TestRepository(bool mustChange) : IUserRepository
    {
        public UserAccount Account { get; } = new()
        {
            UserId = 10,
            Username = "librarian",
            PasswordHash = "test:Previous123",
            RoleId = 2,
            RoleName = "Librarian",
            IsActive = true,
            MustChangePassword = mustChange
        };

        public Task<UserAccount?> GetByUsernameAsync(string username) =>
            Task.FromResult<UserAccount?>(username == Account.Username ? Account : null);

        public Task<UserAccount?> GetByIdAsync(int userId) =>
            Task.FromResult<UserAccount?>(userId == Account.UserId ? Account : null);

        public Task<(UserAccount? User, int RetryAfterSeconds)> AuthenticateAsync(string username, string password,
            Func<string, string, bool> verify, string dummyHash) =>
            Task.FromResult<(UserAccount? User, int RetryAfterSeconds)>(username == Account.Username && verify(password, Account.PasswordHash)
                ? (RetryAfterSeconds > 0 ? null : Account, RetryAfterSeconds) : (null, 0));

        public int RetryAfterSeconds { get; set; }

        public Task<bool> UpdatePasswordAsync(int userId, string expectedHash, string passwordHash, bool mustChangePassword)
        {
            if (userId != Account.UserId || expectedHash != Account.PasswordHash) return Task.FromResult(false);
            Account.PasswordHash = passwordHash;
            Account.MustChangePassword = mustChangePassword;
            return Task.FromResult(true);
        }
    }
}
