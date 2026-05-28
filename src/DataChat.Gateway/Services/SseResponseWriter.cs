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

        await foreach (var chunk in chunks.WithCancellation(cancellationToken))
        {
            if (chunk.Error is not null)
            {
                await WriteEventAsync(response, JsonSerializer.Serialize(new { error = chunk.Error }), cancellationToken);
                break;
            }

            if (chunk.TextDelta is not null)
                await WriteEventAsync(response, JsonSerializer.Serialize(new { delta = chunk.TextDelta }), cancellationToken);

            if (chunk.IsCompleted)
                break;
        }

        await WriteEventAsync(response, "[DONE]", cancellationToken);
    }

    private static async Task WriteEventAsync(HttpResponse response, string data, CancellationToken cancellationToken)
    {
        var line = data == "[DONE]" ? "data: [DONE]\n\n" : $"data: {data}\n\n";
        await response.WriteAsync(line, Encoding.UTF8, cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }
}
