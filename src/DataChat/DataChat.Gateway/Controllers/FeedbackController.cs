using DataChat.Gateway.Models;
using DataChat.Gateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataChat.Gateway.Controllers;

[ApiController]
[Route("v1/feedback")]
public sealed class FeedbackController : ControllerBase
{
    private readonly FeedbackService _feedback;

    public FeedbackController(FeedbackService feedback) => _feedback = feedback;

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] FeedbackRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId) || string.IsNullOrWhiteSpace(request.MessageId))
            return BadRequest(new { message = "sessionId 与 messageId 不能为空。" });
        if (string.IsNullOrWhiteSpace(request.Domain))
            return BadRequest(new { message = "domain 不能为空。" });

        try
        {
            await _feedback.RecordAsync(request, cancellationToken);
            return Ok(new { ok = true });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
