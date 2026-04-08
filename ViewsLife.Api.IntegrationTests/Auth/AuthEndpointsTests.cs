using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ViewsLife.Api.Domains.Auth.Dtos;
using Xunit;
using Xunit.Abstractions;

namespace ViewsLife.Api.IntegrationTests.Auth;

/// <summary>
/// Integration tests for local account registration, sign-in, and session bootstrap.
///
/// Context:
/// - Uses the full API host through WebApplicationFactory.
/// - Adds explicit diagnostics so cookie/auth failures in CI can be pinpointed.
/// </summary>
public sealed class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public AuthEndpointsTests(
        CustomWebApplicationFactory factory,
        ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    /// <summary>
    /// Creates an HTTPS client with automatic cookie handling enabled.
    /// </summary>
    private HttpClient CreateCookieClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost")
        });
    }

    /// <summary>
    /// Creates an HTTPS client with automatic cookie handling disabled.
    /// Use this when manually sending a Cookie header.
    /// </summary>
    private HttpClient CreateManualCookieClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });
    }

    /// <summary>
    /// Extracts the first cookie pair from a Set-Cookie header.
    ///
    /// Example:
    /// "viewslife_auth=abc123; expires=...; path=/; secure; httponly"
    /// becomes:
    /// "viewslife_auth=abc123"
    /// </summary>
    /// <param name="setCookieHeader">Raw Set-Cookie header value.</param>
    /// <returns>The cookie pair suitable for a Cookie request header.</returns>
    private static string ExtractCookiePair(string setCookieHeader)
    {
        return setCookieHeader.Split(';', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
    }

    [Fact]
    public async Task Register_ShouldCreateUserAndTenant_AndSetAuthCookie()
    {
        using HttpClient client = CreateCookieClient();

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

        foreach (string header in cookieHeaders!)
        {
            _output.WriteLine($"Register Set-Cookie header: {header}");
        }

        cookieHeaders.Any(value => value.Contains("viewslife_auth"))
            .Should().BeTrue();
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenEmailAlreadyExists()
    {
        using HttpClient client = CreateCookieClient();

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
        using HttpClient registerClient = CreateCookieClient();

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

        using HttpClient signInClient = CreateCookieClient();

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

        foreach (string header in cookieHeaders!)
        {
            _output.WriteLine($"SignIn Set-Cookie header: {header}");
        }

        cookieHeaders.Any(value => value.Contains("viewslife_auth"))
            .Should().BeTrue();
    }

    [Fact]
    public async Task Me_ShouldReturnTenantContext_WhenAuthenticated()
    {
        using HttpClient registerClient = CreateCookieClient();

        var request = new RegisterRequestDto
        {
            DisplayName = "Bootstrap User",
            Email = "bootstrap.user@example.com",
            Password = "Password!123",
            TenantName = "Bootstrap Space"
        };

        HttpResponseMessage registerResponse = await registerClient.PostAsJsonAsync(
            "/api/auth/register",
            request);

        _output.WriteLine($"Register response status: {registerResponse.StatusCode}");

        string registerBody = await registerResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"Register response body: {registerBody}");

        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        registerResponse.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookieHeaders)
            .Should().BeTrue();

        cookieHeaders.Should().NotBeNull();

        var cookieHeaderList = cookieHeaders!.ToList();

        foreach (string header in cookieHeaderList)
        {
            _output.WriteLine($"Register Set-Cookie header: {header}");
        }

        string authCookieHeader = cookieHeaderList
            .First(header => header.Contains("viewslife_auth"));

        string cookiePair = ExtractCookiePair(authCookieHeader);

        _output.WriteLine($"Extracted auth cookie pair: {cookiePair}");

        using HttpClient meClient = CreateManualCookieClient();

        var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        meRequest.Headers.Add("Cookie", cookiePair);

        _output.WriteLine($"Outgoing /api/auth/me Cookie header: {cookiePair}");

        HttpResponseMessage meResponse = await meClient.SendAsync(meRequest);

        _output.WriteLine($"Me response status: {meResponse.StatusCode}");

        string meBody = await meResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"Me response body: {meBody}");

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