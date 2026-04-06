namespace ViewsLife.Api.Domains.Auth.Dtos;

/// Represents the payload sent by the frontend when completing
/// a Sign in with Apple authentication flow.
public sealed class AppleSignInRequestDto
{
    public string IdentityToken { get; set; } = string.Empty;
}