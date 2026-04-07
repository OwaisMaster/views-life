using System.ComponentModel.DataAnnotations;

namespace ViewsLife.Api.Domains.Auth.Dtos;

/// Request payload for local account registration and tenant bootstrap.
public sealed class RegisterRequestDto
{
    /// Display name for the user profile.
    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    /// Email used for the local account.
    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    /// Password for the local account.
    [Required]
    [MinLength(8)]
    [MaxLength(200)]
    public string Password { get; set; } = string.Empty;

    /// Name for the newly bootstrapped tenant.
    [Required]
    [MaxLength(200)]
    public string TenantName { get; set; } = string.Empty;
}