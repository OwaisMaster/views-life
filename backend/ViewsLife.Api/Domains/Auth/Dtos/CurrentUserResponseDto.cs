namespace ViewsLife.Api.Domains.Auth.Dtos;

/// Represents the current authenticated user and active tenant context.
public sealed class CurrentUserResponseDto
{
    /// Current user identifier.
    public string UserId { get; set; } = string.Empty;

    /// Current user display name.
    public string DisplayName { get; set; } = string.Empty;

    /// Indicates whether the user is authenticated.
    public bool IsAuthenticated { get; set; }

    /// Current tenant identifier.
    public string TenantId { get; set; } = string.Empty;

    /// Current tenant name.
    public string TenantName { get; set; } = string.Empty;

    /// Current tenant slug.
    public string TenantSlug { get; set; } = string.Empty;

    /// Current role inside the tenant.
    public string TenantRole { get; set; } = string.Empty;
}