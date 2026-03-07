using System.Security.Claims;
using Carter;
using CleanArc.Application.Features.Streaks.Commands;
using CleanArc.Application.Features.Streaks.Queries;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Mediator;

namespace CleanArc.Web.Api.Endpoints;

public class StreakEndpoints : ICarterModule
{
  private readonly string _routePrefix = "/api/v{version:apiVersion}/Streaks/";
  private readonly double _version = 1.1;
  private readonly string _tag = "Streaks";

  public void AddRoutes(IEndpointRouteBuilder app)
  {
    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}", async (ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new GetStreakQuery(userId));
      return result.ToEndpointResult();
    }), _version, "GetStreak", _tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapPost($"{_routePrefix}check-in", async (ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new CheckInCommand(userId));
      return result.ToEndpointResult();
    }), _version, "CheckIn", _tag).RequireAuthorization();
  }
}
