using System.Text.Json;
using DataChat.Gateway.Models;

namespace DataChat.Gateway.Services;

public sealed class FeedbackService
{
    private readonly string _logPath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public FeedbackService()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "data");
        Directory.CreateDirectory(dir);
        _logPath = Path.Combine(dir, "feedback.jsonl");
    }

    public async Task RecordAsync(FeedbackRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Rating))
            throw new ArgumentException("rating 不能为空。");

        var rating = request.Rating.Trim().ToLowerInvariant();
        if (rating is not ("up" or "down"))
            throw new ArgumentException("rating 须为 up 或 down。");

        var entry = new
        {
            at = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            request.SessionId,
            request.MessageId,
            request.Domain,
            rating,
            comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim()
        };

        var line = JsonSerializer.Serialize(entry) + Environment.NewLine;
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(_logPath, line, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }
}
