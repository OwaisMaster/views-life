using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ViewsLife.Api.Domains.Auth.Dtos;
using ViewsLife.Api.Domains.Auth.Entities;
using ViewsLife.Api.Domains.Auth.Interfaces;
using ViewsLife.Api.Infrastructure.Persistence;

namespace ViewsLife.Api.Domains.Auth.Services;

/// Auth application service for local registration/sign-in and future provider flows.
/// Context:
/// - This slice formalizes the user + tenant bootstrap model.
/// - New registrations create:
///   1. user
///   2. tenant
///   3. owner membership
/// - Sign-in returns the persisted user + tenant context used to issue auth claims.
/// - Account lockout is applied after repeated failed sign-in attempts.
public sealed class AuthService : IAuthService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILockoutService _lockoutService;
    private readonly PasswordHasher<ApplicationUser> _passwordHasher = new();

    /// Creates a new auth service instance.
    public AuthService(ApplicationDbContext dbContext, ILockoutService lockoutService)
    {
        _dbContext = dbContext;
        _lockoutService = lockoutService;
    }

    public async Task<AuthResponseDto> RegisterLocalAsync(
        RegisterRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // Email is already normalized (lowercase, trimmed) by NormalizedEmail validator
        string normalizedEmail = NormalizeEmail(request.Email);

        bool emailAlreadyExists = await _dbContext.Users
            .AnyAsync(
                user => user.NormalizedEmail == normalizedEmail,
                cancellationToken);

        if (emailAlreadyExists)
        {
            throw new InvalidOperationException("Email already exists.");
        }

        string tenantSlug = await GenerateUniqueTenantSlugAsync(
            request.TenantName,
            cancellationToken);

        var user = new ApplicationUser
        {
            DisplayName = request.DisplayName,
            Email = request.Email,
            NormalizedEmail = normalizedEmail,
            AuthProvider = "Local",
            ProviderSubjectId = normalizedEmail,
            IsEmailVerified = true,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        var tenant = new Tenant
        {
            Name = request.TenantName,
            Slug = tenantSlug,
            OwnerUserId = user.Id,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var membership = new TenantMembership
        {
            TenantId = tenant.Id,
            UserId = user.Id,
            Role = "Owner",
            CreatedAtUtc = DateTime.UtcNow
        };

        // Wraps bootstrap creation in a single transaction so the model cannot
        // end up with a user but no tenant, or a tenant but no owner membership.
        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        _dbContext.Users.Add(user);
        _dbContext.Tenants.Add(tenant);
        _dbContext.TenantMemberships.Add(membership);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new AuthResponseDto
        {
            UserId = user.Id,
            DisplayName = user.DisplayName,
            IsAuthenticated = true,
            AuthProvider = user.AuthProvider,
            TenantId = tenant.Id,
            TenantName = tenant.Name,
            TenantSlug = tenant.Slug,
            TenantRole = membership.Role
        };
    }

    public async Task<AuthResponseDto> SignInLocalAsync(
        SignInRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // Email is already normalized (lowercase, trimmed) by NormalizedEmail validator
        string normalizedEmail = NormalizeEmail(request.Email);

        // Check if the account is locked
        if (await _lockoutService.IsLockedOutAsync(normalizedEmail, cancellationToken))
        {
            throw new UnauthorizedAccessException("Account is temporarily locked due to repeated failed sign-in attempts.");
        }

        ApplicationUser? user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                entity => entity.NormalizedEmail == normalizedEmail &&
                          entity.AuthProvider == "Local",
                cancellationToken);

        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash) || !user.IsActive)
        {
            // Record failed attempt
            await _lockoutService.RecordFailedAttemptAsync(normalizedEmail, cancellationToken);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        PasswordVerificationResult passwordResult =
            _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            // Record failed attempt
            await _lockoutService.RecordFailedAttemptAsync(normalizedEmail, cancellationToken);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        // Reset failed attempts on successful sign-in
        await _lockoutService.ResetFailedAttemptsAsync(normalizedEmail, cancellationToken);

        var tenantContext = await _dbContext.TenantMemberships
            .AsNoTracking()
            .Include(membership => membership.Tenant)
            .FirstOrDefaultAsync(
                membership => membership.UserId == user.Id &&
                              membership.Tenant != null &&
                              membership.Tenant.IsActive,
                cancellationToken);

        if (tenantContext is null || tenantContext.Tenant is null)
        {
            throw new InvalidOperationException("The user does not have an active tenant.");
        }

        return new AuthResponseDto
        {
            UserId = user.Id,
            DisplayName = user.DisplayName,
            IsAuthenticated = true,
            AuthProvider = user.AuthProvider,
            TenantId = tenantContext.Tenant.Id,
            TenantName = tenantContext.Tenant.Name,
            TenantSlug = tenantContext.Tenant.Slug,
            TenantRole = tenantContext.Role
        };
    }

    public async Task<CurrentUserResponseDto> GetCurrentUserAsync(
        string? userId,
        string? tenantId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(tenantId))
        {
            return new CurrentUserResponseDto
            {
                UserId = string.Empty,
                DisplayName = string.Empty,
                IsAuthenticated = false,
                TenantId = string.Empty,
                TenantName = string.Empty,
                TenantSlug = string.Empty,
                TenantRole = string.Empty
            };
        }

        var membership = await _dbContext.TenantMemberships
            .AsNoTracking()
            .Include(entity => entity.User)
            .Include(entity => entity.Tenant)
            .FirstOrDefaultAsync(
                entity => entity.UserId == userId &&
                          entity.TenantId == tenantId,
                cancellationToken);

        if (membership?.User is null ||
            membership.Tenant is null ||
            !membership.User.IsActive ||
            !membership.Tenant.IsActive)
        {
            return new CurrentUserResponseDto
            {
                UserId = string.Empty,
                DisplayName = string.Empty,
                IsAuthenticated = false,
                TenantId = string.Empty,
                TenantName = string.Empty,
                TenantSlug = string.Empty,
                TenantRole = string.Empty
            };
        }

        return new CurrentUserResponseDto
        {
            UserId = membership.User.Id,
            DisplayName = membership.User.DisplayName,
            IsAuthenticated = true,
            TenantId = membership.Tenant.Id,
            TenantName = membership.Tenant.Name,
            TenantSlug = membership.Tenant.Slug,
            TenantRole = membership.Role
        };
    }

    public Task<AuthResponseDto> SignInWithAppleAsync(
        AppleSignInRequestDto request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Apple sign-in validation has not been implemented yet.");
    }

    /// Normalizes an email for case-insensitive lookup.
    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }

    /// Generates a unique tenant slug from a requested tenant name.
    private async Task<string> GenerateUniqueTenantSlugAsync(
        string tenantName,
        CancellationToken cancellationToken)
    {
        string baseSlug = Slugify(tenantName);
        string slug = baseSlug;
        int suffix = 1;

        while (await _dbContext.Tenants.AnyAsync(
                   tenant => tenant.Slug == slug,
                   cancellationToken))
        {
            suffix++;
            slug = $"{baseSlug}-{suffix}";
        }

        return slug;
    }

    /// Converts free-form tenant names into URL-safe slugs.
    /// <returns>Slugified value.</returns>
    private static string Slugify(string value)
    {
        string trimmed = value.Trim().ToLowerInvariant();

        var chars = trimmed
            .Select(character =>
                char.IsLetterOrDigit(character) ? character : '-')
            .ToArray();

        string collapsed = new string(chars);

        while (collapsed.Contains("--", StringComparison.Ordinal))
        {
            collapsed = collapsed.Replace("--", "-", StringComparison.Ordinal);
        }

        return collapsed.Trim('-');
    }
}