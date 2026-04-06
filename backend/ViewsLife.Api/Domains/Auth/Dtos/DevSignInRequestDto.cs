using System.ComponentModel.DataAnnotations;

namespace ViewsLife.Api.Domains.Auth.Dtos;

/// <summary>
/// Temporary request used only for local development.
/// This allows the API to create or find a user without implementing
/// the full Apple authentication flow yet.
///
/// Context:
/// - This is a bridge for development and testing.
/// - It should be removed or disabled outside Development.
/// </summary>
public sealed class DevSignInRequestDto
{
    /// <summary>
    /// Display name for the user profile.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Email for the user profile if available.
    /// </summary>
    [EmailAddress]
    [MaxLength(320)]
    public string? Email { get; set; }

    /// <summary>
    /// Authentication provider name, such as Apple, Google, or Local.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string AuthProvider { get; set; } = string.Empty;

    /// <summary>
    /// Provider-specific stable identifier for the user.
    /// Example:
    /// - Apple subject claim
    /// - Google subject claim
    /// - A local development identifier for temporary testing
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string ProviderSubjectId { get; set; } = string.Empty;
}