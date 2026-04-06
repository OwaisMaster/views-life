using System.Reflection.Metadata;
using Microsoft.EntityFrameworkCore;
using ViewsLife.Api.Domains.Auth.Entities;

namespace ViewsLife.Api.Infrastructure.Persistence;

/// Primary EF Core database context for the ViewsLife API.
///
/// Context:
/// - Registered through dependency injection in ASP.NET Core.
/// - Designed to represent a single unit of work per request, which aligns with
///   EF Core guidance for web applications.
/// - This initial version contains only the Users table so auth can be persisted
///   before Notes and other domains are added.
public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        
    }

    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();

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

            entity.Property(user => user.AuthProvider)
            .IsRequired()
            .HasMaxLength(50);

            entity.Property(user => user.ProviderSubjectId)
            .IsRequired()
            .HasMaxLength(200);

            // Adds indexes that will matter immediately for auth lookups.
            entity.HasIndex(user => user.Email);

            entity.HasIndex(user => new
            {
                user.AuthProvider,
                user.ProviderSubjectId
            }).IsUnique();
        });
    }
}