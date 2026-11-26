using LiteraHub.Core.Models;
namespace LiteraHub.Core.Interfaces;

public interface IRecoveryService
{
    Task<ServiceResult> ResetAsync(string username, string passkey, string password);
}
public interface IRecoveryRepository
{
    Task<bool> ResetAsync(string username, string passkey, string password, Func<string, string, bool> verify, Func<string, string> hash);
}