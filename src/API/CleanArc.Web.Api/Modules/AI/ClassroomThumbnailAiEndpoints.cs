using System.Security.Claims;
using System.Text.Json;
using Carter;
using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Application.Contracts.Infrastructure.ClassroomThumbnails;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Microsoft.AspNetCore.Mvc;

#nullable enable

namespace CleanArc.Web.Api.Endpoints;

public sealed class ClassroomThumbnailAiEndpoints : ICarterModule
{
  private const string RoutePrefix = "/api/v{version:apiVersion}/ai/classroom-thumbnails/";
  private const double Version = 1.1;
  private const string Tag = "AI Classroom Thumbnails";

  public void AddRoutes(IEndpointRouteBuilder app)
  {
    app.MapEndpoint(builder => builder.MapGet(
      $"{RoutePrefix}quota",
      async (
        ClaimsPrincipal user,
        IAiUsageService aiUsageService,
        CancellationToken cancellationToken) =>
      {
        var userId = int.Parse(user.Identity!.GetUserId());
        var quota = await aiUsageService.GetRemainingQuotaAsync(userId, AiFeatureTypes.ClassroomThumbnailGeneration, cancellationToken);
        return Results.Ok(new
        {
          monthlyLimit = quota.MonthlyLimit,
          used = quota.Used,
          remaining = quota.Remaining
        });
      }), Version, "GetClassroomThumbnailQuota", Tag)
      .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

    app.MapEndpoint(builder => builder.MapPost(
      $"{RoutePrefix}generate",
      async (
        [FromBody] ClassroomThumbnailGenerationDto request,
        ClaimsPrincipal user,
        IAiRateLimitService rateLimitService,
        IAiUsageService aiUsageService,
        IClassroomThumbnailImageGenerationService generationService,
        IAiAuditService aiAuditService,
        CancellationToken cancellationToken) =>
      {
        var userId = int.Parse(user.Identity!.GetUserId());
        var subjects = request.Subjects?.Where(subject => !string.IsNullOrWhiteSpace(subject)).Select(subject => subject.Trim()).ToArray()
          ?? Array.Empty<string>();
        if (string.IsNullOrWhiteSpace(request.ClassroomName))
          return Results.BadRequest(new { message = "Classroom name is required." });

        if (request.ClassroomName.Length > 200)
          return Results.BadRequest(new { message = "Classroom name must be 200 characters or fewer." });

        if (request.YearLevel is < 1 or > 6)
          return Results.BadRequest(new { message = "Year level must be between 1 and 6." });

        if (string.IsNullOrWhiteSpace(request.ThumbnailPrompt))
          return Results.BadRequest(new { message = "Thumbnail description is required." });

        if (request.ThumbnailPrompt.Trim().Length > 180)
          return Results.BadRequest(new { message = "Thumbnail description must be 180 characters or fewer." });

        var promptUsed = BuildPrompt(request);
        var auditLogId = await aiAuditService.StartAsync(
          new AiAuditStartRequest(
            AiUseCases.ClassroomThumbnailGeneration,
            "HUGGING_FACE",
            null,
            "v1",
            JsonSerializer.Serialize(request),
            userId),
          cancellationToken);

        var rateLimit = await rateLimitService.TryAcquireAsync(userId, AiFeatureTypes.ClassroomThumbnailGeneration, cancellationToken);
        if (!rateLimit.Allowed)
        {
          await aiAuditService.FailAsync(auditLogId, null, new[] { "Too many AI requests. Please try again later." }, cancellationToken);
          return Results.Json(
            new { message = "Too many AI requests. Please try again later.", retryAfterSeconds = rateLimit.RetryAfterSeconds },
            statusCode: StatusCodes.Status429TooManyRequests);
        }

        var quota = await aiUsageService.GetRemainingQuotaAsync(userId, AiFeatureTypes.ClassroomThumbnailGeneration, cancellationToken);
        if (quota.Remaining <= 0)
        {
          await aiAuditService.FailAsync(auditLogId, null, new[] { "Your AI thumbnail quota is exhausted for this month." }, cancellationToken);
          return Results.BadRequest(new { message = "Your AI thumbnail quota is exhausted for this month." });
        }

        // Enqueue Hangfire background job
        Hangfire.BackgroundJob.Enqueue<IBackgroundJobExecutor>(x =>
          x.ExecuteClassroomThumbnailJobAsync(
            auditLogId,
            userId,
            request.ClassroomName,
            request.YearLevel,
            subjects,
            request.Description,
            request.ThumbnailPrompt));

        return Results.Accepted($"/api/v1.1/ai/jobs/{auditLogId}", new { auditLogId, status = "PENDING" });
      }), Version, "GenerateClassroomThumbnail", Tag)
      .DisableAntiforgery()
      .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));
  }

  private static string BuildPrompt(ClassroomThumbnailGenerationDto request)
  {
    var subjectText = request.Subjects?.Count > 0 ? string.Join(", ", request.Subjects) : "classroom learning";
    var description = string.IsNullOrWhiteSpace(request.Description) ? string.Empty : $" The classroom description is: {request.Description.Trim()}";

    return
      $"Create a child-safe classroom thumbnail for a Malaysian primary school learning app. Teacher request: {request.ThumbnailPrompt.Trim()}. Classroom: {request.ClassroomName.Trim()}, Year {request.YearLevel}, subjects: {subjectText}.{description} Use a playful educational illustration style with books, learning icons, friendly colors, a 1:1 square composition, a transparent background, centered and fully visible subjects, no cropping, no truncation, no cut off edges, and no text overlay.";
  }
}

public sealed record ClassroomThumbnailGenerationDto(
  string ClassroomName,
  int YearLevel,
  IReadOnlyList<string> Subjects,
  string? Description,
  string ThumbnailPrompt);
