namespace ViewsLife.Api.Domains.Auth.Dtos;

/// Authentication/session bootstrap response for the frontend.
public sealed class AuthResponseDto
{
    /// Application user identifier.
    public string UserId { get; set; } = string.Empty;

    /// Display name for the current user.
    public string DisplayName { get; set; } = string.Empty;

    /// Indicates whether the user is authenticated.
    public bool IsAuthenticated { get; set; }

    /// Auth provider name.
    public string AuthProvider { get; set; } = string.Empty;

    /// Current tenant identifier.
    public string TenantId { get; set; } = string.Empty;

    /// Current tenant display name.
    public string TenantName { get; set; } = string.Empty;

    /// Current tenant slug.
    public string TenantSlug { get; set; } = string.Empty;

    /// Current role inside the tenant.
    public string TenantRole { get; set; } = string.Empty;
}