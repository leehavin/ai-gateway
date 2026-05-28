using DataChat.Core.Configuration;
using DataChat.Gateway.Configuration;
using DataChat.Gateway.Models;
using DataChat.Gateway.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DataChat.Gateway.Controllers;

[ApiController]
[Route("v1")]
public sealed class HealthController : ControllerBase
{
    private readonly GatewayOptions _options;
    private readonly DomainsConfiguration _domains;
    private readonly DbgptResourceService _dbgpt;
    private readonly CozeResourceService _coze;

    public HealthController(
        IOptions<GatewayOptions> options,
        DomainsConfiguration domains,
        DbgptResourceService dbgpt,
        CozeResourceService coze)
    {
        _options = options.Value;
        _domains = domains;
        _dbgpt = dbgpt;
        _coze = coze;
    }

    [HttpGet("health")]
    public async Task<ActionResult<GatewayStatusDto>> Get(CancellationToken cancellationToken)
    {
        var cozeCount = _coze.ListConfiguredBots().Count;
        var dbgptOk = !_options.UseMock && await _dbgpt.PingAsync(cancellationToken);
        var cozeOk = !_options.UseMock && cozeCount > 0 && await _coze.PingAsync(cancellationToken);
        return Ok(new GatewayStatusDto
        {
            Status = "ok",
            Service = "DataChat.Gateway",
            UseMock = _options.UseMock,
            DbgptReachable = dbgptOk,
            DbgptBaseUrl = _domains.Defaults.DbgptBaseUrl,
            CozeReachable = cozeOk,
            CozeEndpoint = _domains.Defaults.CozeEndpoint,
            CozeDomainCount = cozeCount,
            DomainCount = _domains.Domains.Count,
            SupportedDbgptChatModes =
            [
                "chat_normal", "chat_app", "chat_data", "chat_knowledge",
                "chat_flow", "chat_with_db_qa", "chat_dashboard"
            ]
        });
    }
}
