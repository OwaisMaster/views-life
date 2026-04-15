using System.Security.Cryptography;
using System.Text;

namespace ViewsLife.Api.Common.Security;

/// <summary>
/// Produces safe debug hashes for cookie values without logging the raw value.
/// </summary>
public static class CookieDebugHasher
{
    /// <summary>
    /// Computes a SHA-256 hash for a string and returns a lowercase hex string.
    /// </summary>
    /// <param name="value">Input value to hash.</param>
    /// <returns>Lowercase SHA-256 hex string.</returns>
    public static string ComputeSha256(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "empty";
        }

        byte[] bytes = Encoding.UTF8.GetBytes(value);
        byte[] hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Extracts a single cookie value by cookie name from a Cookie header string.
    /// </summary>
    /// <param name="cookieHeader">Raw Cookie header.</param>
    /// <param name="cookieName">Cookie name to find.</param>
    /// <returns>The cookie value if found; otherwise null.</returns>
    public static string? ExtractCookieValue(string? cookieHeader, string cookieName)
    {
        if (string.IsNullOrWhiteSpace(cookieHeader))
        {
            return null;
        }

        string[] parts = cookieHeader.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        foreach (string part in parts)
        {
            int separatorIndex = part.IndexOf('=');

            if (separatorIndex <= 0)
            {
                continue;
            }

            string name = part[..separatorIndex];
            string value = part[(separatorIndex + 1)..];

            if (string.Equals(name, cookieName, StringComparison.Ordinal))
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts the cookie name/value pair from a Set-Cookie header.
    /// </summary>
    /// <param name="setCookieHeader">Raw Set-Cookie header.</param>
    /// <returns>Name and value if available; otherwise null.</returns>
    public static (string Name, string Value)? ExtractFromSetCookie(string? setCookieHeader)
    {
        if (string.IsNullOrWhiteSpace(setCookieHeader))
        {
            return null;
        }

        string firstSegment = setCookieHeader.Split(';', 2)[0];
        int separatorIndex = firstSegment.IndexOf('=');

        if (separatorIndex <= 0)
        {
            return null;
        }

        string name = firstSegment[..separatorIndex];
        string value = firstSegment[(separatorIndex + 1)..];

        return (name, value);
    }
}