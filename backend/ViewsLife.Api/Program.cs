using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ViewsLife.Api.Common.Constants;
using ViewsLife.Api.Domains.Auth.Interfaces;
using ViewsLife.Api.Domains.Auth.Repositories;
using ViewsLife.Api.Domains.Auth.Services;
using ViewsLife.Api.Domains.Notes.Interfaces;
using ViewsLife.Api.Domains.Notes.Services;
using ViewsLife.Api.Infrastructure.Logging;
using ViewsLife.Api.Infrastructure.Options;
using ViewsLife.Api.Infrastructure.Persistence;
using ViewsLife.Api.Infrastructure.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Binds strongly typed configuration objects so the application can access
// external settings through validated option classes instead of raw strings.
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.Configure<AppleOptions>(
    builder.Configuration.GetSection(AppleOptions.SectionName));

builder.Services.Configure<OpenAiOptions>(
    builder.Configuration.GetSection(OpenAiOptions.SectionName));

// Registers the EF Core database context for one unit of work per request.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Registers Auth persistence and domain services.
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ILockoutService, LockoutService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuditLogger, AuditLogger>();

// Registers note services.
builder.Services.AddScoped<INoteService, NoteService>();

// Registers rate limiting services.
builder.Services.AddSingleton<RateLimitManager>();

// Adds cookie-based authentication for the application.
// This enables ASP.NET Core to issue and validate an auth cookie automatically.
builder.Services
    .AddAuthentication(AuthConstants.AuthScheme)
    .AddCookie(AuthConstants.AuthScheme, options =>
    {
        options.Cookie.Name = AuthConstants.AuthCookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;

        // Prevents automatic redirect behavior for API endpoints and returns
        // proper HTTP status codes instead.
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
        };
    });

// Adds authorization services so [Authorize] can be used on endpoints.
builder.Services.AddAuthorization();

// Adds support for controller-based APIs.
builder.Services.AddControllers();

// Adds endpoints metadata support for Swagger/OpenAPI.
builder.Services.AddEndpointsApiExplorer();

// Adds Swagger generation for local API exploration and testing.
builder.Services.AddSwaggerGen();

// Adds a named CORS policy for the local Next.js frontend.
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy
            .WithOrigins("https://localhost:3000", "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("StagingPolicy", policy =>
    {
        policy.WithOrigins("https://views-life.vercel.app/")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for cookie-based auth
    });
});

var app = builder.Build();

// Enables Swagger only during development.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Applies the configured CORS policy before auth and endpoint mapping.
app.UseCors("FrontendDev");

// Applies auth-specific rate limiting (skip in test environments).
if (!app.Environment.IsEnvironment("Development"))
{
    app.UseAuthRateLimiting();
}

// Redirects HTTP requests to HTTPS when possible.
app.UseHttpsRedirection();

// Enables authentication and authorization middleware in the request pipeline.
app.UseAuthentication();
app.UseAuthorization();

// Maps attribute-routed controllers.
app.MapControllers();

app.Run();

public partial class Program
{
}