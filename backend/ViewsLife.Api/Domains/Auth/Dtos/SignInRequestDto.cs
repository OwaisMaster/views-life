using System.ComponentModel.DataAnnotations;
using ViewsLife.Api.Common.Validation;

namespace ViewsLife.Api.Domains.Auth.Dtos;

/// Request payload for local account sign-in.
public sealed class SignInRequestDto
{
    /// Email used to identify the local account.
    /// Must be normalized (lowercase, trimmed) and valid format.
    [Required(ErrorMessage = "Email is required.")]
    [NormalizedEmail]
    [MaxLength(320, ErrorMessage = "Email cannot exceed 320 characters.")]
    public string Email { get; set; } = string.Empty;

    /// Password used to verify the local account.
    /// Must be present and not exceed 200 characters.
    [Required(ErrorMessage = "Password is required.")]
    [MaxLength(200, ErrorMessage = "Password cannot exceed 200 characters.")]
    [TrimmedString(allowEmpty: false)]
    public string Password { get; set; } = string.Empty;
}