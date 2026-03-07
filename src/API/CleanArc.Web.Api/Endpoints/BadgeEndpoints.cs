using System.Security.Claims;
using Carter;
using CleanArc.Application.Features.Badges.Commands;
using CleanArc.Application.Features.Badges.Queries;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Endpoints;

public class BadgeEndpoints : ICarterModule
{
  private readonly string _routePrefix = "/api/v{version:apiVersion}/Badges/";
  private readonly double _version = 1.1;
  private readonly string _tag = "Badges";

  public void AddRoutes(IEndpointRouteBuilder app)
  {
    // Get all badges
    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}", async (ISender sender) =>
    {
      var result = await sender.Send(new GetAllBadgesQuery());
      return result.ToEndpointResult();
    }), _version, "GetAllBadges", _tag).RequireAuthorization();

    // Get user badges
    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}user", async (ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new GetUserBadgesQuery(userId));
      return result.ToEndpointResult();
    }), _version, "GetUserBadges", _tag).RequireAuthorization();

    // Set featured badge
    app.MapEndpoint(builder => builder.MapPost($"{_routePrefix}featured", async ([FromBody] SetFeaturedBadgeRequest request, ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new SetFeaturedBadgeCommand(userId, request.BadgeId, request.SlotIndex));
      return result.ToEndpointResult();
    }), _version, "SetFeaturedBadge", _tag).RequireAuthorization();

    // Admin: Create badge
    app.MapEndpoint(builder => builder.MapPost($"{_routePrefix}", async ([FromBody] CreateBadgeCommand command, ISender sender) =>
    {
      var result = await sender.Send(command);
      return result.ToEndpointResult();
    }), _version, "CreateBadge", _tag).RequireAuthorization("AdminPolicy");
  }
}

public record SetFeaturedBadgeRequest(int BadgeId, int SlotIndex);
