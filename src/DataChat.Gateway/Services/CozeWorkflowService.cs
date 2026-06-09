using DataChat.Core.Abstractions;
using DataChat.Core.Chat;
using DataChat.Core.Configuration;
using DataChat.Gateway.Models;
using DataChat.Providers.Coze;

namespace DataChat.Gateway.Services;

public sealed class CozeWorkflowService
{
    private const int WorkflowSchemaConcurrency = 4;

    private readonly IDomainCatalog _domains;
    private readonly CozeOpenApiClient _cozeApi;

    public CozeWorkflowService(IDomainCatalog domains, CozeOpenApiClient cozeApi)
    {
        _domains = domains;
        _cozeApi = cozeApi;
    }

    public DomainProfile? ResolveDomain(string domainId) =>
        _domains.Current.Domains.FirstOrDefault(d =>
            string.Equals(d.Id, domainId, StringComparison.OrdinalIgnoreCase));

    public string? ValidateDomain(string domainId)
    {
        if (string.IsNullOrWhiteSpace(domainId))
            return "domain 不能为空。";
        var domain = ResolveDomain(domainId);
        if (domain is null)
            return "未知领域: " + domainId;
        if (!domain.Provider.Equals("coze", StringComparison.OrdinalIgnoreCase))
            return "该领域不是 Coze 类型。";
        if (domain.Coze is null || string.IsNullOrWhiteSpace(domain.Coze.BotId))
            return "领域未配置 Coze BotId。";
        return null;
    }

    public async Task<IReadOnlyList<CozeWorkflowItemDto>> ListWorkflowsAsync(
        string domainId,
        CancellationToken cancellationToken)
    {
        var error = ValidateDomain(domainId);
        if (error is not null) throw new InvalidOperationException(error);

        var domain = ResolveDomain(domainId)!;
        var coze = domain.Coze!;
        var defaults = _domains.Current.Defaults;
        var list = await _cozeApi.ListBotWorkflowsAsync(domain, defaults, cancellationToken);

        using var gate = new SemaphoreSlim(WorkflowSchemaConcurrency);
        var tasks = list.Select(w => EnrichWorkflowDtoAsync(coze, defaults, w, cancellationToken, gate));
        var items = await Task.WhenAll(tasks);
        return items.OrderBy(i => i.DisplayName).ToList();
    }

    private async Task<CozeWorkflowItemDto> EnrichWorkflowDtoAsync(
        CozeDomainOptions coze,
        GlobalDefaults defaults,
        CozeWorkflowCatalogItem workflow,
        CancellationToken cancellationToken,
        SemaphoreSlim gate)
    {
        var configured = coze.Workflows?.FirstOrDefault(w =>
            string.Equals(w.WorkflowId, workflow.WorkflowId, StringComparison.OrdinalIgnoreCase));

        await gate.WaitAsync(cancellationToken);
        IReadOnlyList<CozeWorkflowInputSpec> merged;
        try
        {
            var apiSpecs = await _cozeApi.TryGetWorkflowInputSpecsAsync(
                coze, defaults, workflow.WorkflowId, cancellationToken);
            merged = CozeWorkflowInputCatalog.MergeWithConfig(apiSpecs, configured?.Inputs);
        }
        finally
        {
            gate.Release();
        }

        var inputParameter = !string.IsNullOrWhiteSpace(configured?.InputParameter)
            ? configured!.InputParameter.Trim()
            : ResolvePrimaryTextParameter(merged, workflow.InputParameter);

        var description = configured?.Description ?? workflow.Description;
        var inputSummary = CozeWorkflowInputCatalog.BuildInputSummary(merged);
        if (string.IsNullOrWhiteSpace(inputSummary) && !string.IsNullOrWhiteSpace(description))
            inputSummary = description.Trim();

        var inputHint = !string.IsNullOrWhiteSpace(configured?.InputHint)
            ? configured!.InputHint!.Trim()
            : CozeWorkflowInputCatalog.BuildPlaceholderHint(merged, null, workflow.DisplayName);
        if (merged.Count == 0 &&
            string.IsNullOrWhiteSpace(configured?.InputHint) &&
            !string.IsNullOrWhiteSpace(description))
        {
            inputHint = $"「{workflow.DisplayName}」{description.Trim()}，输入后发送执行";
        }

        return new CozeWorkflowItemDto
        {
            WorkflowId = workflow.WorkflowId,
            DisplayName = configured?.DisplayName ?? workflow.DisplayName,
            Description = description,
            IconUrl = workflow.IconUrl,
            AppId = workflow.AppId ?? configured?.AppId,
            InputParameter = inputParameter,
            InputSummary = string.IsNullOrWhiteSpace(inputSummary) ? null : inputSummary,
            InputHint = inputHint,
            NeedsAttachment = CozeWorkflowInputCatalog.NeedsAttachment(merged),
            Inputs = merged.Select(MapInputSpec).ToList()
        };
    }

    private static string ResolvePrimaryTextParameter(
        IReadOnlyList<CozeWorkflowInputSpec> inputs,
        string fallback)
    {
        var text = inputs.FirstOrDefault(i =>
            i.Required &&
            (i.Type.Equals("string", StringComparison.OrdinalIgnoreCase) ||
             i.Type.Equals("integer", StringComparison.OrdinalIgnoreCase) ||
             i.Type.Equals("number", StringComparison.OrdinalIgnoreCase)));
        if (text is not null) return text.Name;

        var anyText = inputs.FirstOrDefault(i =>
            i.Type.Equals("string", StringComparison.OrdinalIgnoreCase));
        return anyText?.Name ?? fallback;
    }

    private static CozeWorkflowInputSpecDto MapInputSpec(CozeWorkflowInputSpec spec) => new()
    {
        Name = spec.Name,
        Type = spec.Type,
        Required = spec.Required,
        Label = spec.Label,
        Description = spec.Description,
        Accept = spec.Accept
    };

    public async Task<IReadOnlyDictionary<string, string>> BuildParametersAsync(
        string domainId,
        string workflowId,
        string? input,
        Dictionary<string, string>? extra,
        CancellationToken cancellationToken)
    {
        var domain = ResolveDomain(domainId)!;
        var wf = await _cozeApi.ResolveWorkflowAsync(
            domain,
            _domains.Current.Defaults,
            workflowId,
            cancellationToken) ?? throw new InvalidOperationException($"未知工作流: {workflowId}");

        var coze = domain.Coze!;
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (coze.Workflows is not null)
        {
            var configured = coze.Workflows.FirstOrDefault(w =>
                string.Equals(w.WorkflowId, workflowId, StringComparison.OrdinalIgnoreCase));
            if (configured?.DefaultParameters is not null)
            {
                foreach (var kv in configured.DefaultParameters)
                    parameters[kv.Key] = kv.Value;
            }
        }

        if (extra is not null)
        {
            foreach (var kv in extra)
                parameters[kv.Key] = kv.Value;
        }

        if (!string.IsNullOrWhiteSpace(input))
            parameters[wf.InputParameter] = input.Trim();

        return parameters;
    }

    public IAsyncEnumerable<ChatChunk> StreamRunAsync(
        CozeWorkflowStreamRequest request,
        CancellationToken cancellationToken)
    {
        var domain = ResolveDomain(request.Domain)!;
        return StreamRunInternalAsync(domain, request, cancellationToken);
    }

    public IAsyncEnumerable<ChatChunk> StreamResumeAsync(
        CozeWorkflowResumeRequestDto request,
        CancellationToken cancellationToken)
    {
        var domain = ResolveDomain(request.Domain)!;
        return _cozeApi.StreamResumeAsync(
            domain,
            _domains.Current.Defaults,
            new CozeWorkflowResumeRequest
            {
                WorkflowId = request.WorkflowId,
                EventId = request.EventId,
                InterruptType = request.InterruptType,
                ResumeData = request.ResumeData
            },
            cancellationToken);
    }

    private async IAsyncEnumerable<ChatChunk> StreamRunInternalAsync(
        DomainProfile domain,
        CozeWorkflowStreamRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, string>? parameters = null;
        string? paramError = null;
        try
        {
            parameters = await BuildParametersAsync(
                request.Domain,
                request.WorkflowId,
                request.Input,
                request.Parameters,
                cancellationToken);
        }
        catch (Exception ex)
        {
            paramError = ex.Message;
        }

        if (paramError is not null)
        {
            yield return new ChatChunk { Error = paramError, IsCompleted = true };
            yield break;
        }

        await foreach (var chunk in _cozeApi.StreamRunAsync(
            domain,
            _domains.Current.Defaults,
            request.WorkflowId,
            parameters!,
            cancellationToken))
            yield return chunk;
    }
}
