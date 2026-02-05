using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Quiz;

public class StoryRecallAttemptAnswer : BaseEntity<int>
{
  public int QuizAttemptAnswerId { get; set; }
  public QuizAttemptAnswer QuizAttemptAnswer { get; set; }

  public string Phase { get; set; }
  public int Score { get; set; }

  public ICollection<StoryRecallAttemptSelection> Selections { get; set; } = new List<StoryRecallAttemptSelection>();
}
