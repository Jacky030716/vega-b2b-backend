using System.Text.Json;
using CleanArc.Application.Contracts.Infrastructure.Rag;
using CleanArc.Infrastructure.Persistence.Settings;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace CleanArc.Infrastructure.Persistence.Services.RAG;

public class SqliteRagVectorStore(IOptions<RagVectorStoreOptions> options) : IRagVectorStore
{
  private readonly RagVectorStoreOptions _options = options.Value;
  private readonly SemaphoreSlim _initLock = new(1, 1);
  private volatile bool _isInitialized;
  private string? _connectionString;

  public async Task UpsertChunksAsync(IReadOnlyList<RagChunkUpsert> chunks, CancellationToken cancellationToken)
  {
    if (chunks.Count == 0)
      return;

    await EnsureInitializedAsync(cancellationToken);

    await using var connection = new SqliteConnection(_connectionString);
    await connection.OpenAsync(cancellationToken);
    await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

    foreach (var chunk in chunks)
    {
      if (chunk.Embedding.Length == 0 || string.IsNullOrWhiteSpace(chunk.Content))
        continue;

      await using var command = connection.CreateCommand();
      command.Transaction = transaction;
      command.CommandText = """
INSERT INTO RagChunks (SourceKey, SourceLabel, Content, ContentHash, EmbeddingJson, CreatedAtUtc)
VALUES (@sourceKey, @sourceLabel, @content, @contentHash, @embeddingJson, @createdAtUtc)
ON CONFLICT(ContentHash) DO UPDATE SET
  SourceKey = excluded.SourceKey,
  SourceLabel = excluded.SourceLabel,
  Content = excluded.Content,
  EmbeddingJson = excluded.EmbeddingJson;
""";
      command.Parameters.AddWithValue("@sourceKey", chunk.SourceKey);
      command.Parameters.AddWithValue("@sourceLabel", chunk.SourceLabel);
      command.Parameters.AddWithValue("@content", chunk.Content);
      command.Parameters.AddWithValue("@contentHash", chunk.ContentHash);
      command.Parameters.AddWithValue("@embeddingJson", JsonSerializer.Serialize(chunk.Embedding));
      command.Parameters.AddWithValue("@createdAtUtc", DateTime.UtcNow.ToString("O"));
      await command.ExecuteNonQueryAsync(cancellationToken);
    }

    await transaction.CommitAsync(cancellationToken);
  }

  public async Task<IReadOnlyList<RagRetrievedChunk>> QuerySimilarAsync(
    float[] queryEmbedding,
    int topK,
    CancellationToken cancellationToken)
  {
    if (queryEmbedding.Length == 0 || topK <= 0)
      return [];

    await EnsureInitializedAsync(cancellationToken);

    await using var connection = new SqliteConnection(_connectionString);
    await connection.OpenAsync(cancellationToken);

    await using var command = connection.CreateCommand();
    command.CommandText = """
SELECT SourceKey, SourceLabel, Content, EmbeddingJson
FROM RagChunks
ORDER BY Id DESC
LIMIT @maxScanRows;
""";
    command.Parameters.AddWithValue("@maxScanRows", Math.Max(_options.MaxScanRows, topK));

    var scored = new List<RagRetrievedChunk>();
    await using var reader = await command.ExecuteReaderAsync(cancellationToken);
    while (await reader.ReadAsync(cancellationToken))
    {
      var sourceKey = reader.GetString(0);
      var sourceLabel = reader.GetString(1);
      var content = reader.GetString(2);
      var embeddingJson = reader.GetString(3);

      float[]? candidateEmbedding;
      try
      {
        candidateEmbedding = JsonSerializer.Deserialize<float[]>(embeddingJson);
      }
      catch
      {
        continue;
      }

      if (candidateEmbedding is null || candidateEmbedding.Length == 0)
        continue;

      var similarity = CosineSimilarity(queryEmbedding, candidateEmbedding);
      if (similarity <= 0)
        continue;

      scored.Add(new RagRetrievedChunk(sourceKey, sourceLabel, content, similarity));
    }

    return scored
      .OrderByDescending(item => item.Similarity)
      .Take(topK)
      .ToList();
  }

  private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
  {
    if (_isInitialized)
      return;

    await _initLock.WaitAsync(cancellationToken);
    try
    {
      if (_isInitialized)
        return;

      var databasePath = ResolveDatabasePath(_options.DatabasePath);
      var databaseDirectory = Path.GetDirectoryName(databasePath);
      if (!string.IsNullOrWhiteSpace(databaseDirectory))
      {
        Directory.CreateDirectory(databaseDirectory);
      }

      _connectionString = $"Data Source={databasePath}";
      await using var connection = new SqliteConnection(_connectionString);
      await connection.OpenAsync(cancellationToken);

      await using var command = connection.CreateCommand();
      command.CommandText = """
CREATE TABLE IF NOT EXISTS RagChunks (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  SourceKey TEXT NOT NULL,
  SourceLabel TEXT NOT NULL,
  Content TEXT NOT NULL,
  ContentHash TEXT NOT NULL UNIQUE,
  EmbeddingJson TEXT NOT NULL,
  CreatedAtUtc TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_RagChunks_CreatedAtUtc ON RagChunks(CreatedAtUtc DESC);
""";
      await command.ExecuteNonQueryAsync(cancellationToken);

      _isInitialized = true;
    }
    finally
    {
      _initLock.Release();
    }
  }

  private static string ResolveDatabasePath(string configuredPath)
  {
    if (Path.IsPathRooted(configuredPath))
      return configuredPath;

    return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configuredPath));
  }

  private static double CosineSimilarity(float[] left, float[] right)
  {
    var length = Math.Min(left.Length, right.Length);
    if (length == 0)
      return 0;

    double dot = 0;
    double leftNorm = 0;
    double rightNorm = 0;

    for (var i = 0; i < length; i += 1)
    {
      dot += left[i] * right[i];
      leftNorm += left[i] * left[i];
      rightNorm += right[i] * right[i];
    }

    if (leftNorm <= 0 || rightNorm <= 0)
      return 0;

    return dot / (Math.Sqrt(leftNorm) * Math.Sqrt(rightNorm));
  }
}
