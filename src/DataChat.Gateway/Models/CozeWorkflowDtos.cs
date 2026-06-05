namespace DataChat.Gateway.Models;

public sealed class CozeWorkflowItemDto
{
    public required string WorkflowId { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public string? IconUrl { get; init; }
    public string? AppId { get; init; }
    public string InputParameter { get; init; } = "BOT_USER_INPUT";
}

public sealed class CozeWorkflowStreamRequest
{
    public required string Domain { get; set; }
    public required string WorkflowId { get; set; }
    /// <summary>用户输入，映射到工作流 input 参数。</summary>
    public string? Input { get; set; }
    public Dictionary<string, string>? Parameters { get; set; }
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
