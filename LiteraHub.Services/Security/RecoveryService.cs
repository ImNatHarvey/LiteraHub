using System.Text;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
namespace LiteraHub.Services.Security;

public sealed class RecoveryService(IRecoveryRepository repository, IPasswordHasher hasher) : IRecoveryService
{
    public async Task<ServiceResult> ResetAsync(string username, string passkey, string password)
    {
        username = username.Trim();
        if (username.Length is < 1 or > 50 || passkey.Length != 16 || !passkey.All(char.IsAsciiLetterOrDigit))
            return ServiceResult.Fail("Invalid recovery credentials or recovery temporarily unavailable.");
        var error = PasswordPolicy.Validate(password, "", username);
        if (error != null) return ServiceResult.Fail(error);
        if (Encoding.UTF8.GetByteCount(password) > 72) return ServiceResult.Fail("Password exceeds 72 UTF-8 bytes.");
        return await repository.ResetAsync(username, passkey, password, hasher.Verify, hasher.Hash)
            ? ServiceResult.Ok() : ServiceResult.Fail("Invalid recovery credentials or recovery temporarily unavailable.");
    }
}