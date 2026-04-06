namespace ViewsLife.Api.Common.Constants;

/// Stores authentication-related constants used by controllers and services.
public static class AuthConstants
{
    /// Name of the temporary cookie used to hold the current user identifier
    /// during development before full JWT/cookie auth is implemented.
    public const string DevUserIdCookieName = "viewslife_dev_user_id";
}