using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Application.Contracts.Infrastructure.ClassroomThumbnails;
using CleanArc.Application.Models.Common;
using Mediator;
using Microsoft.Extensions.Logging;

namespace CleanArc.Application.Features.Classrooms.Commands;

internal sealed class GenerateClassroomThumbnailJobCommandHandler(
    IClassroomThumbnailImageGenerationService generationService,
    IAiAuditService aiAuditService,
    IAiUsageService aiUsageService,
    ILogger<GenerateClassroomThumbnailJobCommandHandler> logger)
    : IRequestHandler<GenerateClassroomThumbnailJobCommand, OperationResult<bool>>
{
    public async ValueTask<OperationResult<bool>> Handle(GenerateClassroomThumbnailJobCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Background execution started for classroom thumbnail generation. AuditLogId: {AuditLogId}", request.AuditLogId);

        var promptUsed = BuildPrompt(request);

        try
        {
            var generation = await generationService.GenerateAsync(
                new ClassroomThumbnailGenerationRequest(
                    request.UserId,
                    request.ClassroomName,
                    request.YearLevel,
                    request.Subjects,
                    request.Description,
                    request.ThumbnailPrompt.Trim()),
                cancellationToken);

            if (!generation.IsSuccess)
            {
                var errMsg = generation.ErrorMessage ?? "Thumbnail generation failed.";
                await aiAuditService.FailAsync(request.AuditLogId, null, new[] { errMsg }, cancellationToken);
                return OperationResult<bool>.FailureResult(errMsg);
            }

            await aiUsageService.ConsumeUsageAsync(
                request.UserId,
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

            var refreshedQuota = await aiUsageService.GetRemainingQuotaAsync(request.UserId, AiFeatureTypes.ClassroomThumbnailGeneration, cancellationToken);
            await aiAuditService.CompleteAsync(
                request.AuditLogId,
                JsonSerializer.Serialize(new { promptUsed, model = generation.Result.ModelName }),
                JsonSerializer.Serialize(new
                {
                    imageBase64 = Convert.ToBase64String(generation.Result.ImageBytes),
                    mimeType = generation.Result.MimeType,
                    promptUsed,
                    remainingQuota = refreshedQuota.Remaining
                }),
                AiValidationStatuses.Valid,
                Array.Empty<string>(),
                cancellationToken);

            logger.LogInformation("Background execution completed successfully for Classroom Thumbnail. AuditLogId: {AuditLogId}", request.AuditLogId);
            return OperationResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during classroom thumbnail background generation. AuditLogId: {AuditLogId}", request.AuditLogId);
            await aiAuditService.FailAsync(request.AuditLogId, null, new[] { ex.Message }, cancellationToken);
            return OperationResult<bool>.FailureResult($"Failed to generate classroom thumbnail: {ex.Message}");
        }
    }

    private static string BuildPrompt(GenerateClassroomThumbnailJobCommand request)
    {
        var subjectText = request.Subjects?.Length > 0 ? string.Join(", ", request.Subjects) : "classroom learning";
        var description = string.IsNullOrWhiteSpace(request.Description) ? string.Empty : $" The classroom description is: {request.Description.Trim()}";

        return
            $"Create a child-safe classroom thumbnail for a Malaysian primary school learning app. Teacher request: {request.ThumbnailPrompt.Trim()}. Classroom: {request.ClassroomName.Trim()}, Year {request.YearLevel}, subjects: {subjectText}.{description} Use a playful educational illustration style with books, learning icons, friendly colors, a 1:1 square composition, a transparent background, centered and fully visible subjects, no cropping, no truncation, no cut off edges, and no text overlay.";
    }
}
