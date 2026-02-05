using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Quiz;

public class StoryRecallAttemptSelection : BaseEntity<int>
{
  public int StoryRecallAttemptAnswerId { get; set; }
  public StoryRecallAttemptAnswer StoryRecallAttemptAnswer { get; set; }

  public string RecallQuestionId { get; set; }
  public int SelectedOption { get; set; }
}
