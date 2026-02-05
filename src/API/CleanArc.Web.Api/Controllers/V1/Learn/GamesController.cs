using Asp.Versioning;
using CleanArc.Application.Features.Games.Queries.GetGameCatalog;
using CleanArc.Application.Features.Games.Queries.GetGameConfig;
using CleanArc.WebFramework.BaseController;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Controllers.V1.Learn;

[ApiVersion("1")]
[ApiController]
[Route("api/v{version:apiVersion}/Games")]
[Authorize]
public class GamesController(ISender sender) : BaseController
{
  [HttpGet("catalog")]
  public async Task<IActionResult> GetCatalog()
  {
    var query = await sender.Send(new GetGameCatalogQuery());
    return base.OperationResult(query);
  }

  [HttpGet("{gameType}/config")]
  public async Task<IActionResult> GetConfig(string gameType)
  {
    var query = await sender.Send(new GetGameConfigQuery(gameType));
    return base.OperationResult(query);
  }
}
