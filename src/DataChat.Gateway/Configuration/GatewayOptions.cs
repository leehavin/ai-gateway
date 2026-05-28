namespace DataChat.Gateway.Configuration;

public sealed class GatewayOptions
{
    public const string SectionName = "Gateway";

    public bool UseMock { get; set; }
    public string DomainsFile { get; set; } = "domains.json";
    public string[] ValidTokens { get; set; } = [];
    public string[] AllowedOrigins { get; set; } = ["*"];
    public int TimeoutSeconds { get; set; } = 120;
    public int MaxMessageLength { get; set; } = 32000;
    public int MaxUploadSizeMb { get; set; } = 10;
    public int MaxUploadTextPreviewChars { get; set; } = 12000;
    public string UploadDirectory { get; set; } = "uploads";
    public string PathBase { get; set; } = "";
    public bool EnableSwagger { get; set; } = true;
}
