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
using Microsoft.AspNetCore.Authentication;
using System.IO;
using Microsoft.AspNetCore.DataProtection;
using ViewsLife.Api.Common.Security;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// var dataProtectionKeysPath =
//     builder.Environment.IsDevelopment()
//         ? Path.Combine(builder.Environment.ContentRootPath, ".aspnet", "DataProtection-Keys")
//         : Environment.GetEnvironmentVariable("DATA_PROTECTION_KEYS_PATH")
//             ?? throw new InvalidOperationException(
//                 "DATA_PROTECTION_KEYS_PATH must be configured for non-development environments.");

// Directory.CreateDirectory(dataProtectionKeysPath);

// builder.Services
//     .AddDataProtection()
//     .SetApplicationName("ViewsLife")
//     .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

// // Adds cookie-based authentication for the application.
// // This enables ASP.NET Core to issue and validate an auth cookie automatically.
// builder.Services
//     .AddAuthentication(AuthConstants.AuthScheme)
//     .AddCookie(AuthConstants.AuthScheme, options =>
//     {
//         options.Cookie.Name = AuthConstants.AuthCookieName;
//         options.Cookie.HttpOnly = true;
//         options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
//         options.Cookie.SameSite = SameSiteMode.Lax;
//         options.SlidingExpiration = true;

//         // Prevents automatic redirect behavior for API endpoints and returns
//         // proper HTTP status codes instead.
//         options.Events = new CookieAuthenticationEvents
//         {
//             OnRedirectToLogin = context =>
//             {
//                 context.Response.StatusCode = StatusCodes.Status401Unauthorized;
//                 return Task.CompletedTask;
//             },
//             OnRedirectToAccessDenied = context =>
//             {
//                 context.Response.StatusCode = StatusCodes.Status403Forbidden;
//                 return Task.CompletedTask;
//             }
//         };
//     });
var dataProtectionKeysPath =
    builder.Environment.IsDevelopment()
        ? Path.Combine(builder.Environment.ContentRootPath, ".aspnet", "DataProtection-Keys")
        : Environment.GetEnvironmentVariable("DATA_PROTECTION_KEYS_PATH")
            ?? throw new InvalidOperationException(
                "DATA_PROTECTION_KEYS_PATH must be configured for non-development environments.");

Directory.CreateDirectory(dataProtectionKeysPath);

// Configure a stable Data Protection key ring.
// This is required so the cookie ticket can be protected/unprotected consistently.
builder.Services
    .AddDataProtection()
    .SetApplicationName("ViewsLife")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

// Register cookie authentication using the app's auth scheme.
builder.Services
    .AddAuthentication(AuthConstants.AuthScheme)
    .AddCookie(AuthConstants.AuthScheme, options =>
    {
        options.Cookie.Name = AuthConstants.AuthCookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Path = "/";
        options.SlidingExpiration = true;

        // Return status codes for API endpoints instead of redirecting HTML clients.
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

// Configure the cookie ticket protector via DI without calling BuildServiceProvider().
// TicketDataFormat is the type used to protect/unprotect the cookie ticket.
// builder.Services
//     .AddOptions<CookieAuthenticationOptions>(AuthConstants.AuthScheme)
//     .PostConfigure<IDataProtectionProvider>((options, dataProtectionProvider) =>
//     {
//         var protector = dataProtectionProvider.CreateProtector(
//             "Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware",
//             AuthConstants.AuthScheme,
//             "v2");

//         options.TicketDataFormat = new TicketDataFormat(protector);
//     });

builder.Services.AddAuthorization();




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
        policy.WithOrigins("https://views-life.vercel.app")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for cookie-based auth
    });
});

var app = builder.Build();


var logger = app.Services.GetRequiredService<ILoggerFactory>()
    .CreateLogger("ViewsLife.DataProtection");

var cookieOptionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
var activeCookieOptions = cookieOptionsMonitor.Get(AuthConstants.AuthScheme);

logger.LogInformation(
    "Cookie auth runtime options. Scheme={Scheme}, CookieName={CookieName}, CookiePath={CookiePath}, SameSite={SameSite}, SecurePolicy={SecurePolicy}, SlidingExpiration={SlidingExpiration}, TicketDataFormatType={TicketDataFormatType}",
    AuthConstants.AuthScheme,
    activeCookieOptions.Cookie.Name,
    activeCookieOptions.Cookie.Path,
    activeCookieOptions.Cookie.SameSite,
    activeCookieOptions.Cookie.SecurePolicy,
    activeCookieOptions.SlidingExpiration,
    activeCookieOptions.TicketDataFormat?.GetType().FullName ?? "[null]"
);

logger.LogInformation(
    "DataProtection path resolved to {Path}. FilesPresent={FilesPresent}",
    dataProtectionKeysPath,
    string.Join(", ", Directory.GetFiles(dataProtectionKeysPath).Select(Path.GetFileName))
);

// Enables Swagger only during development.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Applies the configured CORS policy before auth and endpoint mapping.
//app.UseCors("FrontendDev");
app.UseCors("StagingPolicy");

// Applies auth-specific rate limiting (skip in test environments).
if (!app.Environment.IsEnvironment("Development"))
{
    app.UseAuthRateLimiting();
}

// Temporary staging-only auth diagnostics.
// Place this after exception handling / forwarded headers, but before endpoint mapping.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/auth/me"))
    {
        ILogger authLogger = context.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("ViewsLife.AuthDiagnostics");

        var optionsMonitor = context.RequestServices
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();

        CookieAuthenticationOptions cookieOptions =
            optionsMonitor.Get(AuthConstants.AuthScheme);

        string rawCookieHeader = context.Request.Headers.Cookie.ToString();

        // Raw extraction from the Cookie header as currently sent over the wire.
        string? rawExtractedCookieValue =
            CookieDebugHasher.ExtractCookieValue(rawCookieHeader, AuthConstants.AuthCookieName);

        // Parsed cookie value from ASP.NET Core's cookie collection.
        string? parsedRequestCookieValue =
            context.Request.Cookies[AuthConstants.AuthCookieName];

        // Exact cookie value the cookie authentication handler uses internally.
        string? cookieManagerValue =
            cookieOptions.CookieManager.GetRequestCookie(context, AuthConstants.AuthCookieName);
        string? tlsTokenBinding = TlsTokenBindingHelper.GetTlsTokenBinding(context);

        AuthenticateResult authResult =
            await context.AuthenticateAsync(AuthConstants.AuthScheme);

        authLogger.LogInformation(
            "Auth diagnostics for {Path}. RawHeaderPresent={RawHeaderPresent}, RawHeaderLength={RawHeaderLength}, RawExtractedFound={RawExtractedFound}, RawExtractedLength={RawExtractedLength}, RawExtractedHash={RawExtractedHash}, ParsedRequestCookieFound={ParsedRequestCookieFound}, ParsedRequestCookieLength={ParsedRequestCookieLength}, ParsedRequestCookieHash={ParsedRequestCookieHash}, CookieManagerValueFound={CookieManagerValueFound}, CookieManagerValueLength={CookieManagerValueLength}, CookieManagerValueHash={CookieManagerValueHash}, TlsTokenBindingPresent={TlsTokenBindingPresent}, TlsTokenBindingLength={TlsTokenBindingLength}, TlsTokenBindingHash={TlsTokenBindingHash}, AuthSucceeded={AuthSucceeded}, AuthNone={AuthNone}, AuthFailureType={AuthFailureType}, AuthFailureMessage={AuthFailureMessage}, IdentityIsAuthenticated={IdentityIsAuthenticated}, MachineName={MachineName}, TicketDataFormatType={TicketDataFormatType}",
            context.Request.Path,
            !string.IsNullOrWhiteSpace(rawCookieHeader),
            rawCookieHeader.Length,
            !string.IsNullOrWhiteSpace(rawExtractedCookieValue),
            rawExtractedCookieValue?.Length ?? 0,
            CookieDebugHasher.ComputeSha256(rawExtractedCookieValue),
            !string.IsNullOrWhiteSpace(parsedRequestCookieValue),
            parsedRequestCookieValue?.Length ?? 0,
            CookieDebugHasher.ComputeSha256(parsedRequestCookieValue),
            !string.IsNullOrWhiteSpace(cookieManagerValue),
            cookieManagerValue?.Length ?? 0,
            CookieDebugHasher.ComputeSha256(cookieManagerValue),
            !string.IsNullOrWhiteSpace(tlsTokenBinding),
            tlsTokenBinding?.Length ?? 0,
            CookieDebugHasher.ComputeSha256(tlsTokenBinding),
            authResult.Succeeded,
            authResult.None,
            authResult.Failure?.GetType().FullName,
            authResult.Failure?.Message,
            context.User.Identity?.IsAuthenticated ?? false,
            Environment.MachineName,
            cookieOptions.TicketDataFormat?.GetType().FullName ?? "[null]"
        );

        if (!string.IsNullOrWhiteSpace(cookieManagerValue))
        {
            try
            {
                AuthenticationTicket? ticket =
                    cookieOptions.TicketDataFormat.Unprotect(
                        cookieManagerValue,
                        tlsTokenBinding);

                authLogger.LogInformation(
                    "Manual unprotect using CookieManager value. TicketWasNull={TicketWasNull}, PrincipalAuthenticated={PrincipalAuthenticated}, PrincipalClaimCount={PrincipalClaimCount}, UserIdClaimPresent={UserIdClaimPresent}, TenantIdClaimPresent={TenantIdClaimPresent}",
                    ticket is null,
                    ticket?.Principal?.Identity?.IsAuthenticated ?? false,
                    ticket?.Principal?.Claims?.Count() ?? 0,
                    ticket?.Principal?.HasClaim(c => c.Type == AuthConstants.UserIdClaimType) ?? false,
                    ticket?.Principal?.HasClaim(c => c.Type == AuthConstants.TenantIdClaimType) ?? false
                );
            }
            catch (Exception ex)
            {
                authLogger.LogError(
                    ex,
                    "Manual unprotect using CookieManager value threw. ExceptionType={ExceptionType}, InnerExceptionType={InnerExceptionType}, InnerExceptionMessage={InnerExceptionMessage}",
                    ex.GetType().FullName,
                    ex.InnerException?.GetType().FullName,
                    ex.InnerException?.Message
                );
            }
        }
    }

    await next();
});

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