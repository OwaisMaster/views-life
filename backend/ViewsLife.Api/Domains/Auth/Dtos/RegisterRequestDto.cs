using System.ComponentModel.DataAnnotations;
using ViewsLife.Api.Common.Validation;

namespace ViewsLife.Api.Domains.Auth.Dtos;

/// Request payload for local account registration and tenant bootstrap.
public sealed class RegisterRequestDto
{
    /// Display name for the user profile.
    /// Must be trimmed and not empty, max 200 characters.
    [Required(ErrorMessage = "Display name is required.")]
    [TrimmedString(allowEmpty: false)]
    [MaxLength(200, ErrorMessage = "Display name cannot exceed 200 characters.")]
    [MinLength(1, ErrorMessage = "Display name is required.")]
    public string DisplayName { get; set; } = string.Empty;

    /// Email used for the local account.
    /// Must be normalized (lowercase, trimmed) and valid format.
    [Required(ErrorMessage = "Email is required.")]
    [NormalizedEmail]
    [MaxLength(320, ErrorMessage = "Email cannot exceed 320 characters.")]
    public string Email { get; set; } = string.Empty;

    /// Password for the local account.
    /// Must be at least 8 characters, max 200.
    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    [MaxLength(200, ErrorMessage = "Password cannot exceed 200 characters.")]
    [TrimmedString(allowEmpty: false)]
    public string Password { get; set; } = string.Empty;

    /// Name for the newly bootstrapped tenant.
    /// Must be trimmed and not empty, max 200 characters.
    [Required(ErrorMessage = "Tenant name is required.")]
    [TrimmedString(allowEmpty: false)]
    [MaxLength(200, ErrorMessage = "Tenant name cannot exceed 200 characters.")]
    [MinLength(1, ErrorMessage = "Tenant name is required.")]
    public string TenantName { get; set; } = string.Empty;
}