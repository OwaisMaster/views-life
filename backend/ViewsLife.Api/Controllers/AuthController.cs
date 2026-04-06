using Microsoft.AspNetCore.Mvc;
using ViewsLife.Api.Domains.Auth.Dtos;
using ViewsLife.Api.Domains.Auth.Interfaces;

namespace ViewsLife.Api.Controllers;

/// Exposes authentication endpoints for frontend sign-in and session bootstrap.
/// These endpoints are intentionally simple at this stage so the contracts can be
/// proven before real Apple auth and JWT issuance are implemented.
[ApiController]
[Route("api/[controller]")]

public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    /// Creates a new controller instance with the required auth service dependency.
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// Accepts a placeholder Apple sign-in payload and returns a placeholder auth response.
    /// Route: POST /api/auth/apple
    [HttpPost("apple")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponseDto>> AppleSignIn(
        [FromBody] AppleSignInRequestDto request)
    {
        var response = await _authService.SignInWithAppleAsync(request);
        return Ok(response);
    }

    /// Returns placeholder current-user data for frontend bootstrap.
    /// Route: GET /api/auth/me
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CurrentUserResponseDto>> Me()
    {
        var response = await _authService.GetCurrentUserAsync();
        return Ok(response);
    }
}