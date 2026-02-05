using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Quiz;

public class QuizAttemptAnswer : BaseEntity<int>
{
  public int QuizAttemptId { get; set; }
  public QuizAttempt QuizAttempt { get; set; }

  public string QuestionId { get; set; }
  public string QuestionType { get; set; }
  public bool? IsCorrect { get; set; }
  public int? TimeSpentSec { get; set; }

  public MagicBackpackAttemptAnswer MagicBackpackAttemptAnswer { get; set; }
  public WordBridgeAttemptAnswer WordBridgeAttemptAnswer { get; set; }
  public StoryRecallAttemptAnswer StoryRecallAttemptAnswer { get; set; }
}
