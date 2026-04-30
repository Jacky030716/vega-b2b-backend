using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Application.Contracts.Infrastructure.Documents;
using CleanArc.Application.Contracts.Infrastructure.Rag;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

#nullable enable

namespace CleanArc.Application.Features.Games.Commands;

internal sealed class GenerateAiChallengeDraftCommandHandler(
  IUnitOfWork unitOfWork,
  IChallengeDocumentExtractor challengeDocumentExtractor,
  IRagRetrievalService ragRetrievalService,
  IChallengeAiPipelineService challengeAiPipelineService)
  : IRequestHandler<GenerateAiChallengeDraftCommand, OperationResult<GenerateAiChallengeDraftDto>>
{
  public async ValueTask<OperationResult<GenerateAiChallengeDraftDto>> Handle(
    GenerateAiChallengeDraftCommand request,
    CancellationToken cancellationToken)
  {
    if (!IsSupportedGameKey(request.GameKey))
      return OperationResult<GenerateAiChallengeDraftDto>.FailureResult("Unsupported game key for AI generation.");

    var classroom = await unitOfWork.ClassroomRepository.GetClassroomByIdAsync(request.ClassroomId);
    if (classroom is null || !classroom.IsActive)
      return OperationResult<GenerateAiChallengeDraftDto>.NotFoundResult("Classroom not found.");

    if (classroom.TeacherId != request.UserId)
      return OperationResult<GenerateAiChallengeDraftDto>.UnauthorizedResult("You are not authorized to generate challenges for this classroom.");

    var prompt = request.Prompt?.Trim() ?? string.Empty;
    string? extractedDocumentText = null;
    string? sourceDocumentName = null;

    if (request.SourceDocument is not null)
    {
      sourceDocumentName = request.SourceDocument.FileName?.Trim();
      var extraction = await challengeDocumentExtractor.ExtractTextAsync(request.SourceDocument, cancellationToken);
      if (!extraction.IsSuccess)
        return OperationResult<GenerateAiChallengeDraftDto>.FailureResult(extraction.ErrorMessage ?? "Unable to read syllabus document.");

      extractedDocumentText = extraction.Result?.Trim();
    }

    if (string.IsNullOrWhiteSpace(prompt) && string.IsNullOrWhiteSpace(extractedDocumentText))
      return OperationResult<GenerateAiChallengeDraftDto>.FailureResult("Provide a prompt or upload a syllabus document.");

    var retrieval = await ragRetrievalService.BuildAugmentedContextAsync(
      new RagRetrievalRequest(prompt, sourceDocumentName, extractedDocumentText),
      cancellationToken);

    if (!retrieval.IsSuccess)
      return OperationResult<GenerateAiChallengeDraftDto>.FailureResult(retrieval.ErrorMessage ?? "RAG retrieval failed.");

    var draftResult = await challengeAiPipelineService.GenerateStructuredVocabularyFromInputAsync(
      new CustomVocabularyGenerationRequest(request.GameKey, prompt, retrieval.Result.AugmentedContext, request.UserId, request.ClassroomId),
      cancellationToken);

    if (!draftResult.IsSuccess)
      return OperationResult<GenerateAiChallengeDraftDto>.FailureResult(draftResult.ErrorMessage ?? "AI generation failed.");

    var draft = draftResult.Result;
    var response = new GenerateAiChallengeDraftDto(
      draft.Title,
      draft.Description,
      draft.DraftSchema,
      draft.DraftPayload,
      draft.PlayableContentData,
      draft.AiAuditLogId,
      sourceDocumentName,
      retrieval.Result.RetrievedChunks
        .Select(chunk => new RagChunkDto(chunk.SourceLabel, chunk.Content, Math.Round(chunk.Similarity, 4)))
        .ToList());

    return OperationResult<GenerateAiChallengeDraftDto>.SuccessResult(response);
  }

  private static bool IsSupportedGameKey(string gameKey)
    => gameKey is "magic_backpack" or "word_pair" or "word_bridge";
}
