using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Quiz;

public class QuizAttempt : BaseEntity<int>
{
  public string AttemptId { get; set; }
  public string QuizId { get; set; }
  public int UserId { get; set; }
  public DateTime StartedAt { get; set; }
  public DateTime? CompletedAt { get; set; }
  public int? TotalTimeSec { get; set; }
  public string Mode { get; set; }
  public string ClientVersion { get; set; }

  public ICollection<QuizAttemptAnswer> Answers { get; set; } = new List<QuizAttemptAnswer>();
}
