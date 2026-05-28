using DataChat.Gateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataChat.Gateway.Controllers;

/// <summary>Coze（扣子）资源与健康检查。</summary>
[ApiController]
[Route("v1/coze")]
public sealed class CozeController : ControllerBase
{
    private readonly CozeResourceService _coze;

    public CozeController(CozeResourceService coze) => _coze = coze;

    [HttpGet("ping")]
    public async Task<ActionResult<object>> Ping(CancellationToken cancellationToken)
    {
        var ok = await _coze.PingAsync(cancellationToken);
        return Ok(new { reachable = ok });
    }

    [HttpGet("bots")]
    public ActionResult<IReadOnlyList<CozeBotSummary>> ListBots() =>
        Ok(_coze.ListConfiguredBots());
}
