using System.Security.Claims;
using Carter;
using CleanArc.Application.Features.Missions.Commands;
using CleanArc.Application.Features.Missions.Queries;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Endpoints;

public class MissionEndpoints : ICarterModule
{
  private readonly string _routePrefix = "/api/v{version:apiVersion}/Missions/";
  private readonly double _version = 1.1;
  private readonly string _tag = "Missions";

  public void AddRoutes(IEndpointRouteBuilder app)
  {
    // Get active missions
    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}", async (ISender sender) =>
    {
      var result = await sender.Send(new GetActiveMissionsQuery());
      return result.ToEndpointResult();
    }), _version, "GetActiveMissions", _tag).RequireAuthorization();

    // Get user missions
    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}user", async (ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new GetUserMissionsQuery(userId));
      return result.ToEndpointResult();
    }), _version, "GetUserMissions", _tag).RequireAuthorization();

    // Claim mission reward
    app.MapEndpoint(builder => builder.MapPost($"{_routePrefix}{{missionId}}/claim", async (int missionId, ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new ClaimMissionRewardCommand(userId, missionId));
      return result.ToEndpointResult();
    }), _version, "ClaimMissionReward", _tag).RequireAuthorization();

    // Admin: Create mission
    app.MapEndpoint(builder => builder.MapPost($"{_routePrefix}", async ([FromBody] CreateMissionCommand command, ISender sender) =>
    {
      var result = await sender.Send(command);
      return result.ToEndpointResult();
    }), _version, "CreateMission", _tag).RequireAuthorization("AdminPolicy");
  }
}
