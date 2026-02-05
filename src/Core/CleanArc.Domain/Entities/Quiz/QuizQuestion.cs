using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Quiz;

public class QuizQuestion : BaseEntity<int>
{
  public string QuestionId { get; set; }
  public string Type { get; set; }
  public string QuestionText { get; set; }
  public int Points { get; set; }
  public string Explanation { get; set; }

  public int QuizRefId { get; set; }
  public Quiz Quiz { get; set; }

  public MagicBackpackQuestion MagicBackpackQuestion { get; set; }
  public WordBridgeQuestion WordBridgeQuestion { get; set; }
  public StoryRecallQuestion StoryRecallQuestion { get; set; }
}
