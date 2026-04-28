using System.Security.Claims;
using Carter;
using CleanArc.Application.Contracts.Infrastructure.Documents;
using CleanArc.Application.Features.Games.Commands;
using CleanArc.Application.Features.Games.Queries;
using CleanArc.SharedKernel.Extensions;
using CleanArc.Web.Api.Contracts.Requests.Games;
using CleanArc.WebFramework.WebExtensions;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

#nullable enable

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
/// Teacher flow:
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

    // ── Teacher: create a custom challenge for a game ───────────────────────

    app.MapEndpoint(builder => builder.MapPost(
        $"{_routePrefix}{{gameKey}}/challenges",
        async (string gameKey, [FromBody] CreateChallengeRequest request, ClaimsPrincipal user, ISender sender) =>
        {
          var userId = int.Parse(user.Identity!.GetUserId());
          var result = await sender.Send(new CreateChallengeCommand(
              userId,
              gameKey,
              request.Title,
              request.Description,
              request.DifficultyLevel,
              request.ContentData,
              request.IsAIGenerated,
              request.CreationMode,
              request.SourcePrompt,
              request.SourceDocumentName,
              request.ClassroomId
          ));

          return result.ToEndpointResult();
        }
    ), _version, "CreateChallenge", _tag)
      .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

    // ── Teacher: generate AI draft challenge (prompt + optional syllabus) ───

    app.MapEndpoint(builder => builder.MapPost(
        $"{_routePrefix}{{gameKey}}/challenges/ai-draft",
        async (
          string gameKey,
          [FromForm] GenerateAiChallengeDraftRequest request,
          ClaimsPrincipal user,
          ISender sender,
          CancellationToken cancellationToken) =>
        {
          var userId = int.Parse(user.Identity!.GetUserId());

          var documentPayload = await BuildDocumentPayloadAsync(request.SyllabusFile, cancellationToken);
          var result = await sender.Send(new GenerateAiChallengeDraftCommand(
              userId,
              gameKey,
              request.ClassroomId,
              request.Prompt,
              documentPayload
            ),
            cancellationToken);

          return result.ToEndpointResult();
        }
    ), _version, "GenerateAiChallengeDraft", _tag)
      .DisableAntiforgery()
      .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

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

  private static async Task<ChallengeDocumentPayload?> BuildDocumentPayloadAsync(IFormFile? file, CancellationToken cancellationToken)
  {
    if (file is null || file.Length == 0)
      return null;

    await using var memoryStream = new MemoryStream();
    await file.CopyToAsync(memoryStream, cancellationToken);
    return new ChallengeDocumentPayload(file.FileName, file.ContentType, memoryStream.ToArray());
  }
}

public record CompleteAttemptRequest(int Score, int StarsEarned, string? AttemptData);

public record CreateChallengeRequest(
  string Title,
  string Description,
  int DifficultyLevel,
  string ContentData,
  bool IsAIGenerated,
  string? CreationMode,
  string? SourcePrompt,
  string? SourceDocumentName,
  // Classroom to assign this challenge to. Required for teacher-created classroom challenges.
  int? ClassroomId = null
);
