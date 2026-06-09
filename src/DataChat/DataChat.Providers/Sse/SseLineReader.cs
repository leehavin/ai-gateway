using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace DataChat.Providers.Sse;

internal static class SseLineReader
{
    public static async IAsyncEnumerable<string> ReadDataLinesAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null) break;
            if (!line.StartsWith("data:", StringComparison.Ordinal))
                continue;
            var data = line["data:".Length..].TrimStart();
            if (data.Length == 0) continue;
            if (data == "[DONE]") yield break;
            yield return data;
        }
    }

    public static string? TryExtractOpenAiDelta(string jsonLine)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonLine);
            if (doc.RootElement.TryGetProperty("choices", out var choices) &&
                choices.GetArrayLength() > 0 &&
                choices[0].TryGetProperty("delta", out var delta) &&
                delta.TryGetProperty("content", out var content))
                return content.GetString();
            if (doc.RootElement.TryGetProperty("delta", out var d))
                return d.GetString();
        }
        catch (JsonException)
        {
            // ignore malformed chunk
        }
        return null;
    }
}
