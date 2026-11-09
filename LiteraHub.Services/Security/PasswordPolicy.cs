namespace LiteraHub.Services.Security;

public static class PasswordPolicy
{
    public const int MinLength = 8;

    // BCrypt only uses the first 72 bytes, so longer passwords are rejected instead of silently cut.
    public const int MaxLength = 72;

    // Returns an error message, or null when the password is acceptable.
    public static string? Validate(string newPassword, string currentPassword, string username)
    {
        if (string.IsNullOrEmpty(newPassword) || newPassword.Length < MinLength)
            return $"The new password must be at least {MinLength} characters.";

        if (newPassword.Length > MaxLength)
            return $"The new password must be at most {MaxLength} characters.";

        if (!newPassword.Any(char.IsLetter) || !newPassword.Any(char.IsDigit))
            return "The new password must contain at least one letter and one number.";

        if (newPassword == currentPassword)
            return "The new password must be different from the current one.";

        if (string.Equals(newPassword, username, StringComparison.OrdinalIgnoreCase))
            return "The password cannot be the same as the username.";

        return null;
    }
}