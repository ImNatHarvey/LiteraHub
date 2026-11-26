using System.Data.Common;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.Core.Security;

namespace LiteraHub.Services.Security;

public sealed class RoleService : IRoleService
{
    private readonly IRoleRepository _roles;
    private readonly IPermissionService _permissions;
    private readonly ISessionContext _session;

    public RoleService(IRoleRepository roles, IPermissionService permissions, ISessionContext session)
    {
        _roles = roles;
        _permissions = permissions;
        _session = session;
    }

    private void DemandAdmin()
    {
        _permissions.Demand(Permission.ManageUsers);
        if (!string.Equals(_session.Current?.RoleName, RoleNames.Admin, StringComparison.OrdinalIgnoreCase))
            throw new PermissionDeniedException(Permission.ManageUsers);
    }

    public Task<IReadOnlyList<RoleDefinition>> GetAllAsync()
    {
        DemandAdmin();
        return _roles.GetAllAsync();
    }

    public async Task<IReadOnlyList<RolePermission>> GetPermissionsAsync(int roleId)
    {
        DemandAdmin();
        var keys = (await _roles.GetPermissionKeysAsync(roleId)).ToHashSet(StringComparer.Ordinal);
        return Enum.GetValues<RolePermission>().Where(p => keys.Contains(p.ToString())).ToList();
    }
    public async Task<ServiceResult> CreateAsync(RoleInput input)
    {
        DemandAdmin();
        var error = Validate(input);
        if (error is not null) return ServiceResult.Fail(error);
        try { return await _roles.CreateAsync(Normalize(input)) > 0 ? ServiceResult.Ok() : ServiceResult.Fail("Role could not be created."); }
        catch (DbException) { return ServiceResult.Fail("Unable to create role. Check for a duplicate role name or database error."); }
    }

    public async Task<ServiceResult> UpdateAsync(int roleId, RoleInput input)
    {
        DemandAdmin();
        if (input is null) return ServiceResult.Fail("Role information is required.");
        if (input.Description?.Trim().Length > 500) return ServiceResult.Fail("Description cannot exceed 500 characters.");
        var existing = (await _roles.GetAllAsync()).FirstOrDefault(r => r.RoleId == roleId);
        if (existing is null) return ServiceResult.Fail("Role was not found.");
        if (existing.IsSystem)
        {
            try
            {
                var description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
                return await _roles.UpdateAsync(roleId, new RoleInput(existing.Name, description, Array.Empty<RolePermission>()))
                    ? ServiceResult.Ok() : ServiceResult.Fail("Unable to update description.");
            }
            catch (DbException) { return ServiceResult.Fail("Unable to update description."); }
        }
        var error = Validate(input);
        if (error is not null) return ServiceResult.Fail(error);
        try { return await _roles.UpdateAsync(roleId, Normalize(input)) ? ServiceResult.Ok() : ServiceResult.Fail("Role could not be updated."); }
        catch (DbException) { return ServiceResult.Fail("Unable to update role. Check for a duplicate role name or database error."); }
    }

    public async Task<ServiceResult> SetActiveAsync(int roleId, bool active)
    {
        DemandAdmin();
        try { return await _roles.SetActiveAsync(roleId, active) ? ServiceResult.Ok() : ServiceResult.Fail("Role status could not be changed. System roles are protected and roles with active users cannot be archived."); }
        catch (DbException) { return ServiceResult.Fail("Unable to change role status."); }
    }

    public async Task<ServiceResult> DeleteAsync(int roleId)
    {
        DemandAdmin();
        try { return await _roles.DeleteAsync(roleId) ? ServiceResult.Ok() : ServiceResult.Fail("Only archived custom roles with no assigned users can be deleted."); }
        catch (DbException) { return ServiceResult.Fail("Unable to delete role. It may still be referenced."); }
    }

    private static RoleInput Normalize(RoleInput input) => new(input.Name.Trim(),
        string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
        input.Permissions.Distinct().ToArray());

    private static string? Validate(RoleInput input)
    {
        if (input is null || string.IsNullOrWhiteSpace(input.Name)) return "Role name is required.";
        if (input.Name.Trim().Length > 50) return "Role name cannot exceed 50 characters.";
        if (input.Description?.Trim().Length > 500) return "Description cannot exceed 500 characters.";
        if (input.Permissions is null || input.Permissions.Any(p => !Enum.IsDefined(p))) return "Invalid permission selection.";
        var keys = input.Permissions.Select(p => p.ToString()).ToHashSet(StringComparer.Ordinal);
        foreach (var module in new[] { "Books", "Authors", "Categories", "Students", "Users", "Roles", "BorrowReturn", "Reports" })
            if (keys.Any(k => k.StartsWith(module, StringComparison.Ordinal)) && !keys.Contains(module + "View"))
                return $"{module} View permission is required for its other actions.";
        return null;
    }
}
