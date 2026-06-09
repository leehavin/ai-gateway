using DataChat.Gateway.Auth;
using DataChat.Gateway.Models;
using DataChat.Gateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataChat.Gateway.Controllers;

[ApiController]
[Route("v1/coze")]
public sealed class CozeController : ControllerBase
{
    private readonly CozeResourceService _coze;
    private readonly CozeWorkflowService _workflows;
    private readonly ICurrentUserService _currentUser;
    private readonly IAgentAccessService _access;

    public CozeController(
        CozeResourceService coze,
        CozeWorkflowService workflows,
        ICurrentUserService currentUser,
        IAgentAccessService access)
    {
        _coze = coze;
        _workflows = workflows;
        _currentUser = currentUser;
        _access = access;
    }

    [HttpGet("bots")]
    public async Task<ActionResult<IReadOnlyList<CozeBotSummary>>> ListBots(CancellationToken cancellationToken)
    {
        var bots = _coze.ListConfiguredBots();
        var allowed = await _access.GetAllowedAgentIdsAsync(_currentUser.Current, cancellationToken);
        if (allowed is not null)
            bots = bots.Where(b => allowed.Contains(b.DomainId)).ToList();
        return Ok(bots);
    }

    /// <summary>列出 Coze 领域可执行工作流（配置 + Bot 在线绑定）。</summary>
    [HttpGet("workflows")]
    public async Task<ActionResult<IReadOnlyList<CozeWorkflowItemDto>>> ListWorkflows(
        [FromQuery] string domain,
        CancellationToken cancellationToken)
    {
        var validationError = _workflows.ValidateDomain(domain);
        if (validationError is not null)
            return BadRequest(new { error = "BadRequest", message = validationError });

        try
        {
            return Ok(await _workflows.ListWorkflowsAsync(domain, cancellationToken));
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "CozeError", message = ex.Message });
        }
    }

    /// <summary>流式执行 Coze 工作流（代理 workflow/stream_run）。</summary>
    [HttpPost("workflow/stream")]
    [Produces("text/event-stream")]
    public async Task StreamWorkflow(
        [FromBody] CozeWorkflowStreamRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateStreamRequest(request);
        if (validationError is not null)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { error = "BadRequest", message = validationError }, cancellationToken);
            return;
        }

        await SseResponseWriter.WriteStreamAsync(
            Response,
            _workflows.StreamRunAsync(request, cancellationToken),
            cancellationToken);
    }

    /// <summary>恢复被问答节点中断的工作流（代理 workflow/stream_resume）。</summary>
    [HttpPost("workflow/resume")]
    [Produces("text/event-stream")]
    public async Task ResumeWorkflow(
        [FromBody] CozeWorkflowResumeRequestDto request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateResumeRequest(request);
        if (validationError is not null)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { error = "BadRequest", message = validationError }, cancellationToken);
            return;
        }

        await SseResponseWriter.WriteStreamAsync(
            Response,
            _workflows.StreamResumeAsync(request, cancellationToken),
            cancellationToken);
    }

    private string? ValidateStreamRequest(CozeWorkflowStreamRequest request)
    {
        var domainError = _workflows.ValidateDomain(request.Domain);
        if (domainError is not null) return domainError;
        if (string.IsNullOrWhiteSpace(request.WorkflowId))
            return "workflowId 不能为空。";
        return null;
    }

    private static string? ValidateResumeRequest(CozeWorkflowResumeRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Domain))
            return "domain 不能为空。";
        if (string.IsNullOrWhiteSpace(request.WorkflowId))
            return "workflowId 不能为空。";
        if (string.IsNullOrWhiteSpace(request.EventId))
            return "eventId 不能为空。";
        if (string.IsNullOrWhiteSpace(request.ResumeData))
            return "resumeData 不能为空。";
        return null;
    }
}
