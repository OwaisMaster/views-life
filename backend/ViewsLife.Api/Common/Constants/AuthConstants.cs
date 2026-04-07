namespace ViewsLife.Api.Common.Constants;

/// Stores authentication-related constants used across the API.
public static class AuthConstants
{
    /// Cookie authentication scheme name.
    public const string AuthScheme = "ViewsLifeCookieAuth";

    /// Auth cookie name.
    public const string AuthCookieName = "viewslife_auth";

    /// Claim type for application user identifier.
    public const string UserIdClaimType = "viewslife_user_id";

    /// Claim type for current tenant identifier.
    public const string TenantIdClaimType = "viewslife_tenant_id";

    /// Claim type for current tenant role.
    public const string TenantRoleClaimType = "viewslife_tenant_role";
}