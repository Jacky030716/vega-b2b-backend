using System.Security.Claims;
using Carter;
using CleanArc.Application.Features.Mascots.Commands;
using CleanArc.Application.Features.Mascots.Queries;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Endpoints;

public class MascotEndpoints : ICarterModule
{
  private readonly string _routePrefix = "/api/v{version:apiVersion}/Mascots/";
  private readonly double _version = 1.1;
  private readonly string _tag = "Mascots";

  public void AddRoutes(IEndpointRouteBuilder app)
  {
    // Get all mascots
    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}", async (ISender sender) =>
    {
      var result = await sender.Send(new GetAllMascotsQuery());
      return result.ToEndpointResult();
    }), _version, "GetAllMascots", _tag).RequireAuthorization();

    // Get user mascots
    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}user", async (ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new GetUserMascotsQuery(userId));
      return result.ToEndpointResult();
    }), _version, "GetUserMascots", _tag).RequireAuthorization();

    // Equip mascot
    app.MapEndpoint(builder => builder.MapPost($"{_routePrefix}equip", async ([FromBody] EquipMascotRequest request, ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new EquipMascotCommand(userId, request.MascotId));
      return result.ToEndpointResult();
    }), _version, "EquipMascot", _tag).RequireAuthorization();

    // Admin: Create mascot
    app.MapEndpoint(builder => builder.MapPost($"{_routePrefix}", async ([FromBody] CreateMascotCommand command, ISender sender) =>
    {
      var result = await sender.Send(command);
      return result.ToEndpointResult();
    }), _version, "CreateMascot", _tag).RequireAuthorization("AdminPolicy");
  }
}

public record EquipMascotRequest(int MascotId);
