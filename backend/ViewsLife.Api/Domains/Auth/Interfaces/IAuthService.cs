using ViewsLife.Api.Domains.Auth.Dtos;

namespace ViewsLife.Api.Domains.Auth.Interfaces;

public interface IAuthService
{
    /// Handles Sign in with Apple using the provided identity token.
    /// <param name="request">The Apple sign-in request payload.</param>
    /// <returns>A placeholder authentication result.</returns>
    Task<AuthResponseDto> SignInWithAppleAsync(AppleSignInRequestDto request);

  /// Returns the current user context for frontend bootstrap scenarios.
  /// <returns>A placeholder current user response.</returns>
    Task<CurrentUserResponseDto> GetCurrentUserAsync();
}