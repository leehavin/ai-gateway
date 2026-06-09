using DataChat.Core.Configuration;

namespace DataChat.Providers.Coze;

/// <summary>工作流开始节点输入参数（供列表展示与前端提示）。</summary>
public sealed class CozeWorkflowInputSpec
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public bool Required { get; init; }
    public string? Label { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<string> Accept { get; init; } = [];
}

public static class CozeWorkflowInputCatalog
{
    public static IReadOnlyList<CozeWorkflowInputSpec> ParseFromApiParameters(System.Text.Json.JsonElement? parameters)
    {
        if (parameters is not { ValueKind: System.Text.Json.JsonValueKind.Object } root)
            return [];

        var list = new List<CozeWorkflowInputSpec>();
        foreach (var prop in root.EnumerateObject())
        {
            if (prop.Value.ValueKind != System.Text.Json.JsonValueKind.Object)
                continue;
            list.Add(ParseParameter(prop.Name, prop.Value));
        }
        return list;
    }

    public static IReadOnlyList<CozeWorkflowInputSpec> MergeWithConfig(
        IReadOnlyList<CozeWorkflowInputSpec> apiSpecs,
        IReadOnlyList<CozeWorkflowInputOptions>? configured)
    {
        if (configured is not { Count: > 0 })
            return apiSpecs;

        var byName = apiSpecs.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);
        var merged = new List<CozeWorkflowInputSpec>();

        foreach (var cfg in configured)
        {
            if (string.IsNullOrWhiteSpace(cfg.Name)) continue;
            if (byName.TryGetValue(cfg.Name, out var api))
            {
                merged.Add(new CozeWorkflowInputSpec
                {
                    Name = cfg.Name,
                    Type = string.IsNullOrWhiteSpace(cfg.Type) ? api.Type : cfg.Type.Trim(),
                    Required = cfg.Required ?? api.Required,
                    Label = cfg.Label ?? api.Label,
                    Description = cfg.Description ?? api.Description,
                    Accept = cfg.Accept is { Count: > 0 } ? cfg.Accept : api.Accept
                });
                byName.Remove(cfg.Name);
            }
            else
            {
                merged.Add(new CozeWorkflowInputSpec
                {
                    Name = cfg.Name,
                    Type = string.IsNullOrWhiteSpace(cfg.Type) ? "string" : cfg.Type.Trim(),
                    Required = cfg.Required ?? false,
                    Label = cfg.Label,
                    Description = cfg.Description,
                    Accept = cfg.Accept ?? []
                });
            }
        }

        merged.AddRange(byName.Values);
        return merged.OrderBy(s => s.Required ? 0 : 1).ThenBy(s => s.Name).ToList();
    }

    public static string BuildInputSummary(IReadOnlyList<CozeWorkflowInputSpec> inputs)
    {
        if (inputs.Count == 0) return string.Empty;

        static string Label(CozeWorkflowInputSpec s) =>
            !string.IsNullOrWhiteSpace(s.Label) ? s.Label!.Trim() : s.Name;

        var required = inputs.Where(i => i.Required).Select(i => $"{Label(i)}({TypeLabel(i.Type)})").ToList();
        var optional = inputs.Where(i => !i.Required).Select(i => $"{Label(i)}({TypeLabel(i.Type)})").ToList();

        if (required.Count > 0 && optional.Count > 0)
            return $"必填：{string.Join("、", required)}；可选：{string.Join("、", optional)}";
        if (required.Count > 0)
            return $"必填：{string.Join("、", required)}";
        return $"可选：{string.Join("、", optional)}";
    }

    public static bool NeedsAttachment(IReadOnlyList<CozeWorkflowInputSpec> inputs) =>
        inputs.Any(IsAttachmentType);

    public static string BuildPlaceholderHint(
        IReadOnlyList<CozeWorkflowInputSpec> inputs,
        string? inputHint,
        string displayName)
    {
        if (!string.IsNullOrWhiteSpace(inputHint))
            return inputHint.Trim();

        if (inputs.Count == 0)
            return $"已选「{displayName}」，输入后发送执行";

        var textInputs = inputs.Where(i => IsTextType(i.Type)).ToList();
        var fileInputs = inputs.Where(IsAttachmentType).ToList();
        var requiredFiles = fileInputs.Where(i => i.Required).ToList();
        var optionalFiles = fileInputs.Where(i => !i.Required).ToList();

        var parts = new List<string>();
        if (requiredFiles.Count > 0)
            parts.Add(FormatAttachmentHint("必填附件", requiredFiles));
        if (textInputs.Count > 0)
        {
            var names = textInputs.Select(DisplayLabel);
            parts.Add($"输入：{string.Join("、", names)}");
        }
        if (optionalFiles.Count > 0)
            parts.Add(FormatAttachmentHint("可选附件", optionalFiles));

        return parts.Count > 0
            ? $"「{displayName}」{string.Join("；", parts)}，完成后发送"
            : $"已选「{displayName}」，输入后发送执行";
    }

    private static CozeWorkflowInputSpec ParseParameter(string name, System.Text.Json.JsonElement node)
    {
        var type = node.TryGetProperty("type", out var typeEl) && typeEl.ValueKind == System.Text.Json.JsonValueKind.String
            ? typeEl.GetString() ?? "string"
            : "string";
        var required = node.TryGetProperty("required", out var reqEl) &&
                       reqEl.ValueKind == System.Text.Json.JsonValueKind.True;
        var description = node.TryGetProperty("description", out var descEl) &&
                          descEl.ValueKind == System.Text.Json.JsonValueKind.String
            ? descEl.GetString()
            : null;

        if (string.Equals(type, "array", StringComparison.OrdinalIgnoreCase) &&
            node.TryGetProperty("items", out var items) &&
            items.TryGetProperty("type", out var itemType) &&
            itemType.ValueKind == System.Text.Json.JsonValueKind.String)
        {
            type = $"array<{itemType.GetString()}>";
        }

        return new CozeWorkflowInputSpec
        {
            Name = name,
            Type = type,
            Required = required,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Label = InferLabel(name),
            Accept = InferAccept(type)
        };
    }

    private static bool IsTextType(string type) =>
        type.Equals("string", StringComparison.OrdinalIgnoreCase) ||
        type.Equals("integer", StringComparison.OrdinalIgnoreCase) ||
        type.Equals("number", StringComparison.OrdinalIgnoreCase) ||
        type.Equals("boolean", StringComparison.OrdinalIgnoreCase);

    private static bool IsAttachmentType(CozeWorkflowInputSpec spec)
    {
        var t = spec.Type.ToLowerInvariant();
        return t is "image" or "file" or "audio" or "document" or "pdf" or "doc" or "txt" ||
               t.Contains("file", StringComparison.Ordinal) ||
               t.Contains("image", StringComparison.Ordinal) ||
               t.Contains("doc", StringComparison.Ordinal) ||
               t.StartsWith("array<image", StringComparison.Ordinal) ||
               t.StartsWith("array<file", StringComparison.Ordinal) ||
               t.StartsWith("array<audio", StringComparison.Ordinal) ||
               t.StartsWith("array<document", StringComparison.Ordinal) ||
               t.StartsWith("array<doc", StringComparison.Ordinal);
    }

    private static string DisplayLabel(CozeWorkflowInputSpec spec) =>
        !string.IsNullOrWhiteSpace(spec.Label) ? spec.Label!.Trim() : spec.Name;

    private static string FormatAttachmentHint(string prefix, IReadOnlyList<CozeWorkflowInputSpec> files)
    {
        var names = files.Select(DisplayLabel);
        var accept = files.SelectMany(i => i.Accept).Distinct().ToList();
        var acceptNote = accept.Count > 0 ? $"（{string.Join(" ", accept)}）" : "";
        return $"{prefix}：{string.Join("、", names)}{acceptNote}";
    }

    private static string? InferLabel(string name) =>
        name.Trim().ToLowerInvariant() switch
        {
            "doc" or "document" or "pdf" => "文档",
            "img" or "image" => "图片",
            "input" or "query" or "question" => "输入",
            "txt" or "text" => "文本",
            _ => null
        };

    private static string TypeLabel(string type) => type.ToLowerInvariant() switch
    {
        "string" => "文本",
        "image" => "图片",
        "file" => "文件",
        "doc" or "document" => "文档",
        "txt" => "文本文件",
        "audio" => "音频",
        "integer" or "number" => "数字",
        "boolean" => "是/否",
        var t when t.StartsWith("array<image") => "多图",
        var t when t.StartsWith("array<file") => "多文件",
        var t when t.StartsWith("array<audio") => "多音频",
        "array" => "列表",
        "object" => "对象",
        _ => type
    };

    private static IReadOnlyList<string> InferAccept(string type)
    {
        var t = type.ToLowerInvariant();
        if (t == "image" || t.StartsWith("array<image", StringComparison.Ordinal))
            return [".png", ".jpg", ".jpeg", ".webp", ".gif"];
        if (t is "file" or "doc" or "document" || t.StartsWith("array<file", StringComparison.Ordinal) ||
            t.StartsWith("array<doc", StringComparison.Ordinal))
            return [".pdf", ".doc", ".docx", ".txt", ".md"];
        if (t == "txt")
            return [".txt", ".md"];
        if (t == "audio" || t.StartsWith("array<audio", StringComparison.Ordinal))
            return [".mp3", ".wav", ".m4a"];
        return [];
    }
}

