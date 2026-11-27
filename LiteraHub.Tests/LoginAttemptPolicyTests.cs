using LiteraHub.Core.Security;
using Xunit;

namespace LiteraHub.Tests;

public sealed class LoginAttemptPolicyTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void FifthFailureLocksForThreeMinutes()
    {
        var state = LoginAttemptPolicy.Clear;
        for (int i = 1; i <= 4; i++)
        {
            state = LoginAttemptPolicy.RegisterFailure(state, Now);
            Assert.Equal(i, state.Failures);
            Assert.False(LoginAttemptPolicy.IsLocked(state, Now));
        }
        state = LoginAttemptPolicy.RegisterFailure(state, Now);
        Assert.Equal(5, state.Failures);
        Assert.Equal<DateTime?>(Now.AddMinutes(3), state.LockedUntilUtc);
        Assert.True(LoginAttemptPolicy.IsLocked(state, Now));
    }

    [Fact]
    public void AttemptsDuringLockoutDoNotExtendIt()
    {
        var state = new LoginAttemptState(5, Now.AddMinutes(3));
        Assert.Equal(state, LoginAttemptPolicy.RegisterFailure(state, Now.AddMinutes(2)));
    }

    [Fact]
    public void ExpiredLockoutStartsNewFailureSequence()
    {
        var state = new LoginAttemptState(5, Now);
        Assert.False(LoginAttemptPolicy.IsLocked(state, Now));
        Assert.Equal(new LoginAttemptState(1, null), LoginAttemptPolicy.RegisterFailure(state, Now));
    }

    [Fact]
    public void ClearResetsFailureCountAndLockout()
    {
        Assert.Equal(0, LoginAttemptPolicy.Clear.Failures);
        Assert.Null(LoginAttemptPolicy.Clear.LockedUntilUtc);
    }
}
