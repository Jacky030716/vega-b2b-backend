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
        IClassroomThumbnailImageStorageService storageService,
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

        var generation = await generationService.GenerateAsync(
          new ClassroomThumbnailGenerationRequest(
            userId,
            request.ClassroomName,
            request.YearLevel,
            subjects,
            request.Description,
            request.StylePreset),
          cancellationToken);

        if (!generation.IsSuccess)
        {
          await aiAuditService.FailAsync(auditLogId, null, new[] { generation.ErrorMessage ?? "Thumbnail generation failed." }, cancellationToken);
          return Results.BadRequest(new { message = generation.ErrorMessage ?? "Thumbnail generation failed." });
        }

        var upload = await storageService.UploadAsync(
          generation.Result.ImageBytes,
          $"classroom-{Sanitize(request.ClassroomName)}-{request.YearLevel}",
          "image/png",
          cancellationToken);

        if (!upload.IsSuccess)
        {
          await aiAuditService.FailAsync(auditLogId, null, new[] { upload.ErrorMessage ?? "Thumbnail upload failed." }, cancellationToken);
          return Results.BadRequest(new { message = upload.ErrorMessage ?? "Thumbnail upload failed." });
        }

        await aiUsageService.ConsumeUsageAsync(
          userId,
          AiFeatureTypes.ClassroomThumbnailGeneration,
          "POST /api/v1.1/ai/classroom-thumbnails/generate",
          "HUGGING_FACE",
          generation.Result.ModelName,
          1,
          true,
          null,
          "classroom_thumbnail",
          null,
          cancellationToken);

        var refreshedQuota = await aiUsageService.GetRemainingQuotaAsync(userId, AiFeatureTypes.ClassroomThumbnailGeneration, cancellationToken);
        await aiAuditService.CompleteAsync(
          auditLogId,
          JsonSerializer.Serialize(new { promptUsed, model = generation.Result.ModelName }),
          JsonSerializer.Serialize(new
          {
            upload.Result.AssetId,
            upload.Result.Url,
            promptUsed,
            remainingQuota = refreshedQuota.Remaining
          }),
          AiValidationStatuses.Valid,
          Array.Empty<string>(),
          cancellationToken);

        return Results.Ok(new
        {
          assetId = upload.Result.AssetId,
          url = upload.Result.Url,
          promptUsed,
          remainingQuota = refreshedQuota.Remaining
        });
      }), Version, "GenerateClassroomThumbnail", Tag)
      .DisableAntiforgery()
      .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));
  }

  private static string BuildPrompt(ClassroomThumbnailGenerationDto request)
  {
    var subjectText = request.Subjects?.Count > 0 ? string.Join(", ", request.Subjects) : "classroom learning";
    var style = string.IsNullOrWhiteSpace(request.StylePreset) ? "playful learning" : request.StylePreset.Trim();
    var description = string.IsNullOrWhiteSpace(request.Description) ? string.Empty : $" The classroom description is: {request.Description.Trim()}";

    return
      $"Create a cute, colorful classroom thumbnail for a Year {request.YearLevel} {request.ClassroomName} class in a Malaysian primary school learning app. Include playful educational elements, books, icons, and a child-safe {style} aesthetic. Subject focus: {subjectText}.{description} No text overlay. Square composition suitable for a classroom card.";
  }

  private static string Sanitize(string input)
  {
    if (string.IsNullOrWhiteSpace(input))
      return "classroom";

    var cleaned = new string(input
      .Trim()
      .ToLowerInvariant()
      .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
      .ToArray());

    while (cleaned.Contains("--", StringComparison.Ordinal))
      cleaned = cleaned.Replace("--", "-", StringComparison.Ordinal);

    return cleaned.Trim('-');
  }
}

public sealed record ClassroomThumbnailGenerationDto(
  string ClassroomName,
  int YearLevel,
  IReadOnlyList<string> Subjects,
  string? Description,
  string? StylePreset);
