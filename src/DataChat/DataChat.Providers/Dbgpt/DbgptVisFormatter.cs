using System.Text;
using System.Text.Json;

namespace DataChat.Providers.Dbgpt;

/// <summary>
/// 解析 DB-GPT 非流式响应中的 &lt;chart-view&gt;，转为 Markdown 表格等可读文本。
/// </summary>
internal static class DbgptVisFormatter
{
    public static string FormatAssistantContent(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return raw;

        var sb = new StringBuilder();
        var i = 0;
        while (i < raw.Length)
        {
            var tagStart = raw.IndexOf("<chart-view", i, StringComparison.OrdinalIgnoreCase);
            if (tagStart < 0)
            {
                sb.Append(raw.AsSpan(i));
                break;
            }

            sb.Append(raw.AsSpan(i, tagStart - i));
            var jsonStart = raw.IndexOf('{', tagStart);
            if (jsonStart < 0)
            {
                i = tagStart + 1;
                continue;
            }

            var jsonEnd = FindBalancedBraceEnd(raw, jsonStart);
            if (jsonEnd < 0)
            {
                i = tagStart + 1;
                continue;
            }

            var chartJson = raw.Substring(jsonStart, jsonEnd - jsonStart + 1);
            sb.Append(TryFormatChartJson(chartJson));

            var tagEnd = raw.IndexOf("/>", jsonEnd, StringComparison.Ordinal);
            i = tagEnd >= 0 ? tagEnd + 2 : jsonEnd + 1;
        }

        return sb.ToString().Trim();
    }

    private static int FindBalancedBraceEnd(string text, int start)
    {
        var depth = 0;
        var inString = false;
        var escape = false;

        for (var i = start; i < text.Length; i++)
        {
            var ch = text[i];
            if (inString)
            {
                if (escape) escape = false;
                else if (ch == '\\') escape = true;
                else if (ch == '"') inString = false;
                continue;
            }

            if (ch == '"') inString = true;
            else if (ch == '{') depth++;
            else if (ch == '}')
            {
                depth--;
                if (depth == 0) return i;
            }
        }

        return -1;
    }

    private static string TryFormatChartJson(string chartJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(chartJson);
            var root = doc.RootElement;
            var type = root.TryGetProperty("type", out var t) ? t.GetString() : null;
            var sql = root.TryGetProperty("sql", out var s) ? s.GetString() : null;

            if (string.Equals(type, "response_table", StringComparison.OrdinalIgnoreCase)
                && root.TryGetProperty("data", out var data)
                && data.ValueKind == JsonValueKind.Array
                && data.GetArrayLength() > 0)
            {
                return BuildMarkdownTable(data, sql);
            }
        }
        catch (JsonException)
        {
            // ignore malformed chart payload
        }

        return string.Empty;
    }

    private static string BuildMarkdownTable(JsonElement rows, string? sql)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(sql))
            sb.AppendLine("```sql").AppendLine(sql.Trim()).AppendLine("```").AppendLine();

        var first = rows[0];
        if (first.ValueKind != JsonValueKind.Object)
            return sb.ToString();

        var columns = first.EnumerateObject().Select(p => p.Name).ToList();
        sb.AppendLine("| " + string.Join(" | ", columns) + " |");
        sb.AppendLine("| " + string.Join(" | ", columns.Select(_ => "---")) + " |");

        foreach (var row in rows.EnumerateArray())
        {
            var cells = columns.Select(col =>
            {
                if (!row.TryGetProperty(col, out var v)) return "";
                return v.ValueKind switch
                {
                    JsonValueKind.String => v.GetString() ?? "",
                    JsonValueKind.Number => v.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => "",
                    _ => v.GetRawText()
                };
            });
            sb.AppendLine("| " + string.Join(" | ", cells) + " |");
        }

        return sb.ToString().TrimEnd();
    }
}
