using LiteraHub.Core.Security;
using LiteraHub.Core.Models;
using Xunit;

namespace LiteraHub.Tests;

public sealed class StudentAccountPolicyTests
{
    [Theory]
    [InlineData("STU-123456789", true)]
    [InlineData("stu-123456789", true)]
    [InlineData("student.name", false)]
    public void ReservedStudentIdentifier(string value, bool expected) => Assert.Equal(expected, StudentAccountPolicy.IsReservedStudentIdentifier(value));
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("  ", false)]
    [InlineData("02000421492", true)]
    [InlineData("2026-0001", true)]
    [InlineData("a\nb", false)]
    [InlineData("1234567890123456789012345678901", false)]
    public void StudentNumberValidation(string? input, bool expected) => Assert.Equal(expected, StudentAccountPolicy.IsValidStudentNo(input));
    [Theory]
    [InlineData("ab", false)]
    [InlineData("student.01", true)]
    [InlineData("a b c", false)]
    [InlineData("a'b", false)]
    public void UsernameValidation(string input, bool expected) => Assert.Equal(expected, StudentAccountPolicy.IsValidUsername(input));
    [Fact]
    public void GeneratedCredentialMeetsPolicy()
    {
        var value = StudentAccountPolicy.GenerateCredential();
        Assert.Equal(34, value.Length);
        Assert.Null(LiteraHub.Services.Security.PasswordPolicy.Validate(value, "old-password1", "student"));
        Assert.NotEqual(value, StudentAccountPolicy.GenerateCredential());
    }
    [Fact]
    public void RegistrationAuditUsesFixedDescription() => Assert.Equal("StudentAccounts: RegistrationApproved", new AuditEvent(AuditModule.StudentAccounts, AuditAction.RegistrationApproved, 12).Description);
    [Fact]
    public void GeneratedPasskeyMatchesRecoveryServiceFormat()
    {
        var key = StudentAccountPolicy.GenerateRecoveryPasskey();
        Assert.Equal(16, key.Length); Assert.All(key, c => Assert.True(char.IsAsciiLetterOrDigit(c)));
        Assert.NotEqual(key, StudentAccountPolicy.GenerateRecoveryPasskey());
    }
    [Fact]
    public void StudentRoleUsesStableKeyAndDisplayName()
    {
        Assert.True(StudentAccountPolicy.IsStudentRole("Borrower"));
        Assert.Equal("Student", StudentAccountPolicy.DisplayRole("Borrower"));
        Assert.Equal("Admin", StudentAccountPolicy.DisplayRole("Admin"));
    }
    [Fact]
    public void StaleStudentSessionDoesNotMatch()
    {
        var account = new UserAccount { UserId = 3, RoleName = "Borrower", StudentId = 10, IsActive = true, SessionVersion = 2 };
        var session = new UserSession(3, "student", "Borrower", 10) { SessionVersion = 1 };
        Assert.False(StudentAccountPolicy.MatchesSession(account, session));
        Assert.True(StudentAccountPolicy.MatchesSession(account, session with { SessionVersion = 2 }));
    }
}
