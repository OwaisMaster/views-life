using ViewsLife.Api.Domains.Auth.Dtos;

namespace ViewsLife.Api.Domains.Auth.Interfaces;

/// Defines authentication-related application operations.
public interface IAuthService
{
    /// Registers a new local account and bootstraps its tenant.
    /// <returns>Authenticated session response for the new user.</returns>
    Task<AuthResponseDto> RegisterLocalAsync(
        RegisterRequestDto request,
        CancellationToken cancellationToken = default);

    /// Signs a local account in using email and password.
    /// <returns>Authenticated session response.</returns>
    Task<AuthResponseDto> SignInLocalAsync(
        SignInRequestDto request,
        CancellationToken cancellationToken = default);

    /// Loads the current authenticated user and tenant context.
    /// <returns>Current-user bootstrap response.</returns>
    Task<CurrentUserResponseDto> GetCurrentUserAsync(
        string? userId,
        string? tenantId,
        CancellationToken cancellationToken = default);

    /// Placeholder for future Apple Sign-In flow.
    Task<AuthResponseDto> SignInWithAppleAsync(
        AppleSignInRequestDto request,
        CancellationToken cancellationToken = default);
}