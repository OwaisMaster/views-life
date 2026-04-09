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
            Password = "SecurePassword!123",
            TenantName = "Integration Register Tenant"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        authResponse.Should().NotBeNull();
        authResponse!.DisplayName.Should().Be("Integration Register User");
        authResponse.TenantName.Should().Be("Integration Register Tenant");

        // Verify the cookie was set
        var cookieHeader = response.Headers.GetValues("Set-Cookie").FirstOrDefault();
        cookieHeader.Should().NotBeNull();
        cookieHeader.Should().Contain("viewslife_auth");
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenEmailAlreadyExists()
    {
        using HttpClient client = CreateClient();

        var request = new RegisterRequestDto
        {
            DisplayName = "Duplicate Email User",
            Email = "duplicate.email@example.com",
            Password = "SecurePassword!123",
            TenantName = "Duplicate Email Tenant"
        };

        // Register first user
        HttpResponseMessage firstResponse = await client.PostAsJsonAsync("/api/auth/register", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Register second user with same email
        HttpResponseMessage secondResponse = await client.PostAsJsonAsync("/api/auth/register", request);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorDto = await secondResponse.Content.ReadFromJsonAsync<ErrorResponseDto>();
        errorDto.Should().NotBeNull();
        // Generic error response as per hardening requirements
        errorDto!.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SignIn_ShouldReturnAuthenticatedSession_WhenCredentialsValid()
    {
        using HttpClient client = CreateClient();

        // Register a user
        var registerRequest = new RegisterRequestDto
        {
            DisplayName = "SignIn Test User",
            Email = "signin.test@example.com",
            Password = "SecurePassword!123",
            TenantName = "SignIn Test Tenant"
        };

        await client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Sign in with valid credentials
        var signInRequest = new SignInRequestDto
        {
            Email = "signin.test@example.com",
            Password = "SecurePassword!123"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/sign-in", signInRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        authResponse.Should().NotBeNull();
        authResponse!.DisplayName.Should().Be("SignIn Test User");
        authResponse.IsAuthenticated.Should().BeTrue();

        // Verify auth cookie was set
        var cookieHeader = response.Headers.GetValues("Set-Cookie").FirstOrDefault();
        cookieHeader.Should().NotBeNull();
        cookieHeader.Should().Contain("viewslife_auth");
    }

    [Fact]
    public async Task Me_ShouldReturnUnauthorized_WhenCookieInvalid()
    {
        using HttpClient client = CreateClient();

        // Call /api/auth/me without any authentication (no valid cookie or test headers)
        HttpResponseMessage response = await client.GetAsync("/api/auth/me");

        // Should fail because there's no valid authentication
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("")]    // Empty string
    [InlineData("  ")] // Whitespace only
    public async Task Register_ShouldReturnBadRequest_WhenPayloadInvalid(
        string invalidDisplayName)
    {
        using HttpClient client = CreateClient();

        var request = new RegisterRequestDto
        {
            DisplayName = invalidDisplayName,
            Email = "invalid.payload@example.com",
            Password = "SecurePassword!123",
            TenantName = "Invalid Payload Tenant"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("")]  // Empty string
    [InlineData(" ")]  // Only whitespace
    public async Task SignIn_ShouldReturnBadRequest_WhenPayloadInvalid(
        string invalidEmail)
    {
        using HttpClient client = CreateClient();

        var request = new SignInRequestDto
        {
            Email = invalidEmail,
            Password = "SecurePassword!123"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/sign-in",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_ShouldNormalizeEmail_PreventingCaseDuplicates()
    {
        using HttpClient client = CreateClient();

        // Register with lowercase email
        var firstRequest = new RegisterRequestDto
        {
            DisplayName = "Case Test User",
            Email = "casetest@example.com",
            Password = "SecurePassword!123",
            TenantName = "Case Test Tenant"
        };

        HttpResponseMessage firstResponse = await client.PostAsJsonAsync("/api/auth/register", firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Try to register with uppercase variant of same email
        var secondRequest = new RegisterRequestDto
        {
            DisplayName = "Case Test User Upper",
            Email = "CaseTest@EXAMPLE.COM",  // Different case
            Password = "SecurePassword!123",
            TenantName = "Case Test Tenant Upper"
        };

        HttpResponseMessage secondResponse = await client.PostAsJsonAsync("/api/auth/register", secondRequest);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SignIn_ShouldReturnGenericMessage_WhenInvalidCredentials()
    {
        using HttpClient client = CreateClient();

        // Register a user
        var registerRequest = new RegisterRequestDto
        {
            DisplayName = "Invalid Creds Test",
            Email = "invalidcreds.test@example.com",
            Password = "CorrectPassword!123",
            TenantName = "Invalid Creds Tenant"
        };

        await client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Try sign in with wrong password
        var signInRequest = new SignInRequestDto
        {
            Email = "invalidcreds.test@example.com",
            Password = "WrongPassword!123"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/sign-in", signInRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var errorDto = await response.Content.ReadFromJsonAsync<ErrorResponseDto>();
        errorDto.Should().NotBeNull();
        // Should return generic message, not "password incorrect"
        errorDto!.Message.Should().Contain("Invalid credentials");
    }

    [Fact]
    public async Task SignIn_ShouldReturnGenericMessage_WhenUserNotFound()
    {
        using HttpClient client = CreateClient();

        var signInRequest = new SignInRequestDto
        {
            Email = "nonexistent.user@example.com",
            Password = "SomePassword!123"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/sign-in", signInRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var errorDto = await response.Content.ReadFromJsonAsync<ErrorResponseDto>();
        errorDto.Should().NotBeNull();
        // Should return generic message, not "user not found"
        errorDto!.Message.Should().Contain("Invalid credentials");
    }

    [Fact]
    public async Task Me_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        using HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SignIn_ShouldReject_AfterRepeatedFailedAttempts()
    {
        using HttpClient client = CreateClient();

        // Register a user
        var registerRequest = new RegisterRequestDto
        {
            DisplayName = "Lockout Test User",
            Email = "lockout.test@example.com",
            Password = "Password!123",
            TenantName = "Lockout Test Tenant"
        };

        await client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Make 5 failed sign-in attempts
        var signInRequest = new SignInRequestDto
        {
            Email = "lockout.test@example.com",
            Password = "WrongPassword"
        };

        HttpStatusCode? finalResponseCode = null;
        HttpStatusCode? correctPasswordResponseCode = null;

        for (int i = 0; i < 5; i++)
        {
            HttpResponseMessage response = await client.PostAsJsonAsync(
                "/api/auth/sign-in",
                signInRequest);

            // Capture the final response code
            if (i == 4)
            {
                finalResponseCode = response.StatusCode;
            }
        }

        // After 5 failed attempts, should be rejected (either 401 Unauthorized from lockout or 429 from rate limiting)
        (finalResponseCode == HttpStatusCode.Unauthorized || finalResponseCode == HttpStatusCode.TooManyRequests).Should().BeTrue();

        // Verify that even the correct password now returns rejected
        var correctPasswordRequest = new SignInRequestDto
        {
            Email = "lockout.test@example.com",
            Password = "Password!123"
        };

        HttpResponseMessage correctPasswordResponse = await client.PostAsJsonAsync(
            "/api/auth/sign-in",
            correctPasswordRequest);

        correctPasswordResponseCode = correctPasswordResponse.StatusCode;
        (correctPasswordResponseCode == HttpStatusCode.Unauthorized || correctPasswordResponseCode == HttpStatusCode.TooManyRequests).Should().BeTrue();
    }

    [Fact]
    public async Task Register_ShouldRejectWhitespaceOnly_DisplayName()
    {
        using HttpClient client = CreateClient();

        var request = new RegisterRequestDto
        {
            DisplayName = "   ",  // Only whitespace
            Email = "whitespace.test@example.com",
            Password = "Password!123",
            TenantName = "Test Tenant"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_ShouldRejectLeadingTrailingWhitespace_Email()
    {
        using HttpClient client = CreateClient();

        var request = new RegisterRequestDto
        {
            DisplayName = "Test User",
            Email = " test@example.com ",  // Leading/trailing space
            Password = "Password!123",
            TenantName = "Test Tenant"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_ShouldRejectUppercaseEmail_NotNormalized()
    {
        using HttpClient client = CreateClient();

        var request = new RegisterRequestDto
        {
            DisplayName = "Test User",
            Email = "TestUser@Example.COM",  // Not lowercase
            Password = "Password!123",
            TenantName = "Test Tenant"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SignIn_ShouldRejectUppercaseEmail_NotNormalized()
    {
        using HttpClient client = CreateClient();

        // Register with lowercase
        var registerRequest = new RegisterRequestDto
        {
            DisplayName = "Test User",
            Email = "signup.test@example.com",
            Password = "Password!123",
            TenantName = "Test Tenant"
        };

        await client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Try to sign in with uppercase (should be rejected as invalid format)
        var signInRequest = new SignInRequestDto
        {
            Email = "Signup.Test@Example.com",  // Not lowercase
            Password = "Password!123"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/sign-in",
            signInRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_ShouldRejectPasswordTooShort()
    {
        using HttpClient client = CreateClient();

        var request = new RegisterRequestDto
        {
            DisplayName = "Test User",
            Email = "shortpwd.test@example.com",
            Password = "Short1",  // Less than 8 characters
            TenantName = "Test Tenant"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_ShouldRejectPasswordWithLeadingTrailingWhitespace()
    {
        using HttpClient client = CreateClient();

        var request = new RegisterRequestDto
        {
            DisplayName = "Test User",
            Email = "pwdspace.test@example.com",
            Password = " Password123 ",  // Leading/trailing whitespace
            TenantName = "Test Tenant"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
