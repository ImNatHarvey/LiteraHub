using LiteraHub.Core.Models;

namespace LiteraHub.Core.Interfaces;

public interface IUserRepository
{
    Task<UserAccount?> GetByUsernameAsync(string username);

    Task<UserAccount?> GetByIdAsync(int userId);

    // Atomically verifies the password and updates persisted sign-in attempt state.
    Task<(UserAccount? User, int RetryAfterSeconds)> AuthenticateAsync(string username, string password,
        Func<string, string, bool> verify, string dummyHash);

    Task<bool> UpdatePasswordAsync(int userId, string expectedHash, string passwordHash, bool mustChangePassword);
}