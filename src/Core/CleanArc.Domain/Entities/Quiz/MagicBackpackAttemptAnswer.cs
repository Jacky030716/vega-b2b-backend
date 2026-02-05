using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Quiz;

public class MagicBackpackAttemptAnswer : BaseEntity<int>
{
  public int QuizAttemptAnswerId { get; set; }
  public QuizAttemptAnswer QuizAttemptAnswer { get; set; }

  public bool? IsSuccess { get; set; }

  public ICollection<MagicBackpackAttemptSelection> Selections { get; set; } = new List<MagicBackpackAttemptSelection>();
}
