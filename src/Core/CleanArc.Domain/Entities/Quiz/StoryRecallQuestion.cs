using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Quiz;

public class StoryRecallQuestion : BaseEntity<int>
{
  public int QuizQuestionId { get; set; }
  public QuizQuestion QuizQuestion { get; set; }

  public string Theme { get; set; }
  public string StoryAudioUrl { get; set; }
  public string StoryText { get; set; }

  public ICollection<StoryRecallItem> RecallQuestions { get; set; } = new List<StoryRecallItem>();
}
