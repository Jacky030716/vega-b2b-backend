using System.Security.Claims;
using Carter;
using CleanArc.Application.Features.Friends.Commands;
using CleanArc.Application.Features.Friends.Queries;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Endpoints;

public class FriendEndpoints : ICarterModule
{
  private readonly string _routePrefix = "/api/v{version:apiVersion}/Friends/";
  private readonly double _version = 1.1;
  private readonly string _tag = "Friends";

  public void AddRoutes(IEndpointRouteBuilder app)
  {
    // Get friends and pending requests
    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}", async (ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new GetFriendsQuery(userId));
      return result.ToEndpointResult();
    }), _version, "GetFriends", _tag).RequireAuthorization();

    // Send friend request
    app.MapEndpoint(builder => builder.MapPost($"{_routePrefix}request", async ([FromBody] SendFriendRequest request, ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new SendFriendRequestCommand(userId, request.AddresseeId));
      return result.ToEndpointResult();
    }), _version, "SendFriendRequest", _tag).RequireAuthorization();

    // Respond to friend request
    app.MapEndpoint(builder => builder.MapPost($"{_routePrefix}respond", async ([FromBody] RespondFriendRequest request, ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new RespondFriendRequestCommand(userId, request.FriendshipId, request.Action));
      return result.ToEndpointResult();
    }), _version, "RespondFriendRequest", _tag).RequireAuthorization();
  }
}

public record SendFriendRequest(int AddresseeId);
public record RespondFriendRequest(int FriendshipId, string Action);
