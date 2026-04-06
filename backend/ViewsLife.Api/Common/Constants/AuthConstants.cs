namespace ViewsLife.Api.Common.Constants;

/// Stores authentication-related constants used across the API.
public static class AuthConstants
{
    /// The ASP.NET Core authentication scheme name used for the application's
    /// cookie-based authentication flow.
    public const string AuthScheme = "ViewsLifeCookieAuth";

    /// The cookie name used by the application's authentication middleware.
    public const string AuthCookieName = "viewslife_auth";

    /// Custom claim type used to store the application user identifier.
    /// This is distinct from provider-specific identifiers.
    public const string UserIdClaimType = "viewslife_user_id";
}