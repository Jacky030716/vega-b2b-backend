using CleanArc.Application.Models.Common;

namespace CleanArc.Application.Contracts.Infrastructure.Rag;

/// <summary>
/// Input for retrieval-augmented context construction.
/// </summary>
public record RagRetrievalRequest(string Prompt, string? DocumentName, string? DocumentText);

/// <summary>
/// A retrieved chunk and its semantic similarity score.
/// </summary>
public record RagRetrievedChunk(string SourceKey, string SourceLabel, string Content, double Similarity);

/// <summary>
/// Retrieval output used for prompt augmentation.
/// </summary>
public record RagRetrievalResult(string AugmentedContext, IReadOnlyList<RagRetrievedChunk> RetrievedChunks);

/// <summary>
/// Chunk payload persisted to the local vector store.
/// </summary>
public record RagChunkUpsert(string SourceKey, string SourceLabel, string Content, string ContentHash, float[] Embedding);

/// <summary>
/// Local vector store abstraction for upserting and similarity searching chunks.
/// </summary>
public interface IRagVectorStore
{
  /// <summary>
  /// Upserts one or more chunks into the vector store.
  /// </summary>
  /// <param name="chunks">Chunks with embeddings to persist.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  Task UpsertChunksAsync(IReadOnlyList<RagChunkUpsert> chunks, CancellationToken cancellationToken);

  /// <summary>
  /// Returns the most similar chunks for the given embedding.
  /// </summary>
  /// <param name="queryEmbedding">Query embedding vector.</param>
  /// <param name="topK">Maximum number of chunks to return.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Ranked chunk matches by cosine similarity.</returns>
  Task<IReadOnlyList<RagRetrievedChunk>> QuerySimilarAsync(
    float[] queryEmbedding,
    int topK,
    CancellationToken cancellationToken);
}

/// <summary>
/// Service that builds retrieval context from local vectorized chunks.
/// </summary>
public interface IRagRetrievalService
{
  /// <summary>
  /// Builds augmented context for challenge generation from prompt/document sources.
  /// </summary>
  /// <param name="request">Retrieval request payload.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Augmented context and retrieved chunks wrapped in an operation result.</returns>
  Task<OperationResult<RagRetrievalResult>> BuildAugmentedContextAsync(
    RagRetrievalRequest request,
    CancellationToken cancellationToken);
}
