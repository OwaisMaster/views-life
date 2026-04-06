using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViewsLife.Api.Common.Constants;
using ViewsLife.Api.Domains.Auth.Dtos;
using ViewsLife.Api.Domains.Auth.Interfaces;

namespace ViewsLife.Api.Controllers;

/// Exposes authentication endpoints for development bootstrap and future real provider flows.
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _environment;

    /// Creates a new auth controller instance.
    /// <param name="authService">Auth application service.</param>
    /// <param name="environment">Hosting environment used to gate development-only endpoints.</param>
    public AuthController(IAuthService authService, IWebHostEnvironment environment)
    {
        _authService = authService;
        _environment = environment;
    }

    /// Temporary development sign-in endpoint.
    /// This endpoint creates or finds a user and issues a real ASP.NET Core auth cookie.
    /// It must not be callable outside Development.
    ///
    /// Route: POST /api/auth/dev-sign-in
    /// <param name="request">Development sign-in payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authenticated user response.</returns>
    [HttpPost("dev-sign-in")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuthResponseDto>> DevSignIn(
        [FromBody] DevSignInRequestDto request,
        CancellationToken cancellationToken)
    {
        // Restricts the endpoint to Development only.
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        AuthResponseDto response =
            await _authService.SignInForDevelopmentAsync(request, cancellationToken);

        // Builds claims for the authenticated application user.
        var claims = new List<Claim>
        {
            new(AuthConstants.UserIdClaimType, response.UserId),
            new(ClaimTypes.NameIdentifier, response.UserId),
            new(ClaimTypes.Name, response.DisplayName),
            new("auth_provider", response.AuthProvider)
        };

        // Creates the claims identity and principal used by cookie authentication.
        var identity = new ClaimsIdentity(claims, AuthConstants.AuthScheme);
        var principal = new ClaimsPrincipal(identity);

        // Issues the real authentication cookie through ASP.NET Core auth middleware.
        await HttpContext.SignInAsync(
            AuthConstants.AuthScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                IssuedUtc = DateTimeOffset.UtcNow
            });

        return Ok(response);
    }

    /// Returns the current persisted user based on the authenticated claims principal.
    /// Route: GET /api/auth/me
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current user response.</returns>
    [Authorize(AuthenticationSchemes = AuthConstants.AuthScheme)]
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponseDto>> Me(
        CancellationToken cancellationToken)
    {
        // Reads the application user identifier from authenticated claims.
        string? currentUserId = User.FindFirstValue(AuthConstants.UserIdClaimType);

        CurrentUserResponseDto response =
            await _authService.GetCurrentUserAsync(currentUserId, cancellationToken);

        return Ok(response);
    }

    /// Signs the current user out by clearing the auth cookie.
    /// Route: POST /api/auth/sign-out
    /// <returns>No content when sign-out succeeds.</returns>
    [Authorize(AuthenticationSchemes = AuthConstants.AuthScheme)]
    [HttpPost("sign-out")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SignOut()
    {
        await HttpContext.SignOutAsync(AuthConstants.AuthScheme);
        return NoContent();
    }

    /// Placeholder Apple sign-in endpoint.
    /// Route: POST /api/auth/apple
    /// <param name="request">The Apple sign-in payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Apple sign-in response.</returns>
    [HttpPost("apple")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponseDto>> AppleSignIn(
        [FromBody] AppleSignInRequestDto request,
        CancellationToken cancellationToken)
    {
        AuthResponseDto response =
            await _authService.SignInWithAppleAsync(request, cancellationToken);

        return Ok(response);
    }
}