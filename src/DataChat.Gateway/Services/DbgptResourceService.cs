using System.Text.Json;
using DataChat.Core.Abstractions;
using DataChat.Core.Configuration;
using DataChat.Gateway.Models;
using DataChat.Providers.Dbgpt;
using Microsoft.Extensions.Http;

namespace DataChat.Gateway.Services;

public sealed class DbgptResourceService
{
    private readonly DbgptApiClient _api;
    private readonly ILogger<DbgptResourceService> _logger;

    public DbgptResourceService(
        IHttpClientFactory httpClientFactory,
        IApiKeyStore apiKeyStore,
        DomainsConfiguration domains,
        ILogger<DbgptResourceService> logger)
    {
        _api = new DbgptApiClient(httpClientFactory, apiKeyStore, domains.Defaults.DbgptBaseUrl);
        _logger = logger;
    }

    public DbgptApiClient Client => _api;

    public Task<bool> PingAsync(CancellationToken cancellationToken = default) =>
        _api.PingAsync(cancellationToken);

    public async Task<IReadOnlyList<DbgptAppItem>> GetAppsAsync(CancellationToken cancellationToken = default)
    {
        var json = await ReadBodyAsync(await _api.GetAsync("/api/v2/serve/apps", cancellationToken), cancellationToken);
        return ParseArray(json, ParseApp);
    }

    public async Task<DbgptAppItem?> GetAppAsync(string appId, CancellationToken cancellationToken = default)
    {
        var json = await ReadBodyAsync(
            await _api.GetAsync($"/api/v2/serve/apps/{Uri.EscapeDataString(appId)}", cancellationToken),
            cancellationToken);
        return ParseObject(json, ParseApp);
    }

    public async Task<IReadOnlyList<DbgptDatasourceItem>> GetDatasourcesAsync(CancellationToken cancellationToken = default)
    {
        var json = await ReadBodyAsync(await _api.GetAsync("/api/v2/serve/datasources", cancellationToken), cancellationToken);
        return ParseArray(json, ParseDatasource);
    }

    public async Task<DbgptDatasourceItem?> GetDatasourceAsync(string id, CancellationToken cancellationToken = default)
    {
        var json = await ReadBodyAsync(
            await _api.GetAsync($"/api/v2/serve/datasources/{Uri.EscapeDataString(id)}", cancellationToken),
            cancellationToken);
        return ParseObject(json, ParseDatasource);
    }

    public async Task<IReadOnlyList<DbgptKnowledgeSpaceItem>> GetKnowledgeSpacesAsync(CancellationToken cancellationToken = default)
    {
        var json = await ReadBodyAsync(
            await _api.GetAsync("/api/v2/serve/knowledge/spaces", cancellationToken),
            cancellationToken);
        return ParseArray(json, ParseKnowledgeSpace);
    }

    public async Task<HttpResponseMessage> ForwardAsync(
        HttpMethod method,
        string dbgptPath,
        object? body,
        CancellationToken cancellationToken = default)
    {
        return method.Method switch
        {
            "GET" => await _api.GetAsync(dbgptPath, cancellationToken),
            "POST" => await _api.PostAsync(dbgptPath, body, cancellationToken),
            "PUT" => await _api.PutAsync(dbgptPath, body, cancellationToken),
            "DELETE" => await _api.DeleteAsync(dbgptPath, cancellationToken),
            _ => throw new NotSupportedException($"不支持的方法: {method}")
        };
    }

    private async Task<string?> ReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("DB-GPT {Status} {Reason}", (int)response.StatusCode, response.ReasonPhrase);
                return null;
            }
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
    }

    private static IReadOnlyList<T> ParseArray<T>(string? json, Func<JsonElement, T?> parse) where T : class
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            using var doc = JsonDocument.Parse(json);
            var el = doc.RootElement;
            if (el.ValueKind == JsonValueKind.Array)
                return el.EnumerateArray().Select(parse).Where(x => x is not null).Cast<T>().ToList();
            if (el.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                return data.EnumerateArray().Select(parse).Where(x => x is not null).Cast<T>().ToList();
        }
        catch (JsonException) { }
        return [];
    }

    private static T? ParseObject<T>(string? json, Func<JsonElement, T?> parse) where T : class
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var el = doc.RootElement;
            if (el.TryGetProperty("data", out var data))
                return parse(data);
            return parse(el);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static DbgptAppItem? ParseApp(JsonElement el)
    {
        var code = GetString(el, "app_code", "appCode", "code", "id");
        if (string.IsNullOrEmpty(code)) return null;
        return new DbgptAppItem
        {
            AppCode = code,
            AppName = GetString(el, "app_name", "appName", "name") ?? code,
            AppDescribe = GetString(el, "app_describe", "appDescribe", "description")
        };
    }

    private static DbgptDatasourceItem? ParseDatasource(JsonElement el)
    {
        var id = GetString(el, "id", "datasource_id", "datasourceId", "code", "db_name", "dbName");
        if (string.IsNullOrEmpty(id)) return null;
        return new DbgptDatasourceItem
        {
            Id = id,
            Name = GetString(el, "name", "db_name", "dbName") ?? id,
            Type = GetString(el, "type", "db_type", "dbType")
        };
    }

    private static DbgptKnowledgeSpaceItem? ParseKnowledgeSpace(JsonElement el)
    {
        var id = GetString(el, "id", "space_id", "spaceId");
        var name = GetString(el, "name", "space_name", "spaceName");
        if (string.IsNullOrEmpty(id) && string.IsNullOrEmpty(name)) return null;
        return new DbgptKnowledgeSpaceItem
        {
            Id = id ?? name!,
            Name = name ?? id!,
            VectorType = GetString(el, "vector_type", "vectorType"),
            Description = GetString(el, "desc", "description")
        };
    }

    private static string? GetString(JsonElement el, params string[] names)
    {
        foreach (var name in names)
        {
            if (el.TryGetProperty(name, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.String)
                    return prop.GetString();
                if (prop.ValueKind == JsonValueKind.Number)
                    return prop.GetRawText();
            }
        }
        return null;
    }
}
