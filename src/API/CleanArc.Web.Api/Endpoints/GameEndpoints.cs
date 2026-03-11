using System.Security.Claims;
using Carter;
using CleanArc.Application.Features.Games.Commands;
using CleanArc.Application.Features.Games.Queries;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Endpoints;

/// <summary>
/// Games + Challenges + Attempts — the core adventure-map API.
///
/// Student flow:
///   1. GET  /Games                          → pick a game
///   2. GET  /Games/{key}/challenges         → see the adventure map (nodes + unlock/star state)
///   3. POST /Games/{key}/challenges/{id}/attempts  → start a play session
///   4. POST /Games/attempts/{id}/complete   → finish the session (score, stars, XP)
///
/// Teacher flow (future dashboard):
///   - POST /Games/{key}/challenges          → create a custom challenge (inherits gameKey)
/// </summary>
public class GameEndpoints : ICarterModule
{
  private readonly string _routePrefix = "/api/v{version:apiVersion}/Games/";
  private readonly double _version = 1.1;
  private readonly string _tag = "Games";

  public void AddRoutes(IEndpointRouteBuilder app)
  {
    // ── Public: list all available games (no auth needed for browsing) ──────

    app.MapEndpoint(builder => builder.MapGet(_routePrefix, async (ISender sender) =>
    {
      var result = await sender.Send(new GetGamesQuery());
      return result.ToEndpointResult();
    }), _version, "GetGames", _tag);

    // ── Student: adventure map for a specific game ───────────────────────────

    app.MapEndpoint(builder => builder.MapGet(
        $"{_routePrefix}{{gameKey}}/challenges",
        async (string gameKey, ClaimsPrincipal user, ISender sender) =>
        {
          var userId = int.Parse(user.Identity!.GetUserId());
          var result = await sender.Send(new GetChallengesForGameQuery(gameKey, userId));
          return result.ToEndpointResult();
        }
    ), _version, "GetAdventureMap", _tag).RequireAuthorization();

    // ── Student: start an attempt on a specific challenge ────────────────────

    app.MapEndpoint(builder => builder.MapPost(
        $"{_routePrefix}{{gameKey}}/challenges/{{challengeId:int}}/attempts",
        async (string gameKey, int challengeId, ClaimsPrincipal user, ISender sender) =>
        {
          var userId = int.Parse(user.Identity!.GetUserId());
          var result = await sender.Send(new CreateAttemptCommand(userId, challengeId));
          return result.ToEndpointResult();
        }
    ), _version, "CreateChallengeAttempt", _tag).RequireAuthorization();

    // ── Student: complete an attempt (submit score + stars) ──────────────────

    app.MapEndpoint(builder => builder.MapPost(
        $"{_routePrefix}attempts/{{attemptId:int}}/complete",
        async (int attemptId, [FromBody] CompleteAttemptRequest request, ClaimsPrincipal user, ISender sender) =>
        {
          var userId = int.Parse(user.Identity!.GetUserId());
          var result = await sender.Send(new CompleteAttemptCommand(
                  userId, attemptId, request.Score, request.StarsEarned, request.AttemptData ?? "{}"));
          return result.ToEndpointResult();
        }
    ), _version, "CompleteAttempt", _tag).RequireAuthorization();
  }
}

public record CompleteAttemptRequest(int Score, int StarsEarned, string? AttemptData);
