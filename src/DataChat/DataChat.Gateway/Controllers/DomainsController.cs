using DataChat.Core.Abstractions;
using DataChat.Core.Configuration;
using DataChat.Gateway.Auth;
using DataChat.Gateway.Models;
using DataChat.Gateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataChat.Gateway.Controllers;

[ApiController]
[Route("v1/domains")]
public sealed class DomainsController : ControllerBase
{
    private readonly IDomainCatalog _catalog;
    private readonly ICurrentUserService _currentUser;
    private readonly IAgentAccessService _access;

    public DomainsController(
        IDomainCatalog catalog,
        ICurrentUserService currentUser,
        IAgentAccessService access)
    {
        _catalog = catalog;
        _currentUser = currentUser;
        _access = access;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DomainItemDto>>> List(CancellationToken cancellationToken)
    {
        var domains = _catalog.Current.Domains;
        var allowed = await _access.GetAllowedAgentIdsAsync(_currentUser.Current, cancellationToken);
        if (allowed is not null)
            domains = domains.Where(d => allowed.Contains(d.Id)).ToList();

        return Ok(domains.Select(ToDto).ToList());
    }

    /// <summary>从数据库或文件重新加载领域配置。</summary>
    [HttpPost("reload")]
    public async Task<IActionResult> Reload(CancellationToken cancellationToken)
    {
        await _catalog.ReloadAsync(cancellationToken);
        return Ok(new
        {
            ok = true,
            count = _catalog.Current.Domains.Count,
            source = "reloaded"
        });
    }

    private static DomainItemDto ToDto(DomainProfile d) => new()
    {
        Id = d.Id,
        DisplayName = d.DisplayName,
        ChatMode = d.ChatMode,
        Provider = d.Provider,
        Model = d.Model,
        Placeholder = d.Placeholder,
        QuickPrompts = d.QuickPrompts ?? [],
        Dbgpt = d.Dbgpt is null ? null : new DbgptDomainInfoDto
        {
            ChatMode = d.Dbgpt.ChatMode,
            AppId = d.Dbgpt.AppId,
            DatasourceId = d.Dbgpt.DatasourceId,
            KnowledgeSpaceName = d.Dbgpt.KnowledgeSpaceName
        },
        Coze = d.Coze is null ? null : new CozeDomainInfoDto
        {
            BotId = d.Coze.BotId,
            Endpoint = d.Coze.Endpoint
        }
    };
}
