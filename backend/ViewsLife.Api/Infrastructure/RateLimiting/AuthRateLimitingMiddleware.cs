using ViewsLife.Api.Domains.Auth.Dtos;

namespace ViewsLife.Api.Infrastructure.RateLimiting;

/// <summary>
/// Middleware for rate limiting auth endpoints.
/// Enforces IP-based rate limiting to prevent abuse.
/// </summary>
public sealed class AuthRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RateLimitManager _rateLimitManager;

    public AuthRateLimitingMiddleware(RequestDelegate next, RateLimitManager rateLimitManager)
    {
        _next = next;
        _rateLimitManager = rateLimitManager;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply rate limiting to auth endpoints
        if (!IsAuthEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        string clientIp = GetClientIp(context);
        string endpoint = context.Request.Path.Value ?? string.Empty;

        // Determine rate limit policy based on endpoint
        if (!TryGetRateLimitPolicy(endpoint, out var limit, out var window))
        {
            await _next(context);
            return;
        }

        string key = $"{clientIp}:{endpoint}";

        // Check rate limit
        if (!_rateLimitManager.TryConsumeToken(key, limit, window))
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsJsonAsync(new ErrorResponseDto
            {
                Message = "Too many requests. Please try again later.",
                ErrorCode = "RATE_LIMITED"
            });
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// Checks if the request is to an auth endpoint.
    /// </summary>
    private static bool IsAuthEndpoint(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;
        return pathValue.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the client IP address from the request.
    /// </summary>
    private static string GetClientIp(HttpContext context)
    {
        // Check X-Forwarded-For header (for proxies)
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
        {
            var ips = forwarded.ToString().Split(',');
            if (ips.Length > 0 && !string.IsNullOrWhiteSpace(ips[0]))
            {
                return ips[0].Trim();
            }
        }

        // Fall back to remote IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Determines the rate limit policy for the given endpoint.
    /// </summary>
    private static bool TryGetRateLimitPolicy(string endpoint, out int limit, out TimeSpan window)
    {
        endpoint = endpoint.ToLowerInvariant();

        if (endpoint.EndsWith("/register", StringComparison.OrdinalIgnoreCase))
        {
            limit = RateLimitManager.RegisterLimitPerHour;
            window = TimeSpan.FromHours(1);
            return true;
        }

        if (endpoint.EndsWith("/sign-in", StringComparison.OrdinalIgnoreCase))
        {
            limit = RateLimitManager.SignInLimitPerMinute;
            window = TimeSpan.FromMinutes(1);
            return true;
        }

        if (endpoint.EndsWith("/me", StringComparison.OrdinalIgnoreCase) ||
            endpoint.EndsWith("/sign-out", StringComparison.OrdinalIgnoreCase))
        {
            limit = RateLimitManager.MeSignOutLimitPerMinute;
            window = TimeSpan.FromMinutes(1);
            return true;
        }

        limit = 0;
        window = TimeSpan.Zero;
        return false;
    }
}

/// <summary>
/// Extension methods for adding rate limiting middleware.
/// </summary>
public static class AuthRateLimitingExtensions
{
    public static IApplicationBuilder UseAuthRateLimiting(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AuthRateLimitingMiddleware>();
    }
}
