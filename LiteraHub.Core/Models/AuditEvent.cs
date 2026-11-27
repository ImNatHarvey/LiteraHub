namespace LiteraHub.Core.Models;

public enum AuditModule { Books, Authors, Categories, Students, Users, Roles, Profile, Authentication, Circulation, LibraryConfiguration }
public enum AuditAction { Created, Updated, Archived, Restored, Deleted, CredentialsVerified, LoginRejected, LoginLocked, PasswordChanged, Borrowed, Returned }
public enum AuditOutcome { Success, Failed, Denied }

public sealed record AuditEvent(AuditModule Module, AuditAction Action, int? EntityId, AuditOutcome Outcome = AuditOutcome.Success)
{
    // No caller-provided text, credentials, or personal record values enter the log.
    public string Description => $"{Module}: {Action}";
}
