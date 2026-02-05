using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Quiz;

public class WordBridgeAttemptAnswer : BaseEntity<int>
{
  public int QuizAttemptAnswerId { get; set; }
  public QuizAttemptAnswer QuizAttemptAnswer { get; set; }

  public bool Success { get; set; }
  public int Attempts { get; set; }
  public int TimeMs { get; set; }
  public string TargetWord { get; set; }
}
