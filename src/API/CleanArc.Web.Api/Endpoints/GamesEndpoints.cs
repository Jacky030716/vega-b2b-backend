using Carter;
using CleanArc.Application.Features.Games.Queries.GetGameCatalog;
using CleanArc.Application.Features.Games.Queries.GetGameConfig;
using CleanArc.WebFramework.WebExtensions;
using Mediator;

namespace CleanArc.Web.Api.Endpoints;

public class GamesEndpoints : ICarterModule
{
  private readonly string _routePrefix = "/api/v{version:apiVersion}/Games/";
  private readonly double _version = 1.1;
  private readonly string _tag = "Games";

  public void AddRoutes(IEndpointRouteBuilder app)
  {
    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}catalog", async (ISender sender) =>
    {
      var result = await sender.Send(new GetGameCatalogQuery());
      return result.ToEndpointResult();
    }), _version, "GamesCatalog", _tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}{{gameType}}/config", async (string gameType, ISender sender) =>
    {
      var result = await sender.Send(new GetGameConfigQuery(gameType));
      return result.ToEndpointResult();
    }), _version, "GameConfig", _tag).RequireAuthorization();
  }
}
