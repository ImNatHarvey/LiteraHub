using LiteraHub.Core.Models;

namespace LiteraHub.Core.Interfaces;

public interface IRoleRepository
{
    Task<IReadOnlyList<RoleDefinition>> GetAllAsync();
    Task<int> CreateAsync(RoleInput input);
    Task<bool> UpdateAsync(int roleId, RoleInput input);
    Task<bool> SetActiveAsync(int roleId, bool active);
    Task<bool> DeleteAsync(int roleId);
    Task<IReadOnlyList<string>> GetPermissionKeysAsync(int roleId);
    IReadOnlyList<string> GetActiveUserPermissionKeys(int userId, string roleName);
}
