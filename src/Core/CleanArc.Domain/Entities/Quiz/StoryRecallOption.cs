using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Quiz;

public class StoryRecallOption : BaseEntity<int>
{
  public int StoryRecallItemId { get; set; }
  public StoryRecallItem StoryRecallItem { get; set; }

  public int Order { get; set; }
  public string Text { get; set; }
}
