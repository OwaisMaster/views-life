using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ViewsLife.Api.Domains.Auth.Dtos;
using Xunit;

namespace ViewsLife.Api.IntegrationTests.Auth;

/// Integration tests for local account registration, sign-in, and session bootstrap.
///
/// Context:
/// - Uses the full API host through WebApplicationFactory.
/// - Verifies user + tenant bootstrap through real HTTP flows.
/// - Verifies cookie-authenticated /api/auth/me behavior end to end.
public sealed class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// Creates a fresh HTTPS client with cookie handling enabled.
    ///
    /// Important:
    /// - The auth cookie is marked Secure.
    /// - Secure cookies are only sent on HTTPS requests.
    /// - WebApplicationFactory defaults to http://localhost, which would prevent
    ///   the auth cookie from being sent back on /api/auth/me.
    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task Register_ShouldCreateUserAndTenant_AndSetAuthCookie()
    {
        using HttpClient client = CreateClient();

        var request = new RegisterRequestDto
        {
            DisplayName = "Integration Register User",
            Email = "integration.register@example.com",
            Password = "Password!123",
            TenantName = "Integration Register Space"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        AuthResponseDto? payload =
            await response.Content.ReadFromJsonAsync<AuthResponseDto>();

        payload.Should().NotBeNull();
        payload!.IsAuthenticated.Should().BeTrue();
        payload.DisplayName.Should().Be("Integration Register User");
        payload.TenantName.Should().Be("Integration Register Space");
        payload.TenantRole.Should().Be("Owner");

        response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookieHeaders)
            .Should().BeTrue();

        cookieHeaders.Should().NotBeNull();
        cookieHeaders!.Any(value => value.Contains("viewslife_auth"))
            .Should().BeTrue();
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenEmailAlreadyExists()
    {
        using HttpClient client = CreateClient();

        var request = new RegisterRequestDto
        {
            DisplayName = "Duplicate User",
            Email = "duplicate.integration@example.com",
            Password = "Password!123",
            TenantName = "Duplicate Space"
        };

        HttpResponseMessage firstResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage secondResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SignIn_ShouldReturnAuthenticatedSession_WhenCredentialsValid()
    {
        using HttpClient registerClient = CreateClient();

        var registrationRequest = new RegisterRequestDto
        {
            DisplayName = "Integration Sign In User",
            Email = "integration.signin@example.com",
            Password = "Password!123",
            TenantName = "Integration Sign In Space"
        };

        HttpResponseMessage registrationResponse = await registerClient.PostAsJsonAsync(
            "/api/auth/register",
            registrationRequest);

        registrationResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using HttpClient signInClient = CreateClient();

        var signInRequest = new SignInRequestDto
        {
            Email = "integration.signin@example.com",
            Password = "Password!123"
        };

        HttpResponseMessage signInResponse = await signInClient.PostAsJsonAsync(
            "/api/auth/sign-in",
            signInRequest);

        signInResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        AuthResponseDto? payload =
            await signInResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        payload.Should().NotBeNull();
        payload!.IsAuthenticated.Should().BeTrue();
        payload.TenantName.Should().Be("Integration Sign In Space");
        payload.TenantRole.Should().Be("Owner");

        signInResponse.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookieHeaders)
            .Should().BeTrue();

        cookieHeaders.Should().NotBeNull();
        cookieHeaders!.Any(value => value.Contains("viewslife_auth"))
            .Should().BeTrue();
    }

    [Fact]
    public async Task Me_ShouldReturnTenantContext_WhenAuthenticated()
    {
        using HttpClient client = CreateClient();

        var request = new RegisterRequestDto
        {
            DisplayName = "Bootstrap User",
            Email = "bootstrap.user@example.com",
            Password = "Password!123",
            TenantName = "Bootstrap Space"
        };

        HttpResponseMessage registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage meResponse = await client.GetAsync("/api/auth/me");

        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        CurrentUserResponseDto? payload =
            await meResponse.Content.ReadFromJsonAsync<CurrentUserResponseDto>();

        payload.Should().NotBeNull();
        payload!.IsAuthenticated.Should().BeTrue();
        payload.DisplayName.Should().Be("Bootstrap User");
        payload.TenantName.Should().Be("Bootstrap Space");
        payload.TenantSlug.Should().Be("bootstrap-space");
        payload.TenantRole.Should().Be("Owner");
        payload.UserId.Should().NotBeNullOrWhiteSpace();
        payload.TenantId.Should().NotBeNullOrWhiteSpace();
    }
}