using LiteraHub.Services.Security;
using Xunit;

namespace LiteraHub.Tests;

public sealed class PasswordPolicyTests
{
    [Theory]
    [InlineData("")]
    [InlineData("Abc123")]
    [InlineData("abcdefgh")]
    [InlineData("12345678")]
    public void RejectsInvalidPasswords(string password) =>
        Assert.NotNull(PasswordPolicy.Validate(password, "Previous123", "librarian"));

    [Fact]
    public void AcceptsValidPassword() =>
        Assert.Null(PasswordPolicy.Validate("Library123", "Previous123", "librarian"));

    [Fact]
    public void RejectsUnchangedPassword() =>
        Assert.NotNull(PasswordPolicy.Validate("Previous123", "Previous123", "librarian"));

    [Fact]
    public void RejectsUsernameIgnoringCase() =>
        Assert.NotNull(PasswordPolicy.Validate("Library123", "Previous123", "library123"));

    [Theory]
    [InlineData(72, true)]
    [InlineData(73, false)]
    public void EnforcesAsciiLengthBoundary(int length, bool accepted)
    {
        string password = new string('a', length - 1) + "1";
        Assert.Equal(accepted, PasswordPolicy.Validate(password, "Previous123", "librarian") is null);
    }
}
