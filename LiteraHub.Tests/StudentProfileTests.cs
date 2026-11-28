using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.Services.Security;
using Xunit;

namespace LiteraHub.Tests;

public sealed class StudentProfileTests
{
    [Fact]
    public async Task StudentCannotChangeAuthoritativeName()
    {
        var (service, _) = Create(1);
        Assert.False((await service.ChangeNameAsync("OldPassword123", "Changed", null, "Name")).Success);
        Assert.False((await service.UpdateIdentityAsync("OldPassword123", "new.student", "Changed", null, "Name")).Success);
    }
    [Fact]
    public async Task StaleSessionCannotChangePasswordOrGeneratePasskey()
    {
        var (service, _) = Create(0);
        Assert.False((await service.ChangePasswordAsync("OldPassword123", "NewPassword123")).Success);
        Assert.False((await service.RegeneratePasskeyAsync("OldPassword123")).Success);
    }
    [Fact]
    public async Task ActiveStudentMayChangeUsernameWithoutChangingIdentity()
    {
        var (service, session) = Create(1);
        Assert.True((await service.ChangeUsernameAsync("OldPassword123", "new.student")).Success);
        Assert.Equal("new.student", session.Current?.Username);
        Assert.Equal(21, session.Current?.StudentId);
        Assert.Equal(1L, session.Current?.SessionVersion);
    }
    private static (ProfileService, SessionContext) Create(long version)
    {
        var session = new SessionContext(); session.SignIn(new UserSession(8, "student", "Borrower", 21) { SessionVersion = version });
        return (new ProfileService(new ProfileStore(), new UserStore(), new Hasher(), session), session);
    }
    private sealed class Hasher : IPasswordHasher
    {
        public string Hash(string password) => "test:" + password;
        public bool Verify(string password, string hash) => Hash(password) == hash;
    }
    private sealed class UserStore : IUserRepository
    {
        public Task<UserAccount?> GetByIdAsync(int userId) => Task.FromResult<UserAccount?>(new UserAccount { UserId = 8, Username = "student", RoleName = "Borrower", StudentId = 21, IsActive = true, SessionVersion = 1, PasswordHash = "test:OldPassword123" });
        public Task<UserAccount?> GetByUsernameAsync(string username) => throw new NotSupportedException();
        public Task<(UserAccount? User, int RetryAfterSeconds)> AuthenticateAsync(string username, string password, Func<string, string, bool> verify, string dummyHash) => throw new NotSupportedException();
        public Task<bool> UpdatePasswordAsync(int userId, string expectedHash, string passwordHash, bool mustChangePassword, long expectedSessionVersion = 0) => throw new NotSupportedException();
    }
    private sealed class ProfileStore : IProfileRepository
    {
        public Task<ProfileInfo?> GetAsync(int userId) => throw new NotSupportedException();
        public Task<bool> ReplaceIdentityAsync(int userId, string expectedHash, string oldUsername, string username, string firstName, string? middleName, string lastName) => throw new NotSupportedException();
        public Task<bool> ReplaceNameAsync(int userId, string expectedHash, string firstName, string? middleName, string lastName) => throw new NotSupportedException();
        public Task<bool> ReplaceUsernameAsync(int userId, string expectedHash, string oldUsername, string newUsername) => Task.FromResult(userId == 8 && expectedHash == "test:OldPassword123" && oldUsername == "student" && newUsername == "new.student");
        public Task<bool> ReplacePasswordAsync(int userId, string expectedHash, string newHash) => throw new NotSupportedException();
        public Task<bool> ReplacePasskeyAsync(int userId, string expectedHash, string passkeyHash) => throw new NotSupportedException();
    }
}
