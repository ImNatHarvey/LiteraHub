using System.Security.Cryptography;

namespace LiteraHub.Core.Security;

public static class StudentAccountPolicy
{
    public const string PublicRegistrationResponse = "If eligible, your request will be reviewed. Visit the library with proof of enrollment and identity.";
    public static bool IsValidStudentNo(string? value) => value is not null && value.Trim().Length is >= 1 and <= 30 && !value.Any(char.IsControl);
    public static bool IsValidUsername(string? value) => value is not null && value.Trim().Length is >= 3 and <= 50 && value.Trim().All(c => char.IsLetterOrDigit(c) || c is '.' or '_' or '-');
    public static string GenerateCredential() => "A7" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
}
