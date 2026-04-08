using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;
using ViewsLife.Api.Infrastructure.Persistence;

namespace ViewsLife.Api.IntegrationTests;

/// <summary>
/// Custom integration-test host that swaps PostgreSQL for SQLite in-memory
/// and stabilizes Data Protection for auth-cookie round trips.
///
/// Context:
/// - Cookie authentication uses ASP.NET Core Data Protection.
/// - In CI, auth cookies can become unreadable between requests if the test host
///   does not share a stable key ring.
/// - Persisting keys to a temp directory for the test host lifetime makes auth
///   behavior deterministic in GitHub Actions.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private DbConnection? _connection;
    private string? _dataProtectionKeysDirectory;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Removes the application's existing DbContext registration.
            var dbContextDescriptor = services.SingleOrDefault(
                descriptor => descriptor.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (dbContextDescriptor is not null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Creates and opens a shared SQLite in-memory connection for the test host lifetime.
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // Registers ApplicationDbContext against SQLite in-memory for integration tests.
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // Creates a stable Data Protection key directory for this factory instance.
            _dataProtectionKeysDirectory = Path.Combine(
                Path.GetTempPath(),
                "ViewsLife.IntegrationTests.DataProtectionKeys",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(_dataProtectionKeysDirectory);

            // Stabilizes cookie encryption/decryption across requests in the same test host.
            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(_dataProtectionKeysDirectory))
                .SetApplicationName("ViewsLife.IntegrationTests");

            // Builds the service provider and ensures the schema exists.
            var serviceProvider = services.BuildServiceProvider();

            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection?.Dispose();

            if (!string.IsNullOrWhiteSpace(_dataProtectionKeysDirectory) &&
                Directory.Exists(_dataProtectionKeysDirectory))
            {
                try
                {
                    Directory.Delete(_dataProtectionKeysDirectory, recursive: true);
                }
                catch
                {
                    // Swallows cleanup failures because they should not fail the test run.
                }
            }
        }
    }
}