using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Application.Contracts.Infrastructure.Documents;
using CleanArc.Application.Contracts.Infrastructure.Rag;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;
using Microsoft.Extensions.Logging;

namespace CleanArc.Application.Features.Games.Commands;

internal sealed class GenerateAiChallengeDraftJobCommandHandler(
    IUnitOfWork unitOfWork,
    IChallengeDocumentExtractor challengeDocumentExtractor,
    IRagRetrievalService ragRetrievalService,
    IChallengeAiPipelineService challengeAiPipelineService,
    IAiAuditService aiAuditService,
    ILogger<GenerateAiChallengeDraftJobCommandHandler> logger)
    : IRequestHandler<GenerateAiChallengeDraftJobCommand, OperationResult<bool>>
{
    public async ValueTask<OperationResult<bool>> Handle(
        GenerateAiChallengeDraftJobCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Background execution started for challenge draft. Classroom: {ClassroomId}, AuditLogId: {AuditLogId}", 
            request.ClassroomId, request.AuditLogId);

        try
        {
            var classroom = await unitOfWork.ClassroomRepository.GetClassroomByIdAsync(request.ClassroomId);
            if (classroom is null)
            {
                await aiAuditService.FailAsync(request.AuditLogId, null, new[] { "Classroom not found." }, cancellationToken);
                return OperationResult<bool>.FailureResult("Classroom not found.");
            }

            var promptText = request.Prompt?.Trim() ?? string.Empty;
            string? extractedDocumentText = null;
            string? sourceDocumentName = null;

            if (request.SourceDocument is not null)
            {
                sourceDocumentName = request.SourceDocument.FileName?.Trim();
                var extraction = await challengeDocumentExtractor.ExtractTextAsync(request.SourceDocument, cancellationToken);
                if (!extraction.IsSuccess)
                {
                    var errMsg = extraction.ErrorMessage ?? "Unable to read syllabus document.";
                    await aiAuditService.FailAsync(request.AuditLogId, null, new[] { errMsg }, cancellationToken);
                    return OperationResult<bool>.FailureResult(errMsg);
                }

                extractedDocumentText = extraction.Result?.Trim();
            }

            if (string.IsNullOrWhiteSpace(promptText) && string.IsNullOrWhiteSpace(extractedDocumentText))
            {
                var errMsg = "Provide a prompt or upload a syllabus document.";
                await aiAuditService.FailAsync(request.AuditLogId, null, new[] { errMsg }, cancellationToken);
                return OperationResult<bool>.FailureResult(errMsg);
            }

            var retrieval = await ragRetrievalService.BuildAugmentedContextAsync(
                new RagRetrievalRequest(promptText, sourceDocumentName, extractedDocumentText),
                cancellationToken);

            if (!retrieval.IsSuccess)
            {
                var errMsg = retrieval.ErrorMessage ?? "RAG retrieval failed.";
                await aiAuditService.FailAsync(request.AuditLogId, null, new[] { errMsg }, cancellationToken);
                return OperationResult<bool>.FailureResult(errMsg);
            }

            var draftResult = await challengeAiPipelineService.GenerateStructuredVocabularyFromInputAsync(
                new CustomVocabularyGenerationRequest(
                    request.GameKey, 
                    promptText, 
                    retrieval.Result.AugmentedContext, 
                    request.UserId, 
                    request.ClassroomId,
                    ExistingAuditLogId: request.AuditLogId),
                cancellationToken);

            if (!draftResult.IsSuccess)
            {
                var errMsg = draftResult.ErrorMessage ?? "AI generation failed.";
                // Custom challenge draft pipeline completes audit log itself, but we check if we need to fail it
                return OperationResult<bool>.FailureResult(errMsg);
            }

            // Enrich the completed audit log with RAG chunks and document metadata
            var enriched = new
            {
                title = draftResult.Result.Title,
                description = draftResult.Result.Description,
                draftSchema = draftResult.Result.DraftSchema,
                draftPayload = draftResult.Result.DraftPayload,
                playableContentData = draftResult.Result.PlayableContentData,
                aiAuditLogId = request.AuditLogId,
                sourceDocumentName = sourceDocumentName,
                retrievedChunks = retrieval.Result.RetrievedChunks
                    .Select(chunk => new { sourceLabel = chunk.SourceLabel, content = chunk.Content, similarity = Math.Round(chunk.Similarity, 4) })
                    .ToList()
            };

            await aiAuditService.CompleteAsync(
                request.AuditLogId,
                draftResult.Result.PlayableContentData,
                JsonSerializer.Serialize(enriched),
                AiValidationStatuses.Valid,
                Array.Empty<string>(),
                cancellationToken);

            logger.LogInformation("Background execution completed successfully for Challenge Draft. AuditLogId: {AuditLogId}", request.AuditLogId);
            return OperationResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during challenge draft background generation. AuditLogId: {AuditLogId}", request.AuditLogId);
            await aiAuditService.FailAsync(request.AuditLogId, null, new[] { ex.Message }, cancellationToken);
            return OperationResult<bool>.FailureResult($"Failed to generate challenge draft: {ex.Message}");
        }
    }
}

