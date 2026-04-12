namespace ViewsLife.Api.Infrastructure.Logging;

/// <summary>
/// Security audit event types.
/// These are the key events to track for security monitoring.
/// </summary>
public enum AuditEventType
{
    RegistrationAttempted = 0,
    RegistrationSucceeded = 1,
    RegistrationFailed = 2,
    SignInAttempted = 3,
    SignInSucceeded = 4,
    SignInFailed = 5,
    AccountLockedOut = 6,
    SignOutOccurred = 7,
    InvalidRequestProcessed = 8,
    RateLimitExceeded = 9
}

/// <summary>
/// Secure audit logging service for security-relevant events.
/// Does not log passwords, tokens, or sensitive personal data.
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Logs a security audit event.
    /// </summary>
    Task LogAuditEventAsync(
        AuditEventType eventType,
        string userId,
        string? email,
        string? ipAddress,
        string? details);
}

/// <summary>
/// Implementation of audit logging using ILogger.
/// Logs to the standard logging framework.
/// </summary>
public sealed class AuditLogger : IAuditLogger
{
    private readonly ILogger<AuditLogger> _logger;

    public AuditLogger(ILogger<AuditLogger> logger)
    {
        _logger = logger;
    }

    public Task LogAuditEventAsync(
        AuditEventType eventType,
        string userId,
        string? email,
        string? ipAddress,
        string? details)
    {
        // Mask email for logging (show only domain)
        string maskedEmail = MaskEmail(email);

        string safeUserId = SanitizeForLog(userId);
        string safeEmail = SanitizeForLog(maskedEmail);
        string safeIpAddress = SanitizeForLog(ipAddress);
        string safeDetails = SanitizeForLog(details);

        var logMessage = $"[AUDIT] {eventType:G} | User: {safeUserId} | Email: {safeEmail} | IP: {safeIpAddress} | Details: {safeDetails}";

        switch (eventType)
        {
            case AuditEventType.RegistrationSucceeded:
            case AuditEventType.SignInSucceeded:
                _logger.LogInformation(logMessage);
                break;

            case AuditEventType.RegistrationFailed:
            case AuditEventType.SignInFailed:
            case AuditEventType.AccountLockedOut:
            case AuditEventType.RateLimitExceeded:
                _logger.LogWarning(logMessage);
                break;

            case AuditEventType.InvalidRequestProcessed:
                _logger.LogWarning(logMessage);
                break;

            default:
                _logger.LogInformation(logMessage);
                break;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Sanitizes a value for plain-text log output to prevent log forging.
    /// </summary>
    private static string SanitizeForLog(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "[unknown]";
        }

        return value
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty);
    }

    /// <summary>
    /// Masks email to show only the domain for privacy.
    /// Example: user@example.com -> ***@example.com
    /// </summary>
    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "[unknown]";
        }

        var parts = email.Split('@');
        if (parts.Length != 2)
        {
            return "[invalid]";
        }

        return $"***@{parts[1]}";
    }
}
