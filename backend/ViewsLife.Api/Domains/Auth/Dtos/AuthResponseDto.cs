namespace ViewsLife.Api.Domains.Auth.Dtos;

/// Represents the authentication response returned to the frontend
/// after a successful sign-in operation.
public sealed class AuthResponseDto
{
    /// The application-issued access token.
    /// For now this is placeholder data until JWT issuing is implemented.
    public string AccessToken { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}