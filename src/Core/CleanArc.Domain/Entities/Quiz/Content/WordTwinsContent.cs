namespace CleanArc.Domain.Entities.Quiz.Content;

/// <summary>
/// ContentData schema for the "word_pair" game (Word Twins / memory card matching).
/// Serialized to jsonb in the Challenges table.
/// </summary>
public class WordTwinsContent
{
  public List<WordTwinsPair> Pairs { get; set; } = new();
}

public class WordTwinsPair
{
  /// <summary>The word displayed on the card, e.g. "Book".</summary>
  public string Word { get; set; } = string.Empty;

  /// <summary>Optional translation hint.</summary>
  public string? Translation { get; set; }

  /// <summary>
  /// Firebase Storage relative path for the card image.
  /// Example: "quizzes/word-twins/book.jpg"
  /// Resolve at runtime on the frontend:
  ///   getDownloadURL(ref(storage, imageRef))
  /// </summary>
  public string? ImageRef { get; set; }

  /// <summary>
  /// Local bundled asset key used as offline fallback
  /// when ImageRef is null or network is unavailable.
  /// Matches the imageResolver.ts key map on the frontend.
  /// </summary>
  public string? ImageKey { get; set; }
}
