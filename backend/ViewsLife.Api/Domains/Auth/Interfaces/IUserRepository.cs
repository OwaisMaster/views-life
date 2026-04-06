using ViewsLife.Api.Domains.Auth.Entities;

namespace ViewsLife.Api.Domains.Auth.Interfaces;

/// Defines persistence operations for application users.
/// This keeps the Auth service focused on business logic instead of
/// direct Entity Framework Core query details.
public interface IUserRepository
{
    /// Finds a user by the upstream authentication provider and its user identifier.
    /// <param name="authProvider">The provider name, such as Apple or Google.</param>
    /// <param name="providerSubjectId">The stable provider-specific subject identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async database access.</param>
    /// <returns>The matching user if found; otherwise null.</returns>
    Task<ApplicationUser?> GetByProviderAsync(
        string authProvider,
        string providerSubjectId,
        CancellationToken cancellationToken = default);

    /// Finds a user by the application user identifier.
    /// <param name="userId">The application user identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async database access.</param>
    /// <returns>The matching user if found; otherwise null.</returns>
    Task<ApplicationUser?> GetByIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// Adds a new user to the database.
    /// <param name="user">The user entity to add.</param>
    /// <param name="cancellationToken">Cancellation token for async database access.</param>
    Task AddAsync(
        ApplicationUser user,
        CancellationToken cancellationToken = default);

    /// Persists pending changes to the database.
    /// <param name="cancellationToken">Cancellation token for async database access.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}