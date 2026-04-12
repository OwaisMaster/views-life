using System.ComponentModel.DataAnnotations;

namespace ViewsLife.Api.Domains.Auth.Entities;

/// <summary>
/// Tracks failed sign-in attempts per normalized email for account lockout.
/// </summary>
public sealed class SignInAttempt
{
    /// <summary>
    /// Primary key.
    /// </summary>
    [Key]
    [MaxLength(100)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Normalized email that failed to sign in.
    /// </summary>
    [Required]
    [MaxLength(320)]
    public string NormalizedEmail { get; set; } = string.Empty;

    /// <summary>
    /// Number of consecutive failed sign-in attempts.
    /// </summary>
    public int FailedAttempts { get; set; } = 0;

    /// <summary>
    /// UTC timestamp of the last failed attempt.
    /// </summary>
    public DateTime LastFailedAttemptUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC timestamp when the account will be unlocked (null if not locked).
    /// </summary>
    public DateTime? LockedUntilUtc { get; set; }

    /// <summary>
    /// Indicates whether the account is currently locked.
    /// </summary>
    public bool IsLocked => LockedUntilUtc.HasValue && LockedUntilUtc > DateTime.UtcNow;
}
