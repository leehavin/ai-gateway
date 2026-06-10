using System.Text;
using System.Text.Json;
using DataChat.Core.Chat;
using Microsoft.AspNetCore.Http.Features;

namespace DataChat.Gateway.Services;

public static class SseResponseWriter
{
    public static void PrepareHeaders(HttpResponse response)
    {
        response.ContentType = "text/event-stream; charset=utf-8";
        response.Headers.CacheControl = "no-cache";
        response.Headers.Connection = "keep-alive";
        response.HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
    }

    public static async Task WriteStreamAsync(
        HttpResponse response,
        IAsyncEnumerable<ChatChunk> chunks,
        CancellationToken cancellationToken)
    {
        PrepareHeaders(response);

        try
        {
            await foreach (var chunk in chunks.WithCancellation(cancellationToken))
            {
                if (chunk.Error is not null)
                {
                    await WriteEventAsync(response, JsonSerializer.Serialize(new { error = chunk.Error }), cancellationToken);
                    break;
                }

                if (chunk.ThinkingDelta is not null)
                    await WriteEventAsync(
                        response,
                        JsonSerializer.Serialize(new { type = "thinking", delta = chunk.ThinkingDelta }),
                        cancellationToken);

                if (chunk.TextDelta is not null)
                    await WriteEventAsync(
                        response,
                        JsonSerializer.Serialize(new { type = "delta", delta = chunk.TextDelta }),
                        cancellationToken);

                if (chunk.Citations is { Count: > 0 })
                    await WriteEventAsync(
                        response,
                        JsonSerializer.Serialize(new { type = "citations", citations = chunk.Citations }),
                        cancellationToken);

                if (chunk.WorkflowInterrupt is not null)
                {
                    var wi = chunk.WorkflowInterrupt;
                    await WriteEventAsync(
                        response,
                        JsonSerializer.Serialize(new
                        {
                            type = "workflow_interrupt",
                            workflowId = wi.WorkflowId,
                            eventId = wi.EventId,
                            interruptType = wi.InterruptType,
                            nodeTitle = wi.NodeTitle,
                            prompt = wi.Prompt
                        }),
                        cancellationToken);
                }

                if (chunk.WorkflowDebugUrl is not null)
                    await WriteEventAsync(
                        response,
                        JsonSerializer.Serialize(new { type = "workflow_done", debugUrl = chunk.WorkflowDebugUrl }),
                        cancellationToken);

                if (chunk.IsCompleted)
                    break;
            }

            if (!IsStreamCanceled(response, cancellationToken))
                await WriteEventAsync(response, "[DONE]", cancellationToken);
        }
        catch (OperationCanceledException) when (IsStreamCanceled(response, cancellationToken))
        {
            // 客户端断开、切换会话或 AbortController 取消 — SSE 正常结束，不向上抛
        }
    }

    private static bool IsStreamCanceled(HttpResponse response, CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested
        || response.HttpContext.RequestAborted.IsCancellationRequested;

    private static async Task WriteEventAsync(HttpResponse response, string data, CancellationToken cancellationToken)
    {
        if (IsStreamCanceled(response, cancellationToken))
            throw new OperationCanceledException(cancellationToken);

        var line = data == "[DONE]" ? "data: [DONE]\n\n" : $"data: {data}\n\n";
        await response.WriteAsync(line, Encoding.UTF8, cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }
}
