using System.Security.Cryptography;
using LiteraHub.Core.Models;

namespace LiteraHub.Core.Security;

public static class StudentAccountPolicy
{
    public const string PublicRegistrationResponse = "If eligible, your request will be reviewed. Visit the library with proof of enrollment and identity.";
    public static bool IsValidStudentNo(string? value) => value is not null && value.Trim().Length is >= 1 and <= 30 && !value.Any(char.IsControl);
    public static bool IsValidUsername(string? value) => value is not null && value.Trim().Length is >= 3 and <= 50 && value.Trim().All(c => char.IsLetterOrDigit(c) || c is '.' or '_' or '-');
    public static bool IsReservedStudentIdentifier(string? value) => value?.Trim().StartsWith("STU-", StringComparison.OrdinalIgnoreCase) == true;
    public static string GenerateCredential() => "A7" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
    private const string PasskeyAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static string GenerateRecoveryPasskey() => new string(Enumerable.Range(0, 16)
        .Select(_ => PasskeyAlphabet[RandomNumberGenerator.GetInt32(PasskeyAlphabet.Length)]).ToArray());

    public static bool IsStudentRole(string? role) => string.Equals(role, RoleNames.Borrower, StringComparison.OrdinalIgnoreCase);
    public static bool IsValidIdentity(string? role, int? studentId) => !string.IsNullOrWhiteSpace(role) && !string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase) &&
        (IsStudentRole(role) ? studentId is > 0 : studentId is null);
    public static bool MatchesSession(UserAccount user, UserSession session) => user.IsActive &&
        user.UserId == session.UserId && user.StudentId == session.StudentId &&
        string.Equals(user.RoleName, session.RoleName, StringComparison.OrdinalIgnoreCase) &&
        user.SessionVersion == session.SessionVersion && IsValidIdentity(user.RoleName, user.StudentId);
    public static string DisplayRole(string role) => IsStudentRole(role) ? "Student" : role;
}
