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

public static class AiUseCases
{
  public const string CustomChallengeExtraction = "CUSTOM_CHALLENGE_EXTRACTION";
  public const string ModuleChallengePlanning = "MODULE_CHALLENGE_PLANNING";
  public const string SpellCatcherConfig = "SPELL_CATCHER_CONFIG";
  public const string SyllableSushiConfig = "SYLLABLE_SUSHI_CONFIG";
  public const string VoiceBridgeConfig = "VOICE_BRIDGE_CONFIG";
  public const string AdminAuditor = "ADMIN_AUDITOR";
  public const string StickerGeneration = "STICKER_GENERATION";
}

public static class AiGenerationStatuses
{
  public const string None = "NONE";
  public const string AiAssisted = "AI_ASSISTED";
  public const string AiGenerated = "AI_GENERATED";
  public const string FailedFallback = "FAILED_FALLBACK";
}

public static class AiValidationStatuses
{
  public const string Pending = "PENDING";
  public const string Valid = "VALID";
  public const string Invalid = "INVALID";
  public const string Failed = "FAILED";
}

public record AiPromptDefinition(
  string UseCase,
  string Version,
  string Description,
  string SystemInstruction,
  string OutputSchemaName);

public interface IAiPromptRegistry
{
  AiPromptDefinition Get(string useCase, string? variant = null);
}

public record AiAuditStartRequest(
  string UseCase,
  string Provider,
  string? ModelName,
  string PromptVersion,
  string InputPayloadJson,
  int? RelatedUserId = null,
  int? RelatedClassroomId = null,
  int? RelatedModuleId = null,
  int? RelatedChallengeId = null);

public interface IAiAuditService
{
  Task<int> StartAsync(AiAuditStartRequest request, CancellationToken cancellationToken);
  Task CompleteAsync(int auditLogId, string rawOutputJson, string parsedOutputJson, string validationStatus, IReadOnlyList<string> validationErrors, CancellationToken cancellationToken);
  Task FailAsync(int auditLogId, string? rawOutputJson, IReadOnlyList<string> validationErrors, CancellationToken cancellationToken);
  Task AttachChallengeAsync(int auditLogId, int challengeId, CancellationToken cancellationToken);
}

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
  string AugmentedContext,
  int? RelatedUserId = null,
  int? RelatedClassroomId = null);

public record CustomVocabularyGenerationResult(
  string Title,
  string Description,
  string DraftSchema,
  string DraftPayload,
  string PlayableContentData,
  int? AiAuditLogId = null);

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
  string? WeakSkill,
  int? RelatedUserId = null,
  int? RelatedClassroomId = null);

public record ModuleChallengePlanResult(
  IReadOnlyList<string> SelectedWords,
  string RecommendedGameType,
  int DifficultyLevel,
  string Reason,
  string FocusType,
  int? AiAuditLogId = null);

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
