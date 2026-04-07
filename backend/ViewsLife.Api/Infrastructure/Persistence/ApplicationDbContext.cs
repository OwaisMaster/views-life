using Microsoft.EntityFrameworkCore;
using ViewsLife.Api.Domains.Auth.Entities;

namespace ViewsLife.Api.Infrastructure.Persistence;

/// Primary EF Core database context for the ViewsLife API.
///
/// Context:
/// - Contains the user + tenant bootstrap model in this slice.
/// - Notes and other domains will be added after tenant identity is complete.
public sealed class ApplicationDbContext : DbContext
{
    /// Creates a new database context instance using injected EF Core options.
    /// <param name="options">Configured EF Core options.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// Application users.
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();

    /// Tenants.
    public DbSet<Tenant> Tenants => Set<Tenant>();

    /// Tenant memberships.
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();

    /// Configures entity mappings, constraints, and indexes.
    /// <param name="modelBuilder">EF Core model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasKey(user => user.Id);

            entity.Property(user => user.DisplayName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(user => user.Email)
                .HasMaxLength(320);

            entity.Property(user => user.NormalizedEmail)
                .HasMaxLength(320);

            entity.Property(user => user.AuthProvider)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(user => user.ProviderSubjectId)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasIndex(user => user.NormalizedEmail)
                .IsUnique();

            entity.HasIndex(user => new
            {
                user.AuthProvider,
                user.ProviderSubjectId
            }).IsUnique();
        });

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(tenant => tenant.Id);

            entity.Property(tenant => tenant.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(tenant => tenant.Slug)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(tenant => tenant.OwnerUserId)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(tenant => tenant.Slug)
                .IsUnique();

            entity.HasOne(tenant => tenant.OwnerUser)
                .WithMany()
                .HasForeignKey(tenant => tenant.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TenantMembership>(entity =>
        {
            entity.HasKey(membership => membership.Id);

            entity.Property(membership => membership.TenantId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(membership => membership.UserId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(membership => membership.Role)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(membership => new
            {
                membership.TenantId,
                membership.UserId
            }).IsUnique();

            entity.HasOne(membership => membership.Tenant)
                .WithMany(tenant => tenant.Memberships)
                .HasForeignKey(membership => membership.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(membership => membership.User)
                .WithMany(user => user.TenantMemberships)
                .HasForeignKey(membership => membership.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}