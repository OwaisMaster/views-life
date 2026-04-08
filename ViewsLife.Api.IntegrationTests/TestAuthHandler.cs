using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ViewsLife.Api.IntegrationTests;

/// <summary>
/// Test authentication handler used only by integration tests.
///
/// Context:
/// - Produces an authenticated ClaimsPrincipal when the required test headers exist.
/// - Allows protected endpoints to be exercised deterministically in CI without
///   depending on cookie round-trip/decryption behavior.
/// </summary>
public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>
    /// Name of the integration-test authentication scheme.
    /// </summary>
    public const string SchemeName = "IntegrationTestScheme";

    /// <summary>
    /// Header used to pass the application user identifier.
    /// </summary>
    public const string UserIdHeader = "X-Test-UserId";

    /// <summary>
    /// Header used to pass the current tenant identifier.
    /// </summary>
    public const string TenantIdHeader = "X-Test-TenantId";

    /// <summary>
    /// Header used to pass the current tenant role.
    /// </summary>
    public const string TenantRoleHeader = "X-Test-TenantRole";

    /// <summary>
    /// Header used to pass the display name.
    /// </summary>
    public const string DisplayNameHeader = "X-Test-DisplayName";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    /// <summary>
    /// Authenticates the current request using integration-test headers.
    /// </summary>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserIdHeader, out var userIdValues) ||
            !Request.Headers.TryGetValue(TenantIdHeader, out var tenantIdValues))
        {
            return Task.FromResult(AuthenticateResult.Fail(
                "Missing required integration test auth headers."));
        }

        string userId = userIdValues.ToString();
        string tenantId = tenantIdValues.ToString();
        string tenantRole = Request.Headers.TryGetValue(TenantRoleHeader, out var roleValues)
            ? roleValues.ToString()
            : "Owner";
        string displayName = Request.Headers.TryGetValue(DisplayNameHeader, out var nameValues)
            ? nameValues.ToString()
            : "Integration Test User";

        var claims = new List<Claim>
        {
            new("viewslife_user_id", userId),
            new("viewslife_tenant_id", tenantId),
            new("viewslife_tenant_role", tenantRole),
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, displayName)
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}