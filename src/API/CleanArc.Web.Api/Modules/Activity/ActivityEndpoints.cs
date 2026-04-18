using System.Security.Claims;
using Carter;
using CleanArc.Application.Features.Activity.Queries;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Endpoints;

public class ActivityEndpoints : ICarterModule
{
  private readonly string _routePrefix = "/api/v{version:apiVersion}/Activity/";
  private readonly double _version = 1.1;
  private readonly string _tag = "Activity";

  public void AddRoutes(IEndpointRouteBuilder app)
  {
    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}", async ([FromQuery] int? count, ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new GetRecentActivityQuery(userId, count ?? 20));
      return result.ToEndpointResult();
    }), _version, "GetRecentActivity", _tag).RequireAuthorization();
  }
}
