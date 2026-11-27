using System.Data.Common;
using Dapper;
using LiteraHub.Core.Models;

namespace LiteraHub.Data.Repositories;

public static class AuditWriter
{
    // Caller owns the transaction; failure must roll back the associated mutation.
    public static async Task WriteAsync(DbConnection conn, DbTransaction tx, UserSession? actor, AuditEvent entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        await conn.ExecuteAsync("""
            INSERT dbo.ActivityAudit(ActorUserId, ActorUsername, ActorRole, Module, Action, EntityId, Outcome, Description)
            VALUES (@ActorUserId, @ActorUsername, @ActorRole, @Module, @Action, @EntityId, @Outcome, @Description);
            """, new
        {
            ActorUserId = actor?.UserId,
            ActorUsername = actor?.Username,
            ActorRole = actor?.RoleName,
            Module = entry.Module.ToString(),
            Action = entry.Action.ToString(),
            entry.EntityId,
            Outcome = entry.Outcome.ToString(),
            entry.Description
        }, tx);
    }
}
