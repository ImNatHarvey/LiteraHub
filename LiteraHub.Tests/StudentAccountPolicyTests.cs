using LiteraHub.Core.Security;
using LiteraHub.Core.Models;
using Xunit;

namespace LiteraHub.Tests;

public sealed class StudentAccountPolicyTests
{
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
}
