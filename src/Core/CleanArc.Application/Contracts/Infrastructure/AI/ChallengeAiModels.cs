using CleanArc.Application.Models.Common;
using CleanArc.Application.Contracts.Adaptive;

#nullable enable

namespace CleanArc.Application.Contracts.Infrastructure.AI;

/// <summary>
/// Input payload for a JSON-only challenge draft generation call.
/// </summary>
public record ChallengeGenerationRequest(
  string Model,
  string SystemPrompt,
  string UserPrompt,
  double Temperature,
  bool JsonMode);

/// <summary>
/// Output payload from the AI orchestrator generation call.
/// </summary>
public record ChallengeGenerationResult(string RawResponse);

/// <summary>
/// Provides challenge draft inference against the configured cloud/local generation backend.
/// </summary>
public interface IAiGenerationService
{
  /// <summary>
  /// Generates a JSON response for challenge draft synthesis.
  /// </summary>
  /// <param name="request">Generation request metadata and prompts.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Raw JSON text wrapped in an operation result.</returns>
  Task<OperationResult<ChallengeGenerationResult>> GenerateJsonAsync(
    ChallengeGenerationRequest request,
    CancellationToken cancellationToken);
}

public record CustomVocabularyGenerationRequest(
  string GameKey,
  string Prompt,
  string AugmentedContext);

public record CustomVocabularyGenerationResult(
  string Title,
  string Description,
  string DraftSchema,
  string DraftPayload,
  string PlayableContentData);

public record ModuleChallengeAiItem(
  int VocabularyItemId,
  string Word,
  string? BmText,
  string? EnText,
  string? ZhText,
  string? SyllablesJson,
  string? SyllableText,
  string? ItemType,
  int DifficultyLevel,
  string? MeaningText,
  string? ExampleSentence);

public record ModuleChallengePlanRequest(
  int ModuleId,
  string ModuleTitle,
  string Subject,
  int YearLevel,
  string RequestedGameType,
  string Mode,
  IReadOnlyList<ModuleChallengeAiItem> Items,
  IReadOnlyList<string> WeakWords,
  string? WeakSkill);

public record ModuleChallengePlanResult(
  IReadOnlyList<string> SelectedWords,
  string RecommendedGameType,
  int DifficultyLevel,
  string Reason,
  string FocusType);

public record GameConfigGenerationRequest(
  int ModuleId,
  string ModuleTitle,
  string Subject,
  int? ClassroomId,
  string Mode,
  string SourceType,
  string GameType,
  int DifficultyLevel,
  IReadOnlyList<AdaptiveChallengeItemDto> Words);

public interface IChallengeAiPipelineService
{
  Task<OperationResult<CustomVocabularyGenerationResult>> GenerateStructuredVocabularyFromInputAsync(
    CustomVocabularyGenerationRequest request,
    CancellationToken cancellationToken);

  Task<OperationResult<ModuleChallengePlanResult>> GenerateModuleChallengePlanAsync(
    ModuleChallengePlanRequest request,
    CancellationToken cancellationToken);

  Task<OperationResult<GeneratedAdaptiveChallengePreviewDto>> GenerateGameConfigAsync(
    GameConfigGenerationRequest request,
    CancellationToken cancellationToken);
}

/// <summary>
/// Provides challenge draft inference against the configured LLM backend.
/// </summary>
public interface IChallengeAiOrchestrator
{
  /// <summary>
  /// Generates a JSON response for challenge draft synthesis.
  /// </summary>
  /// <param name="request">Generation request metadata and prompts.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Raw JSON text wrapped in an operation result.</returns>
  Task<OperationResult<ChallengeGenerationResult>> GenerateJsonAsync(
    ChallengeGenerationRequest request,
    CancellationToken cancellationToken);
}

/// <summary>
/// Provides text embedding vectors for retrieval workflows.
/// </summary>
public interface ITextEmbeddingService
{
  /// <summary>
  /// Computes an embedding vector for the provided text.
  /// </summary>
  /// <param name="text">Input text to embed.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Embedding vector wrapped in an operation result.</returns>
  Task<OperationResult<float[]>> EmbedAsync(string text, CancellationToken cancellationToken);
}
