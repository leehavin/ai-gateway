using DataChat.Gateway.Models;
using DataChat.Gateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataChat.Gateway.Controllers;

[ApiController]
[Route("v1/chat")]
public sealed class ChatController : ControllerBase
{
    private readonly GatewayChatService _chatService;

    public ChatController(GatewayChatService chatService) => _chatService = chatService;

    /// <summary>流式对话（SSE），与 Web 嵌入组件 / WinForms 共用契约。</summary>
    [HttpPost("stream")]
    [Produces("text/event-stream")]
    public async Task Stream([FromBody] ChatStreamRequest request, CancellationToken cancellationToken)
    {
        var validationError = _chatService.ValidateRequest(request);
        if (validationError is not null)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { error = "BadRequest", message = validationError }, cancellationToken);
            return;
        }

        var domain = _chatService.ResolveDomain(request.Domain)!;
        var context = _chatService.BuildContext(request, domain);
        await SseResponseWriter.WriteStreamAsync(
            Response,
            _chatService.StreamAsync(context, cancellationToken),
            cancellationToken);
    }
}
