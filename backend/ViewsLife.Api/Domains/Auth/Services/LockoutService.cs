using Microsoft.EntityFrameworkCore;
using ViewsLife.Api.Domains.Auth.Entities;
using ViewsLife.Api.Domains.Auth.Interfaces;
using ViewsLife.Api.Infrastructure.Persistence;

namespace ViewsLife.Api.Domains.Auth.Services;

/// <summary>
/// Implementation of account lockout management.
/// 
/// Lockout strategy:
/// - Lock after 5 failed attempts
/// - Lock duration: 15 minutes
/// - Failed attempt count resets after 30 minutes of inactivity
/// - Reset on successful sign-in
/// </summary>
public sealed class LockoutService : ILockoutService
{
    private const int MaxFailedAttempts = 5;
    private const int LockoutDurationMinutes = 15;
    private const int ResetInactivityMinutes = 30;

    private readonly ApplicationDbContext _dbContext;

    public LockoutService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> IsLockedOutAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        SignInAttempt? attempt = await GetOrCreateAttemptAsync(normalizedEmail, cancellationToken);

        // Check if account is currently locked
        if (attempt.IsLocked)
        {
            return true;
        }

        // Check if failed attempts should be reset due to inactivity
        if (ShouldResetInactivity(attempt))
        {
            attempt.FailedAttempts = 0;
            attempt.LockedUntilUtc = null;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return false;
    }

    public async Task RecordFailedAttemptAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        SignInAttempt attempt = await GetOrCreateAttemptAsync(normalizedEmail, cancellationToken);

        // Reset counter if it's been inactive for too long
        if (ShouldResetInactivity(attempt))
        {
            attempt.FailedAttempts = 0;
            attempt.LockedUntilUtc = null;
        }

        // Increment failure count
        attempt.FailedAttempts++;
        attempt.LastFailedAttemptUtc = DateTime.UtcNow;

        // Apply lockout if threshold is exceeded
        if (attempt.FailedAttempts >= MaxFailedAttempts)
        {
            attempt.LockedUntilUtc = DateTime.UtcNow.AddMinutes(LockoutDurationMinutes);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetFailedAttemptsAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        SignInAttempt? attempt = await _dbContext.SignInAttempts
            .FirstOrDefaultAsync(a => a.NormalizedEmail == normalizedEmail, cancellationToken);

        if (attempt is not null)
        {
            attempt.FailedAttempts = 0;
            attempt.LockedUntilUtc = null;
            attempt.LastFailedAttemptUtc = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<TimeSpan?> GetRemainingLockoutTimeAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        SignInAttempt? attempt = await _dbContext.SignInAttempts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.NormalizedEmail == normalizedEmail, cancellationToken);

        if (attempt?.LockedUntilUtc is null)
        {
            return null;
        }

        TimeSpan remaining = attempt.LockedUntilUtc.Value - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : null;
    }

    /// <summary>
    /// Gets or creates a sign-in attempt record for the given email.
    /// </summary>
    private async Task<SignInAttempt> GetOrCreateAttemptAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        SignInAttempt? existing = await _dbContext.SignInAttempts
            .FirstOrDefaultAsync(a => a.NormalizedEmail == normalizedEmail, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var newAttempt = new SignInAttempt
        {
            NormalizedEmail = normalizedEmail,
            FailedAttempts = 0,
            LastFailedAttemptUtc = DateTime.UtcNow
        };

        _dbContext.SignInAttempts.Add(newAttempt);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return newAttempt;
    }

    /// <summary>
    /// Determines if the failed attempt counter should be reset due to inactivity.
    /// </summary>
    private static bool ShouldResetInactivity(SignInAttempt attempt)
    {
        return DateTime.UtcNow - attempt.LastFailedAttemptUtc > TimeSpan.FromMinutes(ResetInactivityMinutes);
    }
}
