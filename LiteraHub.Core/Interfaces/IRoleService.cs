using LiteraHub.Core.Models;
using LiteraHub.Core.Security;

namespace LiteraHub.Core.Interfaces;

public interface IRoleService
{
    Task<IReadOnlyList<RoleDefinition>> GetAllAsync();
    Task<ServiceResult> CreateAsync(RoleInput input);
    Task<ServiceResult> UpdateAsync(int roleId, RoleInput input);
    Task<ServiceResult> SetActiveAsync(int roleId, bool active);
    Task<ServiceResult> DeleteAsync(int roleId);
    Task<IReadOnlyList<RolePermission>> GetPermissionsAsync(int roleId);
}
