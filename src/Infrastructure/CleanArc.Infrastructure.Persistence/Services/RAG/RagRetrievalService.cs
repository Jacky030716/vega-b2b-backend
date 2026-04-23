using System.Security.Cryptography;
using System.Text;
using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Application.Contracts.Infrastructure.Rag;
using CleanArc.Application.Models.Common;
using CleanArc.Infrastructure.Persistence.Settings;
using Microsoft.Extensions.Options;

namespace CleanArc.Infrastructure.Persistence.Services.RAG;

public class RagRetrievalService(
  ITextEmbeddingService textEmbeddingService,
  IRagVectorStore ragVectorStore,
  IOptions<RagVectorStoreOptions> options)
  : IRagRetrievalService
{
  private const int MaxChunksPerSource = 40;
  private readonly ITextEmbeddingService _textEmbeddingService = textEmbeddingService;
  private readonly IRagVectorStore _ragVectorStore = ragVectorStore;
  private readonly RagVectorStoreOptions _options = options.Value;

  public async Task<OperationResult<RagRetrievalResult>> BuildAugmentedContextAsync(
    RagRetrievalRequest request,
    CancellationToken cancellationToken)
  {
    var prompt = NormalizeText(request.Prompt);
    var documentText = NormalizeText(request.DocumentText);

    if (string.IsNullOrWhiteSpace(prompt) && string.IsNullOrWhiteSpace(documentText))
      return OperationResult<RagRetrievalResult>.FailureResult("RAG retrieval requires prompt or document text.");

    var sourceEntries = BuildSourceEntries(prompt, request.DocumentName, documentText);
    var topK = Math.Max(1, _options.MaxRetrievedChunks);
    var sourceFallbackChunks = sourceEntries
      .SelectMany(entry => SplitIntoChunks(entry.SourceText)
        .Take(MaxChunksPerSource)
        .Select(chunk => new RagRetrievedChunk(entry.SourceKey, entry.SourceLabel, chunk, 0)))
      .Take(topK)
      .ToList();

    var upserts = new List<RagChunkUpsert>();

    foreach (var entry in sourceEntries)
    {
      var chunks = SplitIntoChunks(entry.SourceText)
        .Take(MaxChunksPerSource)
        .ToList();

      foreach (var chunk in chunks)
      {
        var embedding = await _textEmbeddingService.EmbedAsync(chunk, cancellationToken);
        if (!embedding.IsSuccess)
        {
          var fallbackContext = BuildAugmentedContext(sourceFallbackChunks);
          return OperationResult<RagRetrievalResult>.SuccessResult(new RagRetrievalResult(fallbackContext, sourceFallbackChunks));
        }

        var contentHash = ComputeSha256($"{entry.SourceKey}:{chunk}");
        upserts.Add(new RagChunkUpsert(
          SourceKey: entry.SourceKey,
          SourceLabel: entry.SourceLabel,
          Content: chunk,
          ContentHash: contentHash,
          Embedding: embedding.Result));
      }
    }

    if (upserts.Count > 0)
      await _ragVectorStore.UpsertChunksAsync(upserts, cancellationToken);

    var queryText = !string.IsNullOrWhiteSpace(prompt) ? prompt : documentText;
    var queryEmbedding = await _textEmbeddingService.EmbedAsync(queryText, cancellationToken);
    if (!queryEmbedding.IsSuccess)
    {
      var fallbackContext = BuildAugmentedContext(sourceFallbackChunks);
      return OperationResult<RagRetrievalResult>.SuccessResult(new RagRetrievalResult(fallbackContext, sourceFallbackChunks));
    }

    var retrievedChunks = await _ragVectorStore.QuerySimilarAsync(queryEmbedding.Result, topK, cancellationToken);

    if (retrievedChunks.Count == 0)
    {
      // Fall back to direct prompt/doc snippets when the vector store has no matches yet.
      var fallbackChunks = upserts
        .Take(topK)
        .Select(item => new RagRetrievedChunk(item.SourceKey, item.SourceLabel, item.Content, 0))
        .ToList();

      var fallbackContext = BuildAugmentedContext(fallbackChunks);
      return OperationResult<RagRetrievalResult>.SuccessResult(new RagRetrievalResult(fallbackContext, fallbackChunks));
    }

    var context = BuildAugmentedContext(retrievedChunks);
    return OperationResult<RagRetrievalResult>.SuccessResult(new RagRetrievalResult(context, retrievedChunks));
  }

  private List<RagSourceEntry> BuildSourceEntries(string prompt, string? documentName, string? documentText)
  {
    var entries = new List<RagSourceEntry>();

    if (!string.IsNullOrWhiteSpace(prompt))
    {
      entries.Add(new RagSourceEntry(
        SourceKey: $"prompt:{ComputeSha256(prompt)}",
        SourceLabel: "Teacher Prompt",
        SourceText: prompt));
    }

    if (!string.IsNullOrWhiteSpace(documentText))
    {
      var safeName = string.IsNullOrWhiteSpace(documentName) ? "syllabus" : documentName.Trim();
      entries.Add(new RagSourceEntry(
        SourceKey: $"document:{ComputeSha256($"{safeName}:{documentText}")}",
        SourceLabel: $"Document: {safeName}",
        SourceText: documentText));
    }

    return entries;
  }

  private IEnumerable<string> SplitIntoChunks(string text)
  {
    var chunkSize = Math.Max(200, _options.ChunkSize);
    var overlap = Math.Max(0, Math.Min(_options.ChunkOverlap, chunkSize - 1));
    var step = Math.Max(1, chunkSize - overlap);

    if (text.Length <= chunkSize)
    {
      yield return text;
      yield break;
    }

    for (var start = 0; start < text.Length; start += step)
    {
      var length = Math.Min(chunkSize, text.Length - start);
      var segment = text.Substring(start, length).Trim();
      if (!string.IsNullOrWhiteSpace(segment))
        yield return segment;

      if (start + length >= text.Length)
        yield break;
    }
  }

  private static string BuildAugmentedContext(IReadOnlyList<RagRetrievedChunk> chunks)
  {
    if (chunks.Count == 0)
      return string.Empty;

    var sb = new StringBuilder();
    for (var index = 0; index < chunks.Count; index += 1)
    {
      var chunk = chunks[index];
      sb.AppendLine($"[Chunk {index + 1}] {chunk.SourceLabel}");
      sb.AppendLine(chunk.Content);
      sb.AppendLine();
    }

    return sb.ToString().Trim();
  }

  private static string NormalizeText(string? raw)
  {
    if (string.IsNullOrWhiteSpace(raw))
      return string.Empty;

    return string.Join(' ', raw
      .Split(['\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries)
      .Select(part => part.Trim()))
      .Trim();
  }

  private static string ComputeSha256(string value)
  {
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
    return Convert.ToHexString(bytes);
  }

  private sealed record RagSourceEntry(string SourceKey, string SourceLabel, string SourceText);
}
