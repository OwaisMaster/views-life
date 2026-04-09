namespace ViewsLife.Api.Domains.Auth.Dtos;

/// <summary>
/// Standardized error response for authentication failures and validation errors.
/// Does not leak internal details or framework information.
/// </summary>
public sealed class ErrorResponseDto
{
    /// <summary>
    /// Generic error message shown to the user.
    /// Should not reveal whether account exists, password rules, etc.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional error code for client-side handling.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Validation errors by field (only for 400 Bad Request).
    /// </summary>
    public Dictionary<string, string[]>? Errors { get; set; }
}
