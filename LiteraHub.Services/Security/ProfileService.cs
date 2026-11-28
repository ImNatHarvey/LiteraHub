using System.Security.Cryptography;
using System.Text;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.Core.Security;
namespace LiteraHub.Services.Security;

public sealed class ProfileService(IProfileRepository profiles, IUserRepository users,
    IPasswordHasher hasher, ISessionContext session) : IProfileService
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    public Task<ProfileInfo?> GetAsync() => session.Current is { } current
        ? profiles.GetAsync(current.UserId) : Task.FromResult<ProfileInfo?>(null);

    private async Task<UserAccount?> VerifyAsync(string password)
    {
        var current = session.Current;
        if (current is null || string.IsNullOrEmpty(password) || Encoding.UTF8.GetByteCount(password) > 72) return null;
        var user = await users.GetByIdAsync(current.UserId);
        if (user is null || user.MustChangePassword || !StudentAccountPolicy.MatchesSession(user, current)) return null;
        bool valid = await Task.Run(() => hasher.Verify(password, user.PasswordHash));
        return valid && session.Current == current ? user : null;
    }
    public async Task<ServiceResult> UpdateIdentityAsync(string currentPassword, string username, string firstName, string? middleName, string lastName)
    {
        if (session.Current is { } actor && StudentAccountPolicy.IsStudentRole(actor.RoleName))
            return ServiceResult.Fail("Student identity is managed by the library.");
        username = username.Trim(); firstName = firstName.Trim(); lastName = lastName.Trim(); middleName = middleName?.Trim();
        if (username.Length is < 3 or > 50 || !username.All(c => char.IsLetterOrDigit(c) || c is '.' or '_' or '-'))
            return ServiceResult.Fail("Invalid username.");
        if (firstName.Length is < 1 or > 100 || lastName.Length is < 1 or > 100 || (middleName?.Length ?? 0) > 100)
            return ServiceResult.Fail("First and last names are required; each name must be at most 100 characters.");
        var current = session.Current;
        var user = await VerifyAsync(currentPassword);
        if (user is null || current is null || session.Current != current || current.UserId != user.UserId)
            return ServiceResult.Fail("Current password is incorrect or the account is unavailable.");
        try
        {
            if (!await profiles.ReplaceIdentityAsync(user.UserId, user.PasswordHash, user.Username, username, firstName, string.IsNullOrWhiteSpace(middleName) ? null : middleName, lastName))
                return ServiceResult.Fail("Account changed. Sign in again and retry.");
            if (session.Current == current && current.Username != username)
                session.SignIn(current with { Username = username });
            return ServiceResult.Ok();
        }
        catch (System.Data.Common.DbException ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            return ServiceResult.Fail("Unable to save account information. The username may already be in use.");
        }
    }
    public async Task<ServiceResult> ChangeNameAsync(string currentPassword, string firstName, string? middleName, string lastName)
    {
        if (session.Current is { } actor && StudentAccountPolicy.IsStudentRole(actor.RoleName))
            return ServiceResult.Fail("Student identity is managed by the library.");
        firstName = firstName.Trim(); lastName = lastName.Trim(); middleName = middleName?.Trim();
        if (firstName.Length is < 1 or > 100 || lastName.Length is < 1 or > 100 || (middleName?.Length ?? 0) > 100)
            return ServiceResult.Fail("First and last names are required; each name must be at most 100 characters.");
        var current = session.Current;
        var user = await VerifyAsync(currentPassword);
        if (user is null || current is null || session.Current != current || current.UserId != user.UserId)
            return ServiceResult.Fail("Current password is incorrect or the account is unavailable.");
        return await profiles.ReplaceNameAsync(user.UserId, user.PasswordHash, firstName, string.IsNullOrWhiteSpace(middleName) ? null : middleName, lastName)
            ? ServiceResult.Ok() : ServiceResult.Fail("Account changed. Sign in again and retry.");
    }
    public async Task<ServiceResult> ChangeUsernameAsync(string currentPassword, string newUsername)
    {
        newUsername = newUsername.Trim();
        if (newUsername.Length is < 3 or > 50 || !newUsername.All(c => char.IsLetterOrDigit(c) || c is '.' or '_' or '-'))
            return ServiceResult.Fail("Username must contain 3–50 letters, digits, periods, underscores or hyphens.");
        var current = session.Current;
        var user = await VerifyAsync(currentPassword);
        if (user is null || current is null || session.Current != current || !StudentAccountPolicy.MatchesSession(user, current))
            return ServiceResult.Fail("Current password is incorrect or the account is unavailable.");
        if (string.Equals(user.Username, newUsername, StringComparison.Ordinal))
            return ServiceResult.Fail("Enter a different username.");
        try
        {
            if (!await profiles.ReplaceUsernameAsync(user.UserId, user.PasswordHash, user.Username, newUsername))
                return ServiceResult.Fail("Account changed. Sign in again and retry.");
            if (session.Current == current)
                session.SignIn(current with { Username = newUsername });
            return ServiceResult.Ok();
        }
        catch (System.Data.Common.DbException ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            return ServiceResult.Fail("Unable to change username. It may already be in use.");
        }
    }
    public async Task<ServiceResult> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        var current = session.Current;
        var user = await VerifyAsync(currentPassword);
        if (user is null) return ServiceResult.Fail("Current password is incorrect or the account is unavailable.");
        var error = PasswordPolicy.Validate(newPassword, currentPassword, user.Username);
        if (error is not null) return ServiceResult.Fail(error);
        if (Encoding.UTF8.GetByteCount(newPassword) > 72) return ServiceResult.Fail("Password exceeds 72 UTF-8 bytes.");
        var hash = await Task.Run(() => hasher.Hash(newPassword));
        if (current is null || session.Current != current || !StudentAccountPolicy.MatchesSession(user, current)) return ServiceResult.Fail("Session has ended.");
        return await profiles.ReplacePasswordAsync(user.UserId, user.PasswordHash, hash)
            ? ServiceResult.Ok() : ServiceResult.Fail("Account changed. Sign in again and retry.");
    }
    public async Task<RecoveryPasskeyResult> RegeneratePasskeyAsync(string currentPassword)
    {
        var current = session.Current;
        var user = await VerifyAsync(currentPassword);
        if (user is null) return new(false, null, "Current password is incorrect or the account is unavailable.");
        var key = new string(Enumerable.Range(0, 16).Select(_ => Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)]).ToArray());
        var hash = await Task.Run(() => hasher.Hash(key));
        if (current is null || session.Current != current || !StudentAccountPolicy.MatchesSession(user, current)) return new(false, null, "Session has ended.");
        return await profiles.ReplacePasskeyAsync(user.UserId, user.PasswordHash, hash)
            ? new(true, key, null) : new(false, null, "Account changed. Sign in again and retry.");
    }
}
