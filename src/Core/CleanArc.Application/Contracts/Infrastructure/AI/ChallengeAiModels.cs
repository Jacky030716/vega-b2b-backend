using CleanArc.Application.Models.Common;

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
