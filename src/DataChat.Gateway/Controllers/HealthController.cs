using DataChat.Core.Abstractions;
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
    private readonly IDomainCatalog _domains;
    private readonly DbgptResourceService _dbgpt;

    public HealthController(
        IOptions<GatewayOptions> options,
        IDomainCatalog domains,
        DbgptResourceService dbgpt)
    {
        _options = options.Value;
        _domains = domains;
        _dbgpt = dbgpt;
    }

    [HttpGet("health")]
    public async Task<ActionResult<GatewayStatusDto>> Get(CancellationToken cancellationToken)
    {
        var dbgptOk = !_options.UseMock && await _dbgpt.PingAsync(cancellationToken);
        return Ok(new GatewayStatusDto
        {
            Status = "ok",
            Service = "DataChat.Gateway",
            UseMock = _options.UseMock,
            DbgptReachable = dbgptOk,
            DbgptBaseUrl = _domains.Current.Defaults.DbgptBaseUrl,
            DomainCount = _domains.Current.Domains.Count
        });
    }
}
