using System.ComponentModel.DataAnnotations;

namespace ViewsLife.Api.Domains.Auth.Dtos;

/// Request payload for local account sign-in.
public sealed class SignInRequestDto
{
    /// Email used to identify the local account.
    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    /// Password used to verify the local account.
    [Required]
    [MaxLength(200)]
    public string Password { get; set; } = string.Empty;
}