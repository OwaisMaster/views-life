using System.Data.Common;
using System.IO;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ViewsLife.Api.Infrastructure.Persistence;
using ViewsLife.Api.Infrastructure.RateLimiting;

namespace ViewsLife.Api.IntegrationTests;

/// <summary>
/// Custom integration-test host that swaps PostgreSQL for SQLite in-memory,
/// configures a deterministic Data Protection key ring, and supports a test
/// authentication scheme for protected endpoint testing.
///
/// Context:
/// - SQLite in-memory gives relational behavior without using the dev database.
/// - Data Protection is explicitly configured for deterministic host behavior.
/// - The test auth scheme allows protected endpoint tests to avoid CI-only
///   cookie round-trip flakiness.
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

            // Creates a deterministic file-system-backed Data Protection key store.
            _dataProtectionKeysDirectory = Path.Combine(
                Path.GetTempPath(),
                "ViewsLife.IntegrationTests.DataProtectionKeys");

            Directory.CreateDirectory(_dataProtectionKeysDirectory);

            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(_dataProtectionKeysDirectory))
                .SetApplicationName("ViewsLife.IntegrationTests");

            // Adds the test authentication scheme alongside the default cookie auth.
            // This allows tests to use either cookie authentication (default) or
            // test headers for deterministic authentication testing.
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });

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
        }
    }
}