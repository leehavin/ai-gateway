using DataChat.Core.Abstractions;
using DataChat.Core.Configuration;
using DataChat.Gateway.Models;
using Microsoft.AspNetCore.Mvc;

namespace DataChat.Gateway.Controllers;

[ApiController]
[Route("v1/domains")]
public sealed class DomainsController : ControllerBase
{
    private readonly IDomainCatalog _catalog;

    public DomainsController(IDomainCatalog catalog) => _catalog = catalog;

    [HttpGet]
    public ActionResult<IReadOnlyList<DomainItemDto>> List() =>
        Ok(_catalog.Current.Domains.Select(ToDto).ToList());

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
