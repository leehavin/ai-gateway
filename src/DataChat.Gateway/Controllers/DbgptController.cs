using System.Text.Json;
using DataChat.Gateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataChat.Gateway.Controllers;

/// <summary>代理 DB-GPT 官方 v2 serve API（对齐 http://docs.dbgpt.cn/docs/api）。</summary>
[ApiController]
[Route("v1/dbgpt")]
public sealed class DbgptController : ControllerBase
{
    private readonly DbgptResourceService _resources;

    public DbgptController(DbgptResourceService resources) => _resources = resources;

    [HttpGet("ping")]
    public async Task<IActionResult> Ping(CancellationToken cancellationToken) =>
        Ok(new { reachable = await _resources.PingAsync(cancellationToken) });

    [HttpGet("apps")]
    public async Task<IActionResult> Apps(CancellationToken cancellationToken) =>
        Ok(await _resources.GetAppsAsync(cancellationToken));

    [HttpGet("apps/{appId}")]
    public async Task<IActionResult> App(string appId, CancellationToken cancellationToken)
    {
        var app = await _resources.GetAppAsync(appId, cancellationToken);
        return app is null ? NotFound() : Ok(app);
    }

    [HttpGet("datasources")]
    public async Task<IActionResult> Datasources(CancellationToken cancellationToken) =>
        Ok(await _resources.GetDatasourcesAsync(cancellationToken));

    [HttpGet("datasources/{id}")]
    public async Task<IActionResult> Datasource(string id, CancellationToken cancellationToken)
    {
        var ds = await _resources.GetDatasourceAsync(id, cancellationToken);
        return ds is null ? NotFound() : Ok(ds);
    }

    [HttpGet("knowledge/spaces")]
    public async Task<IActionResult> KnowledgeSpaces(CancellationToken cancellationToken) =>
        Ok(await _resources.GetKnowledgeSpacesAsync(cancellationToken));

    /// <summary>透传 DB-GPT 原始 JSON（用于尚未封装的 v2 管理接口）。</summary>
    [HttpPost("proxy/{*path}")]
    public async Task ProxyPost(string path, [FromBody] JsonElement? body, CancellationToken cancellationToken) =>
        await Proxy(HttpMethod.Post, path, body, cancellationToken);

    [HttpPut("proxy/{*path}")]
    public async Task ProxyPut(string path, [FromBody] JsonElement? body, CancellationToken cancellationToken) =>
        await Proxy(HttpMethod.Put, path, body, cancellationToken);

    [HttpDelete("proxy/{*path}")]
    public async Task ProxyDelete(string path, CancellationToken cancellationToken) =>
        await Proxy(HttpMethod.Delete, path, null, cancellationToken);

    private async Task<IActionResult> Proxy(
        HttpMethod method,
        string path,
        JsonElement? body,
        CancellationToken cancellationToken)
    {
        var dbgptPath = "/api/v2/" + path.TrimStart('/');
        object? payload = null;
        if (body is { } el && el.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null)
            payload = JsonSerializer.Deserialize<object>(el.GetRawText());

        using var response = await _resources.ForwardAsync(method, dbgptPath, payload, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = content,
            ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json"
        };
    }
}
