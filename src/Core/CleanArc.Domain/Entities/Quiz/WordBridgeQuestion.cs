using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Quiz;

public class WordBridgeQuestion : BaseEntity<int>
{
  public int QuizQuestionId { get; set; }
  public QuizQuestion QuizQuestion { get; set; }

  public string TargetWord { get; set; }
  public string Translation { get; set; }
  public string Difficulty { get; set; }
  public string ImageUrl { get; set; }
}
