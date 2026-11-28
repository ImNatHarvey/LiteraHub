using System.Data;
using System.Data.Common;
using Dapper;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.Core.Security;

namespace LiteraHub.Data.Repositories;

public sealed class StudentRegistrationRow
{
    public int RegistrationId { get; set; }
    public int? StudentId { get; set; }
    public string Status { get; set; } = "";
    public DateTime SubmittedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int? ReviewerUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? AccountUserId { get; set; }
    public string? DecisionCode { get; set; }
}

// Never serialize or log this credential delivery object.
public sealed class StudentAccountCredentials(int userId, string username, string password, string passkey)
{
    public int UserId { get; } = userId;
    public string Username { get; } = username;
    public string TemporaryPassword { get; } = password;
    public string RecoveryPasskey { get; } = passkey;
}

public sealed class StudentAccountRepository(IDbConnectionFactory factory, IPasswordHasher hasher, ISessionContext session)
{
    private UserSession Actor => session.Current ?? throw new UnauthorizedAccessException("Sign in first.");

    private static async Task AuthorizeAsync(DbConnection db, DbTransaction tx, UserSession actor)
    {
        if (actor.StudentId.HasValue || string.Equals(actor.RoleName, RoleNames.Borrower, StringComparison.OrdinalIgnoreCase))
            throw new RolePermissionDeniedException(RolePermission.StudentsManageAccounts);
        await DelegationGuard.LoadAsync(db, tx, actor, RolePermission.StudentsManageAccounts, RolePermission.StudentsView);
        if (await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Users WHERE UserId=@UserId AND StudentId IS NULL", new { actor.UserId }, tx) != 1)
            throw new RolePermissionDeniedException(RolePermission.StudentsManageAccounts);
    }

    private static async Task ExpireAsync(DbConnection db, DbTransaction tx)
    {
        var expired = await db.QueryAsync<int>("""
            UPDATE dbo.StudentRegistrations SET Status='Expired', ReviewedAt=SYSUTCDATETIME(), DecisionCode='Expired'
            OUTPUT INSERTED.RegistrationId WHERE Status='Pending' AND (ExpiresAt<=SYSUTCDATETIME() OR StudentId IS NULL);
            """, transaction: tx);
        foreach (var id in expired)
            await AuditWriter.WriteAsync(db, tx, null, new(AuditModule.StudentAccounts, AuditAction.RegistrationExpired, id));
    }

    public async Task<string> SubmitAsync(string? studentNo)
    {
        if (!StudentAccountPolicy.IsValidStudentNo(studentNo)) return StudentAccountPolicy.PublicRegistrationResponse;
        await using var db = factory.CreateConnection();
        await db.OpenAsync();
        await using var tx = await db.BeginTransactionAsync(IsolationLevel.Serializable);
        // Global persisted throttle for the trusted library kiosk. Counts valid-shaped attempts, even unknown IDs.
        var allowed = await db.ExecuteAsync("""
            UPDATE dbo.StudentRegistrationThrottle WITH (UPDLOCK,HOLDLOCK)
            SET Attempts=CASE WHEN WindowStartedAt<=DATEADD(MINUTE,-1,SYSUTCDATETIME()) THEN 1 ELSE Attempts+1 END,
                WindowStartedAt=CASE WHEN WindowStartedAt<=DATEADD(MINUTE,-1,SYSUTCDATETIME()) THEN SYSUTCDATETIME() ELSE WindowStartedAt END
            WHERE SingletonId=1 AND (Attempts<10 OR WindowStartedAt<=DATEADD(MINUTE,-1,SYSUTCDATETIME()));
            """, transaction: tx);
        if (allowed == 1)
        {
            await ExpireAsync(db, tx);
            var id = await db.QuerySingleOrDefaultAsync<int?>("""
                INSERT dbo.StudentRegistrations(StudentId,ExpiresAt)
                OUTPUT INSERTED.RegistrationId
                SELECT s.StudentId,DATEADD(DAY,7,SYSUTCDATETIME()) FROM dbo.Students s WITH (UPDLOCK,HOLDLOCK)
                WHERE s.StudentNo=@StudentNo AND s.IsActive=1
                AND NOT EXISTS(SELECT 1 FROM dbo.Users u WITH (UPDLOCK,HOLDLOCK) WHERE u.StudentId=s.StudentId)
                AND NOT EXISTS(SELECT 1 FROM dbo.StudentRegistrations r WITH (UPDLOCK,HOLDLOCK) WHERE r.StudentId=s.StudentId AND r.Status='Pending');
                """, new { StudentNo = studentNo!.Trim() }, tx);
            if (id.HasValue) await AuditWriter.WriteAsync(db, tx, null, new(AuditModule.StudentAccounts, AuditAction.RegistrationSubmitted, id));
        }
        await tx.CommitAsync();
        return StudentAccountPolicy.PublicRegistrationResponse;
    }

    public async Task<IReadOnlyList<StudentRegistrationRow>> GetRegistrationsAsync()
    {
        var actor = Actor;
        await using var db = factory.CreateConnection(); await db.OpenAsync();
        await using var tx = await db.BeginTransactionAsync(IsolationLevel.Serializable);
        await AuthorizeAsync(db, tx, actor);
        await ExpireAsync(db, tx);
        var rows = (await db.QueryAsync<StudentRegistrationRow>("SELECT RegistrationId,StudentId,Status,SubmittedAt,ExpiresAt,ReviewerUserId,ReviewedAt,AccountUserId,DecisionCode FROM dbo.StudentRegistrations ORDER BY SubmittedAt DESC,RegistrationId DESC", transaction: tx)).ToList();
        await tx.CommitAsync(); return rows;
    }

    public async Task RejectAsync(int registrationId, bool notEligible)
    {
        var actor = Actor;
        await using var db = factory.CreateConnection(); await db.OpenAsync();
        await using var tx = await db.BeginTransactionAsync(IsolationLevel.Serializable);
        await AuthorizeAsync(db, tx, actor);
        int changed = await db.ExecuteAsync("""
            UPDATE dbo.StudentRegistrations SET Status='Rejected',ReviewedAt=SYSUTCDATETIME(),ReviewerUserId=@UserId,DecisionCode=@Code
            WHERE RegistrationId=@registrationId AND Status='Pending' AND ExpiresAt>SYSUTCDATETIME();
            """, new { actor.UserId, registrationId, Code = notEligible ? "NotEligible" : "IdentityNotVerified" }, tx);
        if (changed != 1) throw new InvalidOperationException("Registration is no longer pending.");
        await AuditWriter.WriteAsync(db, tx, actor, new(AuditModule.StudentAccounts, AuditAction.RegistrationRejected, registrationId));
        await tx.CommitAsync();
    }

    // Staff must verify identity in person before calling this operation. No claimant password is accepted.
    public async Task<StudentAccountCredentials> ProvisionAsync(int studentId, string username, bool identityVerified, int? registrationId = null)
    {
        if (!identityVerified) throw new InvalidOperationException("Verify identity and enrollment before provisioning.");
        if (!StudentAccountPolicy.IsValidUsername(username)) throw new ArgumentException("Invalid username.", nameof(username));
        username = username.Trim();
        var actor = Actor;
        string password = StudentAccountPolicy.GenerateCredential(), passkey = StudentAccountPolicy.GenerateCredential();
        string passwordHash = await Task.Run(() => hasher.Hash(password));
        string passkeyHash = await Task.Run(() => hasher.Hash(passkey));
        await using var db = factory.CreateConnection(); await db.OpenAsync();
        await using var tx = await db.BeginTransactionAsync(IsolationLevel.Serializable);
        await AuthorizeAsync(db, tx, actor);
        await ExpireAsync(db, tx);
        var pending = await db.QuerySingleOrDefaultAsync<int?>("SELECT RegistrationId FROM dbo.StudentRegistrations WITH (UPDLOCK,HOLDLOCK) WHERE StudentId=@studentId AND Status='Pending' AND ExpiresAt>SYSUTCDATETIME()", new { studentId }, tx);
        if (pending != registrationId) throw new InvalidOperationException("Review the current registration before provisioning.");
        int? id = await db.QuerySingleOrDefaultAsync<int?>("""
            INSERT dbo.Users(Username,PasswordHash,RoleId,StudentId,MustChangePassword,IsActive,RecoveryPasskeyHash,RecoveryPasskeyCreatedAt)
            OUTPUT INSERTED.UserId
            SELECT @username,@passwordHash,r.RoleId,s.StudentId,1,1,@passkeyHash,SYSUTCDATETIME()
            FROM dbo.Students s WITH (UPDLOCK,HOLDLOCK) CROSS JOIN dbo.Roles r WITH (UPDLOCK,HOLDLOCK)
            WHERE s.StudentId=@studentId AND s.IsActive=1 AND r.Name=N'Borrower' AND r.IsSystem=1 AND r.IsActive=1
            AND NOT EXISTS(SELECT 1 FROM dbo.Users u WITH (UPDLOCK,HOLDLOCK) WHERE u.StudentId=s.StudentId OR u.Username=@username);
            """, new { studentId, username, passwordHash, passkeyHash }, tx);
        if (!id.HasValue) throw new InvalidOperationException("Student unavailable, account already linked, or username unavailable.");
        if (registrationId.HasValue)
        {
            int changed = await db.ExecuteAsync("""
                UPDATE dbo.StudentRegistrations SET Status='Approved',ReviewedAt=SYSUTCDATETIME(),ReviewerUserId=@UserId,AccountUserId=@AccountId,DecisionCode='IdentityVerified'
                WHERE RegistrationId=@registrationId AND StudentId=@studentId AND Status='Pending' AND ExpiresAt>SYSUTCDATETIME();
                """, new { actor.UserId, AccountId = id.Value, registrationId, studentId }, tx);
            if (changed != 1) throw new InvalidOperationException("Registration expired or changed.");
            await AuditWriter.WriteAsync(db, tx, actor, new(AuditModule.StudentAccounts, AuditAction.RegistrationApproved, registrationId));
        }
        await AuditWriter.WriteAsync(db, tx, actor, new(AuditModule.StudentAccounts, AuditAction.Created, id));
        await tx.CommitAsync();
        return new(id.Value, username, password, passkey);
    }
}
