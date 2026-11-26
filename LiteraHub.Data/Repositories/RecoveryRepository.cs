using System.Data;
using Dapper;
using LiteraHub.Core.Interfaces;
namespace LiteraHub.Data.Repositories;

public sealed class RecoveryRepository(IDbConnectionFactory factory) : IRecoveryRepository
{
    private sealed class State
    {
        public int UserId { get; set; }
        public string PasswordHash { get; set; } = "";
        public string? RecoveryPasskeyHash { get; set; }
        public bool Eligible { get; set; }
        public bool Locked { get; set; }
        public int Failures { get; set; }
    }
    public async Task<bool> ResetAsync(string username, string passkey, string password, Func<string, string, bool> verify, Func<string, string> hash)
    {
        await using var db = factory.CreateConnection();
        await db.OpenAsync();
        await using var tx = await db.BeginTransactionAsync(IsolationLevel.Serializable);
        var state = await db.QuerySingleOrDefaultAsync<State>("""
            SELECT u.UserId, u.PasswordHash, u.RecoveryPasskeyHash,
                CAST(CASE WHEN u.IsActive=1 AND r.IsActive=1 AND r.Name<>N'Borrower' THEN 1 ELSE 0 END AS BIT) Eligible,
                CAST(CASE WHEN RecoveryLockedUntil > SYSUTCDATETIME() THEN 1 ELSE 0 END AS BIT) Locked,
                CASE WHEN RecoveryLockedUntil <= SYSUTCDATETIME() THEN 0 ELSE RecoveryFailures END Failures
            FROM dbo.Users u WITH (UPDLOCK,HOLDLOCK) JOIN dbo.Roles r ON r.RoleId=u.RoleId
            WHERE u.Username=@username
            """, new { username }, tx);
        bool success = false;
        string eventType = "RecoveryDenied";
        if (state is { Eligible: true, Locked: false, RecoveryPasskeyHash: not null })
        {
            bool valid = await Task.Run(() => verify(passkey, state.RecoveryPasskeyHash));
            if (valid && !await Task.Run(() => verify(password, state.PasswordHash)))
            {
                string newHash = await Task.Run(() => hash(password));
                await db.ExecuteAsync("""
                    UPDATE dbo.Users SET PasswordHash=@newHash, MustChangePassword=0, RecoveryPasskeyHash=NULL,
                    RecoveryPasskeyCreatedAt=NULL, RecoveryFailures=0, RecoveryLockedUntil=NULL WHERE UserId=@UserId
                    """, new { newHash, state.UserId }, tx);
                success = true; eventType = "RecoverySucceeded";
            }
            else if (!valid)
            {
                int failures = state.Failures + 1;
                await db.ExecuteAsync("""
                    UPDATE dbo.Users SET RecoveryFailures=@failures,
                    RecoveryLockedUntil=CASE WHEN @failures>=5 THEN DATEADD(MINUTE,15,SYSUTCDATETIME()) ELSE NULL END
                    WHERE UserId=@UserId
                    """, new { failures, state.UserId }, tx);
            }
        }
        await db.ExecuteAsync("INSERT dbo.RecoveryAudit(UserId,EventType) VALUES (@id,@eventType)", new { id = state?.UserId, eventType }, tx);
        await tx.CommitAsync();
        return success;
    }
}
