using DataChat.Gateway.Auth;
using DataChat.Gateway.Models;
using DataChat.Gateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataChat.Gateway.Controllers;

[ApiController]
[Route("v1/files")]
public sealed class FilesController : ControllerBase
{
    private readonly FileStorageService _files;
    private readonly GatewayAuthService _auth;

    public FilesController(FileStorageService files, GatewayAuthService auth)
    {
        _files = files;
        _auth = auth;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(52_428_800)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
            return BadRequest(new { error = "BadRequest", message = "请选择要上传的文件（字段名 file）。" });

        try
        {
            var result = await _files.SaveAsync(file, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = "BadRequest", message = ex.Message });
        }
    }

    /// <summary>宿主嵌入：用 ServiceKey 登记本地文件路径，返回 fileId 供 chat-ui / 工作流使用。</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(FileUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterFileRequest request,
        CancellationToken cancellationToken)
    {
        var serviceKey = ResolveServiceKey(request.ServiceKey);
        if (!_auth.IsTrustedServiceKey(serviceKey))
            return Unauthorized(new { error = "Unauthorized", message = "缺少或无效的服务密钥。" });

        try
        {
            var result = await _files.RegisterFromPathAsync(request.Path, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = "BadRequest", message = ex.Message });
        }
    }

    [HttpGet("{fileId}")]
    public IActionResult Download(string fileId)
    {
        var stored = _files.Get(fileId);
        if (stored is null)
            return NotFound();

        var contentType = GetContentType(stored.Name);
        return PhysicalFile(stored.Path, contentType, stored.Name, enableRangeProcessing: true);
    }

    private static string GetContentType(string name)
    {
        var ext = Path.GetExtension(name).ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".md" => "text/markdown",
            ".json" => "application/json",
            ".csv" => "text/csv",
            _ => "application/octet-stream"
        };
    }

    private string? ResolveServiceKey(string? bodyKey)
    {
        if (!string.IsNullOrWhiteSpace(bodyKey))
            return bodyKey.Trim();

        if (Request.Headers.TryGetValue("X-Service-Key", out var header) && !string.IsNullOrWhiteSpace(header))
            return header.ToString().Trim();

        var auth = Request.Headers.Authorization.ToString();
        if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return auth["Bearer ".Length..].Trim();

        return null;
    }
}
