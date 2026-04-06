namespace ViewsLife.Api.Domains.Auth.Dtos;

/// Represents lightweight information about the currently authenticated user.
/// This is useful for session bootstrap and frontend app initialization.
public sealed class CurrentUserResponseDto
{
    public string UserId {get; set;} = string.Empty;

    public string DisplayName {get; set; } = string.Empty;

    public bool IsAuthenticated { get; set; }
}