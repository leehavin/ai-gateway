namespace DataChat.Gateway.Models;

public sealed class CozeWorkflowInputSpecDto
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public bool Required { get; init; }
    public string? Label { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<string> Accept { get; init; } = [];
}

public sealed class CozeWorkflowItemDto
{
    public required string WorkflowId { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public string? IconUrl { get; init; }
    public string? AppId { get; init; }
    public string InputParameter { get; init; } = "BOT_USER_INPUT";
    /// <summary>一句话输入要求，如「必填：问题(文本)、PDF(文件)」。</summary>
    public string? InputSummary { get; init; }
    /// <summary>输入框 placeholder 提示，可覆盖自动生成文案。</summary>
    public string? InputHint { get; init; }
    public bool NeedsAttachment { get; init; }
    public IReadOnlyList<CozeWorkflowInputSpecDto> Inputs { get; init; } = [];
}

public sealed class CozeWorkflowStreamRequest
{
    public required string Domain { get; set; }
    public required string WorkflowId { get; set; }
    /// <summary>用户输入，映射到工作流 input 参数。</summary>
    public string? Input { get; set; }
    public Dictionary<string, string>? Parameters { get; set; }
    /// <summary>Gateway 已上传的附件，服务端会转传 Coze 并映射到工作流文件类参数（如 doc）。</summary>
    public List<ChatAttachmentDto>? Attachments { get; set; }
}

public sealed class CozeWorkflowResumeRequestDto
{
    public required string Domain { get; set; }
    public required string WorkflowId { get; set; }
    public required string EventId { get; set; }
    public required int InterruptType { get; set; }
    public required string ResumeData { get; set; }
}

public sealed class CozeWorkflowInterruptDto
{
    public required string WorkflowId { get; init; }
    public required string EventId { get; init; }
    public required int InterruptType { get; init; }
    public string? NodeTitle { get; init; }
    public string? Prompt { get; init; }
}
