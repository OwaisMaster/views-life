using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ViewsLife.Api.Domains.Auth.Dtos;
using ViewsLife.Api.Domains.Auth.Entities;
using ViewsLife.Api.Infrastructure.Persistence;
using Xunit;

namespace ViewsLife.Api.IntegrationTests.Auth;

/// <summary>
/// Integration tests for local account registration, sign-in, and current-user bootstrap.
///
/// Context:
/// - Registration and sign-in continue to verify that a real auth cookie is issued.
/// - The protected /api/auth/me test uses the integration-test auth scheme and
///   seeded DB state so it stays deterministic in CI.
/// </summary>
public sealed class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Creates an HTTPS client for integration tests.
    /// </summary>
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
        // Seeds a user + tenant + owner membership directly in the integration DB.
        const string userId = "integration-user-001";
        const string tenantId = "integration-tenant-001";

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDbContext dbContext =
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = new ApplicationUser
            {
                Id = userId,
                DisplayName = "Bootstrap User",
                Email = "bootstrap.user@example.com",
                NormalizedEmail = "BOOTSTRAP.USER@EXAMPLE.COM",
                AuthProvider = "Local",
                ProviderSubjectId = "BOOTSTRAP.USER@EXAMPLE.COM",
                PasswordHash = "seeded-hash-not-used-here",
                IsEmailVerified = true,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            var tenant = new Tenant
            {
                Id = tenantId,
                Name = "Bootstrap Space",
                Slug = "bootstrap-space",
                OwnerUserId = userId,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            var membership = new TenantMembership
            {
                Id = "integration-membership-001",
                TenantId = tenantId,
                UserId = userId,
                Role = "Owner",
                CreatedAtUtc = DateTime.UtcNow
            };

            dbContext.Users.Add(user);
            dbContext.Tenants.Add(tenant);
            dbContext.TenantMemberships.Add(membership);

            await dbContext.SaveChangesAsync();
        }

        using HttpClient client = CreateClient();

        // Sends the request as an authenticated integration-test user.
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Add(TestAuthHandler.UserIdHeader, userId);
        request.Headers.Add(TestAuthHandler.TenantIdHeader, tenantId);
        request.Headers.Add(TestAuthHandler.TenantRoleHeader, "Owner");
        request.Headers.Add(TestAuthHandler.DisplayNameHeader, "Bootstrap User");

        HttpResponseMessage meResponse = await client.SendAsync(request);

        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        CurrentUserResponseDto? payload =
            await meResponse.Content.ReadFromJsonAsync<CurrentUserResponseDto>();

        payload.Should().NotBeNull();
        payload!.IsAuthenticated.Should().BeTrue();
        payload.DisplayName.Should().Be("Bootstrap User");
        payload.TenantName.Should().Be("Bootstrap Space");
        payload.TenantSlug.Should().Be("bootstrap-space");
        payload.TenantRole.Should().Be("Owner");
        payload.UserId.Should().Be(userId);
        payload.TenantId.Should().Be(tenantId);
    }
}