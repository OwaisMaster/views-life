using System.ComponentModel.DataAnnotations;

namespace ViewsLife.Api.Domains.Auth.Entities;

/// Represents an application user in ViewsLife.
/// - This is the first persistence entity for the Auth domain.
/// - Each user is effectively the root of a tenant/note space in the current architecture.
/// - Keep this initial version small and stable so auth can stop depending on placeholder data.
public sealed class ApplicationUser
{
    /// Primary key for the user record.
    /// A string key keeps things flexible for external identity providers such as Apple and Google.
    [Key]
    [MaxLength(100)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// User-facing display name.
    /// This can come from a provider profile or later be customized by the user.
    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    /// Email address if available.
    /// Apple Sign-In may not always provide it repeatedly after the initial consent flow,
    /// so this should be optional at the entity level.
    public string? Email { get; set; }

    /// The authentication provider used for this account, for example:
    /// - Apple
    /// - Google
    /// - Local
    [Required]
    [MaxLength(50)]
    public string AuthProvider { get; set; } = string.Empty;

    /// The provider-specific subject/identifier returned by the upstream identity provider.
    /// This will later be used to find or create the user during sign-in.
    [Required]
    [MaxLength(200)]
    public string ProviderSubjectId { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}