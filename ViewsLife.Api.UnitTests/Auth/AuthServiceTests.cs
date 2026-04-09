using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using ViewsLife.Api.Domains.Auth.Dtos;
using ViewsLife.Api.Domains.Auth.Entities;
using ViewsLife.Api.Domains.Auth.Interfaces;
using ViewsLife.Api.Domains.Auth.Services;
using ViewsLife.Api.Infrastructure.Persistence;
using Xunit;

namespace ViewsLife.Api.UnitTests.Auth;

public sealed class AuthServiceTests
{
    private readonly Mock<ILockoutService> _lockoutServiceMock;

    public AuthServiceTests()
    {
        _lockoutServiceMock = new Mock<ILockoutService>();
    }

    [Fact]
    public async Task RegisterLocalAsync_ShouldCreateUserTenantAndOwnerMembership()
    {
        // Creates an isolated relational test database for this test case.
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var service = new AuthService(dbContext, _lockoutServiceMock.Object);

        var request = new RegisterRequestDto
        {
            DisplayName = "Test User",
            Email = "test@example.com",
            Password = "Password!123",
            TenantName = "Test Space"
        };

        // Executes the registration + tenant bootstrap flow.
        AuthResponseDto response = await service.RegisterLocalAsync(request);

        // Verifies the returned authenticated session context.
        response.IsAuthenticated.Should().BeTrue();
        response.DisplayName.Should().Be("Test User");
        response.AuthProvider.Should().Be("Local");
        response.TenantName.Should().Be("Test Space");
        response.TenantSlug.Should().Be("test-space");
        response.TenantRole.Should().Be("Owner");
        response.UserId.Should().NotBeNullOrWhiteSpace();
        response.TenantId.Should().NotBeNullOrWhiteSpace();

        // Verifies user persistence.
        ApplicationUser? persistedUser = await dbContext.Users
            .FirstOrDefaultAsync(user => user.Id == response.UserId);

        persistedUser.Should().NotBeNull();
        persistedUser!.Email.Should().Be("test@example.com");
        persistedUser.NormalizedEmail.Should().Be("TEST@EXAMPLE.COM");
        persistedUser.PasswordHash.Should().NotBeNullOrWhiteSpace();

        // Verifies tenant persistence.
        Tenant? persistedTenant = await dbContext.Tenants
            .FirstOrDefaultAsync(tenant => tenant.Id == response.TenantId);

        persistedTenant.Should().NotBeNull();
        persistedTenant!.Name.Should().Be("Test Space");
        persistedTenant.Slug.Should().Be("test-space");
        persistedTenant.OwnerUserId.Should().Be(response.UserId);

        // Verifies owner membership creation.
        TenantMembership? membership = await dbContext.TenantMemberships
            .FirstOrDefaultAsync(entity =>
                entity.UserId == response.UserId &&
                entity.TenantId == response.TenantId);

        membership.Should().NotBeNull();
        membership!.Role.Should().Be("Owner");
    }

    [Fact]
    public async Task RegisterLocalAsync_ShouldThrow_WhenEmailAlreadyExists()
    {
        // Creates an isolated relational test database for this test case.
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var service = new AuthService(dbContext, _lockoutServiceMock.Object);

        var firstRequest = new RegisterRequestDto
        {
            DisplayName = "First User",
            Email = "duplicate@example.com",
            Password = "Password!123",
            TenantName = "First Space"
        };

        var secondRequest = new RegisterRequestDto
        {
            DisplayName = "Second User",
            Email = "duplicate@example.com",
            Password = "Password!123",
            TenantName = "Second Space"
        };

        // Creates the first user successfully.
        await service.RegisterLocalAsync(firstRequest);

        // Verifies the second registration with the same email is rejected.
        Func<Task> action = async () => await service.RegisterLocalAsync(secondRequest);

        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Email already exists.");
    }

    [Fact]
    public async Task SignInLocalAsync_ShouldReturnTenantContext_WhenPasswordValid()
    {
        // Creates an isolated relational test database for this test case.
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var service = new AuthService(dbContext, _lockoutServiceMock.Object);

        var registrationRequest = new RegisterRequestDto
        {
            DisplayName = "Sign In User",
            Email = "signin@example.com",
            Password = "Password!123",
            TenantName = "Sign In Space"
        };

        // Registers the account so sign-in can be verified independently.
        await service.RegisterLocalAsync(registrationRequest);

        var signInRequest = new SignInRequestDto
        {
            Email = "signin@example.com",
            Password = "Password!123"
        };

        // Executes sign-in.
        AuthResponseDto response = await service.SignInLocalAsync(signInRequest);

        // Verifies the full tenant-aware session payload.
        response.IsAuthenticated.Should().BeTrue();
        response.DisplayName.Should().Be("Sign In User");
        response.AuthProvider.Should().Be("Local");
        response.TenantName.Should().Be("Sign In Space");
        response.TenantSlug.Should().Be("sign-in-space");
        response.TenantRole.Should().Be("Owner");
    }

    [Fact]
    public async Task SignInLocalAsync_ShouldThrow_WhenPasswordInvalid()
    {
        // Creates an isolated relational test database for this test case.
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var service = new AuthService(dbContext, _lockoutServiceMock.Object);

        var registrationRequest = new RegisterRequestDto
        {
            DisplayName = "Bad Password User",
            Email = "badpassword@example.com",
            Password = "Password!123",
            TenantName = "Bad Password Space"
        };

        // Registers the account first.
        await service.RegisterLocalAsync(registrationRequest);

        var signInRequest = new SignInRequestDto
        {
            Email = "badpassword@example.com",
            Password = "WrongPassword!123"
        };

        // Verifies invalid credentials are rejected.
        Func<Task> action = async () => await service.SignInLocalAsync(signInRequest);

        await action.Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid email or password.");
    }
}