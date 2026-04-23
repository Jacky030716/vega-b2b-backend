namespace CleanArc.Infrastructure.Persistence.Settings;

public class RagVectorStoreOptions
{
  public const string SectionName = nameof(RagVectorStoreOptions);

  public string DatabasePath { get; set; } = "rag/challenge-rag.db";
  public int ChunkSize { get; set; } = 600;
  public int ChunkOverlap { get; set; } = 120;
  public int MaxRetrievedChunks { get; set; } = 4;
  public int MaxScanRows { get; set; } = 5000;
}
