using DataChat.Gateway.Models;
using DataChat.Gateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataChat.Gateway.Controllers;

[ApiController]
[Route("v1/sessions")]
public sealed class SessionsController : ControllerBase
{
    private readonly GatewaySessionService _sessions;

    public SessionsController(GatewaySessionService sessions) => _sessions = sessions;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SessionSummaryDto>>> List(
        [FromQuery] string? domain,
        CancellationToken cancellationToken)
    {
        if (!_sessions.IsEnabled) return NotFound(new { error = "SessionApiDisabled" });
        return Ok(await _sessions.ListAsync(domain, cancellationToken));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SessionDetailDto>> Get(string id, CancellationToken cancellationToken)
    {
        if (!_sessions.IsEnabled) return NotFound(new { error = "SessionApiDisabled" });
        var detail = await _sessions.GetAsync(id, cancellationToken);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPost]
    public async Task<ActionResult<SessionDetailDto>> Create(
        [FromBody] CreateSessionRequest request,
        CancellationToken cancellationToken)
    {
        if (!_sessions.IsEnabled) return NotFound(new { error = "SessionApiDisabled" });
        try
        {
            return Ok(await _sessions.CreateAsync(request, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SessionDetailDto>> Sync(
        string id,
        [FromBody] SyncSessionRequest request,
        CancellationToken cancellationToken)
    {
        if (!_sessions.IsEnabled) return NotFound(new { error = "SessionApiDisabled" });
        try
        {
            var detail = await _sessions.SyncAsync(id, request, cancellationToken);
            return detail is null ? NotFound() : Ok(detail);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        if (!_sessions.IsEnabled) return NotFound(new { error = "SessionApiDisabled" });
        await _sessions.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
