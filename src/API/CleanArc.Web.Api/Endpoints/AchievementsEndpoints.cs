using System.Security.Claims;
using Carter;
using CleanArc.Application.Features.Achievements.Commands;
using CleanArc.Application.Features.Achievements.Queries;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Endpoints;

public class AchievementsEndpoints : ICarterModule
{
  private readonly string _routePrefix = "/api/v{version:apiVersion}/Badges/";
  private readonly double _version = 1.1;
  private readonly string _tag = "Badges";

  public void AddRoutes(IEndpointRouteBuilder app)
  {
    // GET /Badges/ — full badge catalog (all badges, no unlock state)
    app.MapEndpoint(builder => builder.MapGet(_routePrefix, async (ISender sender) =>
    {
      var result = await sender.Send(new GetBadgesQuery());
      return result.ToEndpointResult();
    }), _version, "GetAllBadges", _tag).RequireAuthorization();

    // GET /Badges/user — badges the authenticated user has earned
    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}user", async (ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new GetUserBadgesQuery(userId));
      return result.ToEndpointResult();
    }), _version, "GetUserBadges", _tag).RequireAuthorization();

    // POST /Badges/featured — pin a badge to a featured slot
    app.MapEndpoint(builder => builder.MapPost($"{_routePrefix}featured", async (
        [FromBody] SetFeaturedBadgeRequest request,
        ClaimsPrincipal user,
        ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new SetFeaturedBadgeCommand(userId, request.BadgeId, request.SlotIndex));
      return result.ToEndpointResult();
    }), _version, "SetFeaturedBadge", _tag).RequireAuthorization();
  }
}

public record SetFeaturedBadgeRequest(int BadgeId, int SlotIndex);
