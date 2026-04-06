namespace ViewsLife.Api.Domains.Auth.Dtos;

/// Represents the authentication response returned to the frontend
/// after a successful sign-in operation.
public sealed class AuthResponseDto
{
    /// The application user identifier.
    public string UserId { get; set; } = string.Empty;

    /// The user's display name.
    public string DisplayName { get; set; } = string.Empty;

    /// Indicates whether the user is authenticated successfully.
    public bool IsAuthenticated { get; set; }

    /// The authentication provider used for this sign-in result.
    public string AuthProvider { get; set; } = string.Empty;
}