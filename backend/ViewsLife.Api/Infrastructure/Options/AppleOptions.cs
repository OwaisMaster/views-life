namespace ViewsLife.Api.Infrastructure.Options;

public sealed class AppleOptions
{
    public const string SectionName = "Apple";

    public string ClientId { get; set; } = string.Empty;

    public string TeamId { get; set; } = string.Empty;

    public string KeyId { get; set; } = string.Empty;

    public string PrivateKey { get; set; } = string.Empty;
}