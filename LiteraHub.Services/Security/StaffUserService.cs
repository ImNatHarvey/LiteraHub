using System.Data.Common;
using System.Security.Cryptography;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.Core.Security;
using LiteraHub.Services.Security;

namespace LiteraHub.Services.Security;

public sealed class StaffUserService : IStaffUserService
{
    private readonly IStaffUserRepository _users;
    private readonly IPermissionService _permissions;
    private readonly ISessionContext _session;
    private readonly IPasswordHasher _hasher;
    private readonly IRoleRepository _roles;
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

    public StaffUserService(IStaffUserRepository users, IPermissionService permissions,
        ISessionContext session, IPasswordHasher hasher, IRoleRepository roles)
    {
        _users = users;
        _permissions = permissions;
        _session = session;
        _hasher = hasher;
        _roles = roles;
    }

    public Task<IReadOnlyList<StaffUser>> GetAllAsync()
    {
        _permissions.Demand(RolePermission.UsersView);
        return _users.GetAllAsync();
    }

    public Task<StaffUser?> GetByIdAsync(int id)
    {
        _permissions.Demand(RolePermission.UsersView);
        return _users.GetByIdAsync(id);
    }

    public async Task<StaffCreationResult> CreateAsync(StaffUserInput input)
    {
        _permissions.Demand(RolePermission.UsersAdd);
        var validation = Validate(input);
        if (validation is not null) return StaffCreationResult.Fail(validation);
        var normalized = Normalize(input);
        if (await _users.UsernameExistsAsync(normalized.Username))
            return StaffCreationResult.Fail("Username is already in use.");
        // Both credentials are issued once; only their hashes are stored.
        string password = GenerateTemporaryPassword();
        string passkey = GeneratePasskey();
        try
        {
            string hash = await Task.Run(() => _hasher.Hash(password));
            string passkeyHash = await Task.Run(() => _hasher.Hash(passkey));
            int id = await _users.CreateAsync(normalized, hash, passkeyHash, Actor);
            return id > 0 ? StaffCreationResult.Ok(password, passkey) :
                StaffCreationResult.Fail("Selected role is not available.");
        }
        catch (InvalidOperationException ex) { return StaffCreationResult.Fail(ex.Message); }
        catch (UnauthorizedAccessException ex) { return StaffCreationResult.Fail(ex.Message); }
        catch (DbException)
        {
            return StaffCreationResult.Fail("Account could not be created. Check the username and database connection.");
        }
    }

    private static string GeneratePasskey() => new string(Enumerable.Range(0, 16)
        .Select(_ => Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)]).ToArray());

    public async Task<StaffCreationResult> RegeneratePasskeyAsync(int userId)
    {
        _permissions.Demand(RolePermission.UsersEdit);
        if (userId == Actor.UserId) return StaffCreationResult.Fail("Use Profile & Settings for your own account.");
        if ((await _users.GetByIdAsync(userId))?.IsSystemOwner == true)
            return StaffCreationResult.Fail("The System Owner is protected.");
        try
        {
            string key = GeneratePasskey();
            string hash = await Task.Run(() => _hasher.Hash(key));
            return await _users.RegeneratePasskeyAsync(userId, hash, Actor)
                ? new StaffCreationResult(true, "Recovery passkey regenerated.", RecoveryPasskey: key)
                : StaffCreationResult.Fail("Account or role is inactive or unavailable.");
        }
        catch (InvalidOperationException ex) { return StaffCreationResult.Fail(ex.Message); }
        catch (UnauthorizedAccessException ex) { return StaffCreationResult.Fail(ex.Message); }
        catch (DbException) { return StaffCreationResult.Fail("Unable to regenerate recovery passkey."); }
    }

    public async Task<ServiceResult> UpdateAsync(int userId, StaffUserInput input)
    {
        _permissions.Demand(RolePermission.UsersEdit);
        if (userId == _session.Current?.UserId)
            return ServiceResult.Fail("You cannot edit your own staff account here. Use Profile Settings instead.");
        var validation = Validate(input);
        if (validation is not null) return ServiceResult.Fail(validation);
        var normalized = Normalize(input);
        if (await _users.UsernameExistsAsync(normalized.Username, userId))
            return ServiceResult.Fail("Username is already in use.");
        if ((await _users.GetByIdAsync(userId))?.IsSystemOwner == true)
            return ServiceResult.Fail("The System Owner is protected.");
        try
        {
            return await _users.UpdateAsync(userId, normalized, Actor) ? ServiceResult.Ok() :
                ServiceResult.Fail("Staff account was not found.");
        }
        catch (InvalidOperationException ex) { return ServiceResult.Fail(ex.Message); }
        catch (UnauthorizedAccessException ex) { return ServiceResult.Fail(ex.Message); }
        catch (DbException) { return ServiceResult.Fail("Unable to update this account."); }
    }

    public async Task<ServiceResult> SetActiveAsync(int userId, bool active)
    {
        _permissions.Demand(RolePermission.UsersArchiveRestore);
        if (userId == _session.Current?.UserId)
            return ServiceResult.Fail("You cannot archive your own signed-in account.");
        if ((await _users.GetByIdAsync(userId))?.IsSystemOwner == true)
            return ServiceResult.Fail("The System Owner is protected.");
        try
        {
            return await _users.SetActiveAsync(userId, active, Actor) ? ServiceResult.Ok() :
                ServiceResult.Fail("Staff account was not found.");
        }
        catch (InvalidOperationException ex) { return ServiceResult.Fail(ex.Message); }
        catch (UnauthorizedAccessException ex) { return ServiceResult.Fail(ex.Message); }
        catch (DbException) { return ServiceResult.Fail("Unable to change account status."); }
    }

    public async Task<ServiceResult> DeleteAsync(int userId)
    {
        _permissions.Demand(RolePermission.UsersDelete);
        if (userId == _session.Current?.UserId) return ServiceResult.Fail("You cannot delete your own account.");
        if ((await _users.GetByIdAsync(userId))?.IsSystemOwner == true)
            return ServiceResult.Fail("The System Owner is protected.");
        try { return await _users.DeleteAsync(userId, Actor) ? ServiceResult.Ok() : ServiceResult.Fail("Account was not found or is still active. Archive it first."); }
        catch (InvalidOperationException ex) { return ServiceResult.Fail(ex.Message); }
        catch (UnauthorizedAccessException ex) { return ServiceResult.Fail(ex.Message); }
        catch (DbException) { return ServiceResult.Fail("Unable to delete this account. It may be referenced by library records; keep it archived."); }
    }

    private UserSession Actor => _session.Current ?? throw new UnauthorizedAccessException("Sign in first.");

    // Assignment choices are separate from Roles management; a Users-only role may retrieve these.
    public async Task<IReadOnlyList<RoleDefinition>> GetManageableRolesAsync()
    {
        _permissions.Demand(RolePermission.UsersView);
        var actor = Actor;
        var roles = await _roles.GetAllAsync();
        var grants = _permissions.GrantedRolePermissions().Select(p => p.ToString()).ToHashSet(StringComparer.Ordinal);
        bool admin = string.Equals(actor.RoleName, RoleNames.Admin, StringComparison.OrdinalIgnoreCase);
        var allowed = new List<RoleDefinition>();
        foreach (var role in roles)
        {
            if (string.Equals(role.Name, RoleNames.Borrower, StringComparison.OrdinalIgnoreCase)) continue;
            if (admin || (!role.IsSystem && (await _roles.GetPermissionKeysAsync(role.RoleId)).All(grants.Contains)))
                allowed.Add(role);
        }
        return allowed;
    }

    private static StaffUserInput Normalize(StaffUserInput input) =>
        new(input.Username.Trim(), input.RoleName.Trim(), input.FirstName?.Trim(),
            string.IsNullOrWhiteSpace(input.MiddleName) ? null : input.MiddleName.Trim(), input.LastName?.Trim());

    private static string? Validate(StaffUserInput? input)
    {
        if (input is null || string.IsNullOrWhiteSpace(input.Username))
            return "Username is required.";
        if (input.Username.Trim().Length > 50 || input.Username.Trim().Length < 3)
            return "Username must be between 3 and 50 characters.";
        if (!input.Username.Trim().All(c => char.IsLetterOrDigit(c) || c is '.' or '_' or '-'))
            return "Username may contain letters, numbers, periods, underscores, and hyphens only.";
        if (string.IsNullOrWhiteSpace(input.FirstName) || string.IsNullOrWhiteSpace(input.LastName))
            return "First name and last name are required.";
        if (input.FirstName.Trim().Length > 100 || input.LastName.Trim().Length > 100 || (input.MiddleName?.Trim().Length ?? 0) > 100)
            return "Names cannot exceed 100 characters.";
        if (string.IsNullOrWhiteSpace(input.RoleName) || input.RoleName.Trim().Length > 50 ||
            string.Equals(input.RoleName.Trim(), RoleNames.Borrower, StringComparison.OrdinalIgnoreCase))
            return "Select an active staff role.";
        return null;
    }

    private static string GenerateTemporaryPassword()
    {
        Span<char> chars = stackalloc char[18];
        chars[0] = "ABCDEFGHJKLMNPQRSTUVWXYZ"[RandomNumberGenerator.GetInt32(24)];
        chars[1] = "23456789"[RandomNumberGenerator.GetInt32(8)];
        for (int i = 2; i < chars.Length; i++)
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        return new string(chars);
    }
}
