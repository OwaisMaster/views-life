using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViewsLife.Api.Common.Constants;
using ViewsLife.Api.Domains.Auth.Dtos;
using ViewsLife.Api.Domains.Auth.Interfaces;

namespace ViewsLife.Api.Controllers;

/// Exposes auth endpoints for local registration/sign-in and future provider flows.
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _environment;

    /// Creates a new auth controller instance.
    public AuthController(IAuthService authService, IWebHostEnvironment environment)
    {
        _authService = authService;
        _environment = environment;
    }

    /// Registers a new local account and bootstraps its tenant.
    /// Route: POST /api/auth/register
    /// <returns>Authenticated session response.</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseDto>> Register(
        [FromBody] RegisterRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            AuthResponseDto response =
                await _authService.RegisterLocalAsync(request, cancellationToken);

            await IssueAuthCookieAsync(response);

            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    /// Signs an existing local account in.
    /// Route: POST /api/auth/sign-in
    /// <returns>Authenticated session response.</returns>
    [HttpPost("sign-in")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> SignIn(
        [FromBody] SignInRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            AuthResponseDto response =
                await _authService.SignInLocalAsync(request, cancellationToken);

            await IssueAuthCookieAsync(response);

            return Ok(response);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    /// Returns the current authenticated user and tenant context.
    /// Route: GET /api/auth/me
    /// <returns>Current user payload.</returns>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponseDto>> Me(
        CancellationToken cancellationToken)
    {
        string? currentUserId = User.FindFirstValue(AuthConstants.UserIdClaimType);
        string? currentTenantId = User.FindFirstValue(AuthConstants.TenantIdClaimType);

        CurrentUserResponseDto response =
            await _authService.GetCurrentUserAsync(
                currentUserId,
                currentTenantId,
                cancellationToken);

        return Ok(response);
    }

    /// Clears the current auth cookie.
    /// Route: POST /api/auth/sign-out
    [Authorize(AuthenticationSchemes = AuthConstants.AuthScheme)]
    [HttpPost("sign-out")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SignOut()
    {
        await HttpContext.SignOutAsync(AuthConstants.AuthScheme);
        return NoContent();
    }

    /// Placeholder Apple sign-in endpoint.
    [HttpPost("apple")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponseDto>> AppleSignIn(
        [FromBody] AppleSignInRequestDto request,
        CancellationToken cancellationToken)
    {
        AuthResponseDto response =
            await _authService.SignInWithAppleAsync(request, cancellationToken);

        await IssueAuthCookieAsync(response);

        return Ok(response);
    }

    /// Issues the application auth cookie using the supplied session context.
    /// <param name="response">Authenticated session payload.</param>
    private async Task IssueAuthCookieAsync(AuthResponseDto response)
    {
        var claims = new List<Claim>
        {
            new(AuthConstants.UserIdClaimType, response.UserId),
            new(AuthConstants.TenantIdClaimType, response.TenantId),
            new(AuthConstants.TenantRoleClaimType, response.TenantRole),
            new(ClaimTypes.NameIdentifier, response.UserId),
            new(ClaimTypes.Name, response.DisplayName),
            new("auth_provider", response.AuthProvider)
        };

        var identity = new ClaimsIdentity(claims, AuthConstants.AuthScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            AuthConstants.AuthScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                IssuedUtc = DateTimeOffset.UtcNow
            });
    }
}