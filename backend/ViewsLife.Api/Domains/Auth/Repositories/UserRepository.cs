using Microsoft.EntityFrameworkCore;
using ViewsLife.Api.Domains.Auth.Entities;
using ViewsLife.Api.Domains.Auth.Interfaces;
using ViewsLife.Api.Infrastructure.Persistence;

namespace ViewsLife.Api.Domains.Auth.Repositories;

/// Entity Framework Core implementation of the user repository.
/// This is the single place where the Auth domain performs user persistence
/// queries against ApplicationDbContext.
public sealed class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _dbContext;

    /// Creates a new repository instance with the EF Core database context.
    public UserRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApplicationUser?> GetByProviderAsync(
        string authProvider,
        string providerSubjectId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(
            user => user.AuthProvider == authProvider &&
            user.ProviderSubjectId == providerSubjectId,
            cancellationToken
        );
    }

    public async Task<ApplicationUser?> GetByIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }

    public async Task AddAsync(
        ApplicationUser user,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Users.AddAsync(user, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}