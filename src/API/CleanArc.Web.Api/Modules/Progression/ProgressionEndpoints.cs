using System.Security.Claims;
using Carter;
using CleanArc.Application.Features.Progression.Commands;
using CleanArc.Application.Features.Progression.Queries;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Endpoints;

public class ProgressionEndpoints : ICarterModule
{
  private readonly string _routePrefix = "/api/v{version:apiVersion}/Progression/";
  private readonly double _version = 1.1;
  private readonly string _tag = "Progression";

  public void AddRoutes(IEndpointRouteBuilder app)
  {
    // Get user progress
    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}progress", async (ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new GetUserProgressQuery(userId));
      return result.ToEndpointResult();
    }), _version, "GetUserProgress", _tag).RequireAuthorization();

    // Get all levels
    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}levels", async (ISender sender) =>
    {
      var result = await sender.Send(new GetLevelsQuery());
      return result.ToEndpointResult();
    }), _version, "GetLevels", _tag).RequireAuthorization();

    // Add XP (internal/system use)
    app.MapEndpoint(builder => builder.MapPost($"{_routePrefix}xp", async ([FromBody] AddXpRequest request, ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new AddXpCommand(userId, request.Amount, request.Reason));
      return result.ToEndpointResult();
    }), _version, "AddXp", _tag).RequireAuthorization();

    // Admin: Create level
    app.MapEndpoint(builder => builder.MapPost($"{_routePrefix}levels", async ([FromBody] CreateLevelCommand command, ISender sender) =>
    {
      var result = await sender.Send(command);
      return result.ToEndpointResult();
    }), _version, "CreateLevel", _tag).RequireAuthorization("AdminPolicy");
  }
}

public record AddXpRequest(int Amount, string Reason);
