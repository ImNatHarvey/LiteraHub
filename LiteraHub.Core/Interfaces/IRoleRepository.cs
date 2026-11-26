using LiteraHub.Core.Models;

namespace LiteraHub.Core.Interfaces;

public interface IRoleRepository
{
    Task<IReadOnlyList<RoleDefinition>> GetAllAsync();
    Task<int> CreateAsync(RoleInput input, UserSession actor);
    Task<bool> UpdateAsync(int roleId, RoleInput input, UserSession actor);
    Task<bool> SetActiveAsync(int roleId, bool active, UserSession actor);
    Task<bool> DeleteAsync(int roleId, UserSession actor);
    Task<IReadOnlyList<string>> GetPermissionKeysAsync(int roleId);
    IReadOnlyList<string> GetActiveUserPermissionKeys(int userId, string roleName);
}