using FluentAssertions;
using Moq;
using ViewsLife.Api.Domains.Auth.Dtos;
using ViewsLife.Api.Domains.Auth.Entities;
using ViewsLife.Api.Domains.Auth.Interfaces;
using ViewsLife.Api.Domains.Auth.Services;
using Xunit;

namespace ViewsLife.Api.UnitTests.Auth;

/// Unit tests for AuthService business logic.
/// These tests verify service behavior in isolation by mocking the repository.
public sealed class AuthServiceTests
{
    [Fact]
    public async Task SignInForDevelopmentAsync_ShouldCreateUser_WhenUserDoesNotExist()
    {
        // Arranges a mock repository that returns no existing user.
        var repositoryMock = new Mock<IUserRepository>();

        repositoryMock
            .Setup(repo => repo.GetByProviderAsync("Local", "local-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        var service = new AuthService(repositoryMock.Object);

        var request = new DevSignInRequestDto
        {
            DisplayName = "Test User",
            Email = "test@example.com",
            AuthProvider = "Local",
            ProviderSubjectId = "local-001"
        };

        // Executes the development sign-in operation.
        var response = await service.SignInForDevelopmentAsync(request);

        // Verifies a new user was created and persisted.
        response.DisplayName.Should().Be("Test User");
        response.UserId.Should().NotBeNullOrWhiteSpace();

        repositoryMock.Verify(
            repo => repo.AddAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()),
            Times.Once);

        repositoryMock.Verify(
            repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SignInForDevelopmentAsync_ShouldReuseExistingUser_WhenUserExists()
    {
        // Arranges a mock repository with an existing user.
        var existingUser = new ApplicationUser
        {
            Id = "existing-user-id",
            DisplayName = "Existing User",
            Email = "existing@example.com",
            AuthProvider = "Local",
            ProviderSubjectId = "local-001",
            IsActive = true
        };

        var repositoryMock = new Mock<IUserRepository>();

        repositoryMock
            .Setup(repo => repo.GetByProviderAsync("Local", "local-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var service = new AuthService(repositoryMock.Object);

        var request = new DevSignInRequestDto
        {
            DisplayName = "Ignored Name",
            Email = "ignored@example.com",
            AuthProvider = "Local",
            ProviderSubjectId = "local-001"
        };

        // Executes the development sign-in operation.
        var response = await service.SignInForDevelopmentAsync(request);

        // Verifies the existing user was reused and not recreated.
        response.UserId.Should().Be("existing-user-id");
        response.DisplayName.Should().Be("Existing User");

        repositoryMock.Verify(
            repo => repo.AddAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()),
            Times.Never);

        repositoryMock.Verify(
            repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ShouldReturnUnauthenticated_WhenUserIdIsMissing()
    {
        // Arranges a service with a mocked repository.
        var repositoryMock = new Mock<IUserRepository>();
        var service = new AuthService(repositoryMock.Object);

        // Executes the current-user lookup with no user id.
        var response = await service.GetCurrentUserAsync(null);

        // Verifies unauthenticated state is returned.
        response.IsAuthenticated.Should().BeFalse();
        response.UserId.Should().BeEmpty();
        response.DisplayName.Should().BeEmpty();
    }
}