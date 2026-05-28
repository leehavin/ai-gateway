using System.Text.Json;
using DataChat.Core.Configuration;

namespace DataChat.Infrastructure.Configuration;

public static class DomainsConfigurationLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static DomainsConfiguration Load(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<DomainsConfiguration>(json, JsonOptions)
            ?? throw new InvalidOperationException($"无法解析 domains 配置: {path}");
    }
}
