using ViewsLife.Api.Domains.Auth.Dtos;
using ViewsLife.Api.Domains.Auth.Entities;
using ViewsLife.Api.Domains.Auth.Interfaces;

namespace ViewsLife.Api.Domains.Auth.Services;

/// Auth application service for development and future provider-backed sign-in flows.
public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;

    /// Creates a new auth service instance.
    /// <param name="userRepository">Repository used for user persistence operations.</param>
    public AuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<AuthResponseDto> SignInForDevelopmentAsync(
        DevSignInRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // Attempts to find an existing user first based on provider identity.
        ApplicationUser? existingUser = await _userRepository.GetByProviderAsync(
            request.AuthProvider,
            request.ProviderSubjectId,
            cancellationToken);

        ApplicationUser user;

        if (existingUser is not null)
        {
            user = existingUser;
        }
        else
        {
            // Creates a new persisted user when one does not already exist.
            user = new ApplicationUser
            {
                DisplayName = request.DisplayName,
                Email = request.Email,
                AuthProvider = request.AuthProvider,
                ProviderSubjectId = request.ProviderSubjectId,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);
        }

        return new AuthResponseDto
        {
            UserId = user.Id,
            DisplayName = user.DisplayName,
            IsAuthenticated = true,
            AuthProvider = user.AuthProvider
        };
    }

    public async Task<CurrentUserResponseDto> GetCurrentUserAsync(
        string? userId,
        CancellationToken cancellationToken = default)
    {
        // Returns an unauthenticated result if no user id is present in claims.
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new CurrentUserResponseDto
            {
                UserId = string.Empty,
                DisplayName = string.Empty,
                IsAuthenticated = false
            };
        }

        // Loads the current user from the database.
        ApplicationUser? user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return new CurrentUserResponseDto
            {
                UserId = string.Empty,
                DisplayName = string.Empty,
                IsAuthenticated = false
            };
        }

        return new CurrentUserResponseDto
        {
            UserId = user.Id,
            DisplayName = user.DisplayName,
            IsAuthenticated = true
        };
    }

    public Task<AuthResponseDto> SignInWithAppleAsync(
        AppleSignInRequestDto request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Apple sign-in validation has not been implemented yet.");
    }
}