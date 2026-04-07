using System.ComponentModel.DataAnnotations;

namespace ViewsLife.Api.Domains.Auth.Entities;

/// Represents an application user in ViewsLife.
/// Context:
/// - Supports local email/password registration in this slice.
/// - Keeps provider fields so external auth providers such as Apple can be added
///   without replacing the entity model later.
/// - Each user will bootstrap their own tenant on first successful registration.
/// </summary>
public sealed class ApplicationUser
{
    /// Primary key for the user record.
    [Key]
    [MaxLength(100)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// User-facing display name.
    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    /// Email address for local sign-in and future provider matching.
    [MaxLength(320)]
    public string? Email { get; set; }

    /// Normalized version of the email for case-insensitive lookup.
    [MaxLength(320)]
    public string? NormalizedEmail { get; set; }

    /// Password hash for local accounts.
    /// This remains null for external-provider-only accounts.
    public string? PasswordHash { get; set; }

    /// The authentication provider used for this account.
    /// Examples:
    /// - Local
    /// - Apple
    /// - Google
    [Required]
    [MaxLength(50)]
    public string AuthProvider { get; set; } = string.Empty;

    /// Provider-specific stable identifier.
    /// For Local auth, this can be the normalized email.
    [Required]
    [MaxLength(200)]
    public string ProviderSubjectId { get; set; } = string.Empty;

    /// Indicates whether the email has been verified.
    /// For local development in this slice, new users are treated as verified
    /// until email verification is introduced later.
    public bool IsEmailVerified { get; set; } = true;

    /// Indicates whether the account is active.
    public bool IsActive { get; set; } = true;

    /// UTC timestamp for when the user record was created.
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// UTC timestamp for the last meaningful update to the user record.
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// Navigation to tenant memberships for this user.
    public List<TenantMembership> TenantMemberships { get; set; } = [];
}