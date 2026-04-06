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
    /// This endpoint creates or finds a user and stores the user identifier in a development cookie.
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
        // Protects the endpoint so it only exists as a local development bridge.
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        AuthResponseDto response =
            await _authService.SignInForDevelopmentAsync(request, cancellationToken);

        // Stores the current application user identifier in a secure development cookie.
        // This is a temporary bootstrap mechanism until real authentication is wired in.
        Response.Cookies.Append(
            AuthConstants.DevUserIdCookieName,
            response.UserId,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true
            });

        return Ok(response);
    }

    /// Returns the current persisted user based on the development cookie.
    /// Route: GET /api/auth/me
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current user response.</returns>
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CurrentUserResponseDto>> Me(
        CancellationToken cancellationToken)
    {
        // Reads the current user identifier from the development cookie.
        Request.Cookies.TryGetValue(
            AuthConstants.DevUserIdCookieName,
            out string? currentUserId);

        CurrentUserResponseDto response =
            await _authService.GetCurrentUserAsync(currentUserId, cancellationToken);

        return Ok(response);
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