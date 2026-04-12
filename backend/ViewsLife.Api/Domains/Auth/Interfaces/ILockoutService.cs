namespace ViewsLife.Api.Domains.Auth.Interfaces;

/// <summary>
/// Service for managing account lockout after repeated failed sign-in attempts.
/// </summary>
public interface ILockoutService
{
    /// <summary>
    /// Checks if an account is currently locked due to repeated failed attempts.
    /// </summary>
    Task<bool> IsLockedOutAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a failed sign-in attempt and applies lockout if threshold is exceeded.
    /// </summary>
    Task RecordFailedAttemptAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the failed attempt counter after successful sign-in.
    /// </summary>
    Task ResetFailedAttemptsAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the time remaining until the account is unlocked.
    /// Returns null if the account is not locked.
    /// </summary>
    Task<TimeSpan?> GetRemainingLockoutTimeAsync(string normalizedEmail, CancellationToken cancellationToken = default);
}
