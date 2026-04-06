using ViewsLife.Api.Domains.Auth.Dtos;
using ViewsLife.Api.Domains.Auth.Interfaces;

namespace ViewsLife.Api.Domains.Auth.Services;

/// Temporary placeholder authentication service.
/// This provides stable endpoints and contracts before real Apple and JWT logic is added.
public sealed class AuthService : IAuthService
{
    /// Returns a placeholder authentication response.
    /// Later this method will validate the Apple identity token, locate or create
    /// the user, and issue a real application token.
    public Task<AuthResponseDto> SignInWithAppleAsync(AppleSignInRequestDto request)
    {
        var response = new AuthResponseDto
        {
            AccessToken = "placeholder-access-token",
            UserId = "placeholder-user-id",
            DisplayName = "ViewsLife User"
        };

        return Task.FromResult(response);
    }

    /// Returns a placeholder current-user payload.
    /// Later this will derive the user from the authenticated request context.
    public Task<CurrentUserResponseDto> GetCurrentUserAsync()
    {
        var response = new CurrentUserResponseDto
        {
            UserId = "placeholder-user-id",
            DisplayName = "ViewsLife User",
            IsAuthenticated = true
        };

        return Task.FromResult(response);
    }
}