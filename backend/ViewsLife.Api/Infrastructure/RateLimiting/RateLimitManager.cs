namespace ViewsLife.Api.Infrastructure.RateLimiting;

/// <summary>
/// In-memory rate limiting bucket for tracking requests per key.
/// </summary>
public sealed class RateLimitBucket
{
    public required string Key { get; set; }
    public int RequestCount { get; set; }
    public DateTime ResetAtUtc { get; set; }

    /// <summary>
    /// Checks if the bucket has exceeded the limit.
    /// </summary>
    public bool IsExceeded(int limit) => RequestCount >= limit;

    /// <summary>
    /// Checks if the bucket should be reset due to window expiration.
    /// </summary>
    public bool ShouldReset => DateTime.UtcNow >= ResetAtUtc;
}

/// <summary>
/// Simple in-memory rate limiter with IP-based and email-based policies.
/// 
/// Strategy:
/// - register: 3 requests per hour per IP
/// - sign-in: 10 requests per minute per IP
/// - me, sign-out: 60 requests per minute per IP (conservative)
/// 
/// Note: For production at scale, use distributed rate limiting (Redis, etc.)
/// </summary>
public sealed class RateLimitManager
{
    private readonly object _lockObject = new();
    private readonly Dictionary<string, RateLimitBucket> _buckets = [];

    public const int RegisterLimitPerHour = 3;
    public const int SignInLimitPerMinute = 10;
    public const int MeSignOutLimitPerMinute = 60;

    /// <summary>
    /// Checks and records a request for the given key and policy.
    /// Returns true if the request should be allowed, false if rate limited.
    /// </summary>
    public bool TryConsumeToken(string key, int limit, TimeSpan window)
    {
        lock (_lockObject)
        {
            CleanupExpiredBuckets();

            if (_buckets.TryGetValue(key, out var bucket))
            {
                // Reset if window expired
                if (bucket.ShouldReset)
                {
                    bucket.RequestCount = 0;
                    bucket.ResetAtUtc = DateTime.UtcNow.Add(window);
                }

                // Check limit
                if (bucket.IsExceeded(limit))
                {
                    return false;
                }

                bucket.RequestCount++;
                return true;
            }

            // Create new bucket
            var newBucket = new RateLimitBucket
            {
                Key = key,
                RequestCount = 1,
                ResetAtUtc = DateTime.UtcNow.Add(window)
            };

            _buckets.Add(key, newBucket);
            return true;
        }
    }

    /// <summary>
    /// Removes expired buckets to prevent unbounded memory growth.
    /// </summary>
    private void CleanupExpiredBuckets()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _buckets
            .Where(kvp => now > kvp.Value.ResetAtUtc.AddHours(1))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _buckets.Remove(key);
        }
    }
}
