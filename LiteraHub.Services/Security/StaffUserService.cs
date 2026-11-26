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
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

    public StaffUserService(IStaffUserRepository users, IPermissionService permissions,
        ISessionContext session, IPasswordHasher hasher)
    {
        _users = users;
        _permissions = permissions;
        _session = session;
        _hasher = hasher;
    }

    public Task<IReadOnlyList<StaffUser>> GetAllAsync()
    {
        DemandAdmin();
        return _users.GetAllAsync();
    }

    public Task<StaffUser?> GetByIdAsync(int id)
    {
        DemandAdmin();
        return _users.GetByIdAsync(id);
    }

    public async Task<StaffCreationResult> CreateAsync(StaffUserInput input)
    {
        DemandAdmin();
        var validation = Validate(input);
        if (validation is not null) return StaffCreationResult.Fail(validation);
        var normalized = Normalize(input);
        if (await _users.UsernameExistsAsync(normalized.Username))
            return StaffCreationResult.Fail("Username is already in use.");
        // This is a temporary *password*, not a recovery passkey. The latter has its own later phase.
        string password = GenerateTemporaryPassword();
        try
        {
            string hash = await Task.Run(() => _hasher.Hash(password));
            int id = await _users.CreateAsync(normalized, hash);
            return id > 0 ? StaffCreationResult.Ok(password) :
                StaffCreationResult.Fail("Selected role is not available.");
        }
        catch (DbException)
        {
            return StaffCreationResult.Fail("Account could not be created. Check the username and database connection.");
        }
    }

    public async Task<ServiceResult> UpdateAsync(int userId, StaffUserInput input)
    {
        DemandAdmin();
        if (userId == _session.Current?.UserId)
            return ServiceResult.Fail("You cannot edit your own staff account here. Use Profile Settings instead.");
        var validation = Validate(input);
        if (validation is not null) return ServiceResult.Fail(validation);
        var normalized = Normalize(input);
        if (await _users.UsernameExistsAsync(normalized.Username, userId))
            return ServiceResult.Fail("Username is already in use.");
        try
        {
            return await _users.UpdateAsync(userId, normalized) ? ServiceResult.Ok() :
                ServiceResult.Fail("Staff account was not found.");
        }
        catch (InvalidOperationException ex) { return ServiceResult.Fail(ex.Message); }
        catch (DbException) { return ServiceResult.Fail("Unable to update this account."); }
    }

    public async Task<ServiceResult> SetActiveAsync(int userId, bool active)
    {
        DemandAdmin();
        if (userId == _session.Current?.UserId)
            return ServiceResult.Fail("You cannot archive your own signed-in account.");
        try
        {
            return await _users.SetActiveAsync(userId, active) ? ServiceResult.Ok() :
                ServiceResult.Fail("Staff account was not found.");
        }
        catch (InvalidOperationException ex) { return ServiceResult.Fail(ex.Message); }
        catch (DbException) { return ServiceResult.Fail("Unable to change account status."); }
    }

    public async Task<ServiceResult> DeleteAsync(int userId)
    {
        DemandAdmin();
        _permissions.Demand(Permission.HardDelete);
        if (userId == _session.Current?.UserId) return ServiceResult.Fail("You cannot delete your own account.");
        try { return await _users.DeleteAsync(userId) ? ServiceResult.Ok() : ServiceResult.Fail("Account was not found or is still active. Archive it first."); }
        catch (DbException) { return ServiceResult.Fail("Unable to delete this account. It may be referenced by library records; keep it archived."); }
    }

    private void DemandAdmin()
    {
        _permissions.Demand(Permission.ManageUsers);
        if (!string.Equals(_session.Current?.RoleName, RoleNames.Admin, StringComparison.OrdinalIgnoreCase))
            throw new PermissionDeniedException(Permission.ManageUsers);
    }

    private static StaffUserInput Normalize(StaffUserInput input) =>
        new(input.Username.Trim(), input.RoleName.Trim());

    private static string? Validate(StaffUserInput? input)
    {
        if (input is null || string.IsNullOrWhiteSpace(input.Username))
            return "Username is required.";
        if (input.Username.Trim().Length > 50 || input.Username.Trim().Length < 3)
            return "Username must be between 3 and 50 characters.";
        if (!input.Username.Trim().All(c => char.IsLetterOrDigit(c) || c is '.' or '_' or '-'))
            return "Username may contain letters, numbers, periods, underscores, and hyphens only.";
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
