using System.Security.Claims;
using System.Text.Json;
using Carter;
using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Application.Contracts.Infrastructure.Documents;
using CleanArc.Application.Contracts.Persistence;
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

    app.MapEndpoint(builder => builder.MapGet("api/v1.1/temp-progress-debug", (CleanArc.Infrastructure.Persistence.ApplicationDbContext db) =>
    {
      var progress = db.ChallengeProgresses.ToList();
      return Results.Ok(progress);
    }), _version, "TempProgressDebug", _tag);

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
              request.ClassroomId,
              request.ModuleId,
              request.AiAuditLogId
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
          IUnitOfWork unitOfWork,
          IAiRateLimitService rateLimitService,
          IAiUsageService aiUsageService,
          IAiAuditService aiAuditService,
          IAiPromptRegistry promptRegistry,
          CancellationToken cancellationToken) =>
        {
          var userId = int.Parse(user.Identity!.GetUserId());

          if (gameKey is not ("spell_catcher" or "syllable_sushi" or "voice_bridge"))
              return Results.BadRequest(new { message = "Unsupported game key for AI generation." });

          var classroom = await unitOfWork.ClassroomRepository.GetClassroomByIdAsync(request.ClassroomId);
          if (classroom is null || !classroom.IsActive)
              return Results.NotFound(new { message = "Classroom not found." });

          if (classroom.TeacherId != userId)
              return Results.Json(new { message = "You are not authorized to generate challenges for this classroom." }, statusCode: StatusCodes.Status401Unauthorized);

          // Rate limit & Quota check
          var rateLimit = await rateLimitService.TryAcquireAsync(userId, AiFeatureTypes.CustomChallengeGeneration, cancellationToken);
          if (!rateLimit.Allowed)
          {
              return Results.Json(
                  new { message = "Too many AI requests. Please try again later.", retryAfterSeconds = rateLimit.RetryAfterSeconds },
                  statusCode: StatusCodes.Status429TooManyRequests);
          }

          var quota = await aiUsageService.GetRemainingQuotaAsync(userId, AiFeatureTypes.CustomChallengeGeneration, cancellationToken);
          if (quota.Remaining <= 0)
          {
              return Results.BadRequest(new { message = "Your AI quota is exhausted for this month." });
          }

          var promptText = request.Prompt?.Trim() ?? string.Empty;
          var documentPayload = await BuildDocumentPayloadAsync(request.SyllabusFile, cancellationToken);

          if (string.IsNullOrWhiteSpace(promptText) && documentPayload is null)
              return Results.BadRequest(new { message = "Provide a prompt or upload a syllabus document." });

          // Start Audit Log
          var promptDefinition = promptRegistry.Get(AiUseCases.CustomChallengeExtraction, gameKey);
          var auditLogId = await aiAuditService.StartAsync(
              new AiAuditStartRequest(
                  AiUseCases.CustomChallengeExtraction,
                  "GEMINI",
                  null,
                  promptDefinition.Version,
                  JsonSerializer.Serialize(new
                  {
                      request.ClassroomId,
                      gameKey,
                      prompt = promptText,
                      hasSyllabus = documentPayload is not null
                  }),
                  userId,
                  request.ClassroomId),
              cancellationToken);

          // Enqueue Hangfire background job
          Hangfire.BackgroundJob.Enqueue<IBackgroundJobExecutor>(x =>
              x.ExecuteChallengeDraftJobAsync(
                  auditLogId,
                  gameKey,
                  userId,
                  request.ClassroomId,
                  promptText,
                  documentPayload));

          return Results.Accepted($"/api/v1.1/ai/jobs/{auditLogId}", new { auditLogId, status = "PENDING" });
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
  int? ClassroomId = null,
  // Module to attach the classroom challenge to, when launched from a selected module.
  int? ModuleId = null,
  // AI audit row returned by the draft generation endpoint when saving an AI draft.
  int? AiAuditLogId = null
);
