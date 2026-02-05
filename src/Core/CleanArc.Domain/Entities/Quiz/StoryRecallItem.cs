using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Quiz;

public class StoryRecallItem : BaseEntity<int>
{
  public int StoryRecallQuestionId { get; set; }
  public StoryRecallQuestion StoryRecallQuestion { get; set; }

  public string RecallQuestionId { get; set; }
  public string QuestionText { get; set; }
  public int CorrectAnswer { get; set; }

  public ICollection<StoryRecallOption> Options { get; set; } = new List<StoryRecallOption>();
}
