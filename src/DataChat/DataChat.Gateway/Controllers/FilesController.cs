using DataChat.Gateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataChat.Gateway.Controllers;

[ApiController]
[Route("v1/files")]
public sealed class FilesController : ControllerBase
{
    private readonly FileStorageService _files;

    public FilesController(FileStorageService files) => _files = files;

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
}
