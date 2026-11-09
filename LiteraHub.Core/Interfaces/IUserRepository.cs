using LiteraHub.Core.Models;

namespace LiteraHub.Core.Interfaces;

public interface IUserRepository
{
    Task<UserAccount?> GetByUsernameAsync(string username);

    Task<UserAccount?> GetByIdAsync(int userId);

    Task UpdatePasswordAsync(int userId, string passwordHash, bool mustChangePassword);
}