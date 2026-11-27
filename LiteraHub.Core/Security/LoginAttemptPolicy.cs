namespace LiteraHub.Core.Security;

public readonly record struct LoginAttemptState(int Failures, DateTime? LockedUntilUtc);

/// <summary>Shared login policy. Call with database UTC time inside the account transaction.</summary>
public static class LoginAttemptPolicy
{
    public const int MaximumFailures = 5;
    public const int LockoutMinutes = 3;
    public static LoginAttemptState Clear => new(0, null);

    public static bool IsLocked(LoginAttemptState state, DateTime utcNow) =>
        state.LockedUntilUtc is DateTime until && until > utcNow;

    public static LoginAttemptState RegisterFailure(LoginAttemptState state, DateTime utcNow)
    {
        if (IsLocked(state, utcNow)) return state;
        int failures = state.LockedUntilUtc.HasValue ? 1 : Math.Min(state.Failures, MaximumFailures - 1) + 1;
        return new(failures, failures >= MaximumFailures ? utcNow.AddMinutes(LockoutMinutes) : null);
    }
}
