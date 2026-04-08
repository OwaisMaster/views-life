using System.Data.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ViewsLife.Api.Common.Constants;
using ViewsLife.Api.Infrastructure.Persistence;

namespace ViewsLife.Api.IntegrationTests;

/// Custom integration-test host that:
/// - swaps PostgreSQL for SQLite in-memory
/// - runs under a dedicated Testing environment
/// - relaxes cookie security policy for the test host only so automatic cookie
///   round-tripping works reliably with WebApplicationFactory
///
/// Context:
/// - This keeps production cookie settings strict.
/// - It avoids test-only manual Cookie header injection for authenticated flows.
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private DbConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

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

            // Overrides cookie authentication options for the Testing environment only.
            // This allows the test client to round-trip the auth cookie automatically.
            services.PostConfigure<CookieAuthenticationOptions>(
                AuthConstants.AuthScheme,
                options =>
                {
                    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.HttpOnly = true;
                });

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