using LiteraHub.Core.Models;
namespace LiteraHub.Core.Interfaces;

public interface IRecoveryService
{
    Task<ServiceResult> UnlockAsync(string username, string passkey);
    Task<ServiceResult> ResetAsync(string username, string passkey, string password);
}
public interface IRecoveryRepository
{
    Task<bool> UnlockAsync(string username, string passkey, Func<string, string, bool> verify);
    Task<bool> ResetAsync(string username, string passkey, string password, Func<string, string, bool> verify, Func<string, string> hash);
}