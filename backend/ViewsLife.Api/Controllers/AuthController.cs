using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViewsLife.Api.Common.Constants;
using ViewsLife.Api.Domains.Auth.Dtos;
using ViewsLife.Api.Domains.Auth.Interfaces;
using ViewsLife.Api.Infrastructure.Logging;

namespace ViewsLife.Api.Controllers;

/// Exposes auth endpoints for local registration/sign-in and future provider flows.
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAuditLogger _auditLogger;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AuthController> _logger;

    /// Creates a new auth controller instance.
    public AuthController(
        IAuthService authService,
        IAuditLogger auditLogger,
        IWebHostEnvironment environment,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _auditLogger = auditLogger;
        _environment = environment;
        _logger = logger;
    }

    /// Registers a new local account and bootstraps its tenant.
    /// Route: POST /api/auth/register
    /// <returns>Authenticated session response.</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseDto>> Register(
        [FromBody] RegisterRequestDto request,
        CancellationToken cancellationToken)
    {
        // Model validation is automatic via [ApiController] and validation attributes.
        // If we reach here, the request passed all validation.
        try
        {
            AuthResponseDto response =
                await _authService.RegisterLocalAsync(request, cancellationToken);

            // Log successful registration
            await _auditLogger.LogAuditEventAsync(
                AuditEventType.RegistrationSucceeded,
                response.UserId,
                request.Email,
                GetClientIp(),
                $"Tenant: {response.TenantId}");

            await IssueAuthCookieAsync(response);

            return Ok(response);
        }
        catch (InvalidOperationException)
        {
            // Log registration failure
            await _auditLogger.LogAuditEventAsync(
                AuditEventType.RegistrationFailed,
                "[unknown]",
                request.Email,
                GetClientIp(),
                "Duplicate email or other validation failure");

            // Generic response for duplicate email to prevent account enumeration.
            // Do not expose the specific reason for the 400 error.
            return BadRequest(new ErrorResponseDto
            {
                Message = "Registration failed. Please check your input and try again.",
                ErrorCode = "REGISTRATION_FAILED"
            });
        }
    }

    /// Signs an existing local account in.
    /// Route: POST /api/auth/sign-in
    /// <returns>Authenticated session response.</returns>
    [HttpPost("sign-in")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthResponseDto>> SignIn(
        [FromBody] SignInRequestDto request,
        CancellationToken cancellationToken)
    {
        // Model validation is automatic via [ApiController] and validation attributes.
        // If we reach here, the request passed all validation.
        try
        {
            AuthResponseDto response =
                await _authService.SignInLocalAsync(request, cancellationToken);

            // Log successful sign-in
            await _auditLogger.LogAuditEventAsync(
                AuditEventType.SignInSucceeded,
                response.UserId,
                request.Email,
                GetClientIp(),
                $"Tenant: {response.TenantId}");

            await IssueAuthCookieAsync(response);

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex) when (ex.Message.Contains("temporarily locked"))
        {
            // Log account lockout
            await _auditLogger.LogAuditEventAsync(
                AuditEventType.AccountLockedOut,
                "[unknown]",
                request.Email,
                GetClientIp(),
                "Account locked due to repeated failed attempts");

            // Account is locked due to repeated failed attempts
            return StatusCode(
                StatusCodes.Status429TooManyRequests,
                new ErrorResponseDto
                {
                    Message = "Too many failed sign-in attempts. Please try again later.",
                    ErrorCode = "ACCOUNT_LOCKED"
                });
        }
        catch (UnauthorizedAccessException)
        {
            // Log failed sign-in
            await _auditLogger.LogAuditEventAsync(
                AuditEventType.SignInFailed,
                "[unknown]",
                request.Email,
                GetClientIp(),
                "Invalid credentials");

            // Generic response for invalid credentials to prevent account enumeration.
            // Do not reveal whether the email exists or password is wrong.
            return Unauthorized(new ErrorResponseDto
            {
                Message = "Invalid credentials.",
                ErrorCode = "INVALID_CREDENTIALS"
            });
        }
        catch (InvalidOperationException)
        {
            // Log sign-in failure
            await _auditLogger.LogAuditEventAsync(
                AuditEventType.SignInFailed,
                "[unknown]",
                request.Email,
                GetClientIp(),
                "Sign-in failed (no active tenant)");

            // Other validation failures (e.g., inactive account, no tenant)
            return BadRequest(new ErrorResponseDto
            {
                Message = "Sign-in failed. Please check your input and try again.",
                ErrorCode = "SIGNIN_FAILED"
            });
        }
    }

    /// Returns the current authenticated user and tenant context.
    /// Route: GET /api/auth/me
    /// <returns>Current user payload.</returns>
    [Authorize(AuthenticationSchemes = AuthConstants.AuthScheme)]
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponseDto>> Me(
        CancellationToken cancellationToken)
    {
        // Read the raw Cookie header only to determine presence/length.
        // Do NOT log the actual cookie value.
        string cookieHeader = HttpContext.Request.Headers.Cookie.ToString();

        // Force an explicit authentication evaluation for the configured cookie scheme.
        // This helps diagnose whether ASP.NET can validate the auth cookie.
        AuthenticateResult authResult =
            await HttpContext.AuthenticateAsync(AuthConstants.AuthScheme);

        // Log a structured diagnostic event for this request.
        _logger.LogInformation(
            "Auth /me diagnostics. Path={Path}, HasCookieHeader={HasCookieHeader}, CookieHeaderLength={CookieHeaderLength}, AuthSucceeded={AuthSucceeded}, AuthNone={AuthNone}, AuthFailureMessage={AuthFailureMessage}, IdentityIsAuthenticated={IdentityIsAuthenticated}, UserIdClaimPresent={UserIdClaimPresent}, TenantIdClaimPresent={TenantIdClaimPresent}, ClientIp={ClientIp}",
            HttpContext.Request.Path,
            !string.IsNullOrWhiteSpace(cookieHeader),
            cookieHeader.Length,
            authResult.Succeeded,
            authResult.None,
            authResult.Failure?.Message,
            User.Identity?.IsAuthenticated ?? false,
            User.HasClaim(c => c.Type == AuthConstants.UserIdClaimType),
            User.HasClaim(c => c.Type == AuthConstants.TenantIdClaimType),
            GetClientIp());

        string? currentUserId = User.FindFirstValue(AuthConstants.UserIdClaimType);
        string? currentTenantId = User.FindFirstValue(AuthConstants.TenantIdClaimType);

         // Log the resolved identity values that the action sees after authorization.
        _logger.LogInformation(
            "Auth /me resolved claims. CurrentUserId={CurrentUserId}, CurrentTenantId={CurrentTenantId}, DisplayName={DisplayName}",
            currentUserId,
            currentTenantId,
            User.Identity?.Name);

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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SignOut()
    {
        var userId = User.FindFirstValue(AuthConstants.UserIdClaimType);
        var email = User.FindFirstValue(ClaimTypes.Name);

        // Log sign-out
        await _auditLogger.LogAuditEventAsync(
            AuditEventType.SignOutOccurred,
            userId ?? "[unknown]",
            null,
            GetClientIp(),
            null);

        await HttpContext.SignOutAsync(AuthConstants.AuthScheme);
        return NoContent();
    }

    /// Placeholder Apple sign-in endpoint.
    [HttpPost("apple")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseDto>> AppleSignIn(
        [FromBody] AppleSignInRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            AuthResponseDto response =
                await _authService.SignInWithAppleAsync(request, cancellationToken);

            await IssueAuthCookieAsync(response);

            return Ok(response);
        }
        catch (NotImplementedException)
        {
            return BadRequest(new ErrorResponseDto
            {
                Message = "Apple sign-in is not yet available.",
                ErrorCode = "NOT_IMPLEMENTED"
            });
        }
    }

    /// <summary>
    /// Gets the client IP address from the request.
    /// </summary>
    private string GetClientIp()
    {
        // Check X-Forwarded-For header (for proxies)
        if (HttpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
        {
            var ips = forwarded.ToString().Split(',');
            if (ips.Length > 0 && !string.IsNullOrWhiteSpace(ips[0]))
            {
                return ips[0].Trim();
            }
        }

        // Fall back to remote IP
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
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