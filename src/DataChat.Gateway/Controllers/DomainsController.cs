using DataChat.Core.Configuration;
using DataChat.Gateway.Models;
using Microsoft.AspNetCore.Mvc;

namespace DataChat.Gateway.Controllers;

[ApiController]
[Route("v1/domains")]
public sealed class DomainsController : ControllerBase
{
    private readonly DomainsConfiguration _domains;

    public DomainsController(DomainsConfiguration domains) => _domains = domains;

    [HttpGet]
    public ActionResult<IReadOnlyList<DomainItemDto>> List() => Ok(_domains.Domains.Select(ToDto).ToList());

    [HttpGet("{domainId}")]
    public ActionResult<DomainItemDto> Get(string domainId)
    {
        var d = _domains.Domains.FirstOrDefault(x =>
            string.Equals(x.Id, domainId, StringComparison.OrdinalIgnoreCase));
        return d is null ? NotFound() : Ok(ToDto(d));
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
