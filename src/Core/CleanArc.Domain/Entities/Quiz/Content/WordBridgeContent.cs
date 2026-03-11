namespace CleanArc.Domain.Entities.Quiz.Content;

/// <summary>
/// ContentData schema for the "word_bridge" game (Word Builder).
/// Serialized to jsonb in the Challenges table.
/// </summary>
public class WordBridgeContent
{
  public List<WordBridgeWord> Words { get; set; } = new();
}

public class WordBridgeWord
{
  /// <summary>The target word students must spell, e.g. "PEN".</summary>
  public string Target { get; set; } = string.Empty;

  /// <summary>Hint or translation shown to the student, e.g. "A tool used for writing".</summary>
  public string Translation { get; set; } = string.Empty;

  /// <summary>easy | medium | hard</summary>
  public string Difficulty { get; set; } = "easy";

  /// <summary>
  /// Firebase Storage relative path for a word image.
  /// Example: "quizzes/word-bridge/pen.jpg"
  /// Resolve to download URL at runtime using Firebase SDK:
  ///   getDownloadURL(ref(storage, imageRef))
  /// Leave null to use local asset fallback.
  /// </summary>
  public string? ImageRef { get; set; }
}
