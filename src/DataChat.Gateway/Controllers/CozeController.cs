using DataChat.Gateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataChat.Gateway.Controllers;

[ApiController]
[Route("v1/coze")]
public sealed class CozeController : ControllerBase
{
    private readonly CozeResourceService _coze;

    public CozeController(CozeResourceService coze) => _coze = coze;

    [HttpGet("bots")]
    public ActionResult<IReadOnlyList<CozeBotSummary>> ListBots() =>
        Ok(_coze.ListConfiguredBots());
}
