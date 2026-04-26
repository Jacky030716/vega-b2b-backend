using System.Security.Claims;
using Carter;
using CleanArc.Application.Features.Challenges.Commands;
using CleanArc.Application.Features.Challenges.Queries;
using CleanArc.Domain.Entities.Quiz;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Endpoints;

public sealed class ChallengeManagementEndpoints : ICarterModule
{
  private const string RoutePrefix = "/api/v{version:apiVersion}/challenges/";
  private const double Version = 1.1;
  private const string Tag = "Challenge Management";

  public void AddRoutes(IEndpointRouteBuilder app)
  {
    app.MapEndpoint(builder => builder.MapGet(
      $"{RoutePrefix}board",
      async (
        [FromQuery] int classId,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken) =>
      {
        var teacherId = int.Parse(user.Identity!.GetUserId());
        var result = await sender.Send(new GetChallengeBoardQuery(classId, teacherId), cancellationToken);
        return result.ToEndpointResult();
      }), Version, "GetChallengeBoard", Tag)
      .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

    app.MapEndpoint(builder => builder.MapGet(
      $"{RoutePrefix}recommended",
      async (
        [FromQuery] int classId,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken) =>
      {
        var teacherId = int.Parse(user.Identity!.GetUserId());
        var result = await sender.Send(new GetRecommendedChallengesQuery(classId, teacherId), cancellationToken);
        return result.ToEndpointResult();
      }), Version, "GetRecommendedChallenges", Tag)
      .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

    app.MapEndpoint(builder => builder.MapPatch(
      $"{RoutePrefix}{{challengeId:int}}/lifecycle",
      async (
        int challengeId,
        [FromBody] UpdateChallengeLifecycleRequest request,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken) =>
      {
        if (!Enum.TryParse<ChallengeLifecycleState>(request.LifecycleState, true, out var lifecycleState))
        {
          return Results.BadRequest(new Dictionary<string, List<string>>
          {
            { "LifecycleState", new() { "Invalid lifecycle state." } }
          });
        }

        var teacherId = int.Parse(user.Identity!.GetUserId());
        var result = await sender.Send(
          new UpdateChallengeLifecycleCommand(challengeId, teacherId, lifecycleState, request.IsPinned),
          cancellationToken);
        return result.ToEndpointResult();
      }), Version, "UpdateChallengeLifecycle", Tag)
      .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));
  }

  public sealed record UpdateChallengeLifecycleRequest(string LifecycleState, bool IsPinned);
}
