using ViewsLife.Api.Domains.Auth.Dtos;

namespace ViewsLife.Api.Domains.Auth.Interfaces;

/// Defines authentication-related application operations.
public interface IAuthService
{
    /// Temporary development sign-in that finds or creates a user row.
    /// <param name="request">The development sign-in payload.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The authenticated user response.</returns>
    Task<AuthResponseDto> SignInForDevelopmentAsync(
        DevSignInRequestDto request,
        CancellationToken cancellationToken = default);

    /// Returns the current user for the given application user identifier.
    /// <param name="userId">The application user identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The current-user response if found; otherwise unauthenticated response.</returns>
    Task<CurrentUserResponseDto> GetCurrentUserAsync(
        string? userId,
        CancellationToken cancellationToken = default);

    /// Validates an Apple sign-in identity token and signs the user in.
    /// This will replace the development path as the real provider flow.
    /// <param name="request">The Apple sign-in payload.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The authentication response.</returns>
    Task<AuthResponseDto> SignInWithAppleAsync(
        AppleSignInRequestDto request,
        CancellationToken cancellationToken = default);
}