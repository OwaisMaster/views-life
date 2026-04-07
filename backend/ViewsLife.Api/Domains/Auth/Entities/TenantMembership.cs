using System.ComponentModel.DataAnnotations;

namespace ViewsLife.Api.Domains.Auth.Entities;

/// Represents a user's membership in a tenant.
///
/// Context:
/// - This supports one owner per newly bootstrapped tenant today.
/// - It also prepares the model for future collaborators, admins, and viewers.
public sealed class TenantMembership
{
    /// Primary key for the membership record.
    [Key]
    [MaxLength(100)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// The tenant identifier.
    [Required]
    [MaxLength(100)]
    public string TenantId { get; set; } = string.Empty;

    /// The user identifier.
    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;

    /// Role held by the user inside the tenant.
    /// Examples:
    /// - Owner
    /// - Admin
    /// - Member
    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = "Owner";

    /// UTC timestamp for when the membership was created.
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// Navigation to the tenant.
    public Tenant? Tenant { get; set; }

    /// Navigation to the user.
    public ApplicationUser? User { get; set; }
}