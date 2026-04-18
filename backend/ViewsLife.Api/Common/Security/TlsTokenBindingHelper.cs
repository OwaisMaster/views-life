using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace ViewsLife.Api.Common.Security;

/// <summary>
/// Helper methods for reading TLS token binding data from the current request.
///
/// Context:
/// - ASP.NET Core cookie auth can use the TLS token binding value as an
///   additional purpose parameter when protecting and unprotecting tickets.
/// - This helper centralizes that logic so both controllers and middleware
///   use the exact same implementation.
/// </summary>
public static class TlsTokenBindingHelper
{
    /// <summary>
    /// Gets the TLS token binding identifier for the current request as a Base64 string.
    ///
    /// Returns null when token binding is not present.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>Base64 token binding id, or null if unavailable.</returns>
    public static string? GetTlsTokenBinding(HttpContext httpContext)
    {
        byte[]? binding = httpContext.Features
            .Get<ITlsTokenBindingFeature>()
            ?.GetProvidedTokenBindingId();

        return binding == null ? null : Convert.ToBase64String(binding);
    }
}