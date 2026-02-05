using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Quiz;

public class Quiz : BaseEntity<int>
{
  public string QuizId { get; set; }
  public string Title { get; set; }
  public string Description { get; set; }
  public string Theme { get; set; }
  public string Type { get; set; }
  public string Difficulty { get; set; }
  public int EstimatedTime { get; set; }
  public int TotalPoints { get; set; }
  public string CreatedAt { get; set; }
  public string ImageUrl { get; set; }
  public string Category { get; set; }

  public ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
}
