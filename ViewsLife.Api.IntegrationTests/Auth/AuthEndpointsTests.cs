using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ViewsLife.Api.Domains.Auth.Dtos;
using Xunit;

namespace ViewsLife.Api.IntegrationTests.Auth;

/// Integration tests for Auth API endpoints.
/// These tests verify the assembled application behavior through HTTP requests.
public sealed class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task DevSignIn_ShouldCreateUser_AndSetCookie()
    {
        // Arranges a development sign-in payload.
        var request = new DevSignInRequestDto
        {
            DisplayName = "Integration Test User",
            Email = "integration@example.com",
            AuthProvider = "Local",
            ProviderSubjectId = "integration-local-001"
        };

        // Sends the sign-in request to the API.
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/auth/dev-sign-in",
            request);

        // Verifies the response is successful and the development cookie is present.
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookieHeaders)
            .Should().BeTrue();

        cookieHeaders.Should().NotBeNull();
        cookieHeaders!.Any(value => value.Contains("viewslife_dev_user_id"))
            .Should().BeTrue();
    }

    [Fact]
    public async Task Me_ShouldReturnUnauthenticated_WhenNoCookieExists()
    {
        // Calls the current-user endpoint without a sign-in cookie.
        HttpResponseMessage response = await _client.GetAsync("/api/auth/me");

        // Verifies the endpoint returns the unauthenticated state.
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<CurrentUserResponseDto>();

        payload.Should().NotBeNull();
        payload!.IsAuthenticated.Should().BeFalse();
    }
}