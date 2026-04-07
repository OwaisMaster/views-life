using System.ComponentModel.DataAnnotations;

namespace ViewsLife.Api.Domains.Auth.Entities;

/// Represents a tenant in ViewsLife.
///
/// Context:
/// - Every newly registered user gets their own tenant immediately.
/// - Future collaboration and shared spaces hang off this entity instead of
///   overloading the user record as the tenant itself.
public sealed class Tenant
{
    /// Primary key for the tenant record.
    [Key]
    [MaxLength(100)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// Human-readable tenant name shown in the application.
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// URL-safe slug for the tenant.
    /// This must be unique.
    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    /// Identifier of the user who originally created this tenant.
    [Required]
    [MaxLength(100)]
    public string OwnerUserId { get; set; } = string.Empty;

    /// Indicates whether the tenant is active.
    public bool IsActive { get; set; } = true;

    /// UTC timestamp for when the tenant was created.
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// UTC timestamp for the last meaningful update to the tenant.
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// Navigation to the owner user.
    public ApplicationUser? OwnerUser { get; set; }

    /// Navigation to tenant memberships.
    public List<TenantMembership> Memberships { get; set; } = [];
}