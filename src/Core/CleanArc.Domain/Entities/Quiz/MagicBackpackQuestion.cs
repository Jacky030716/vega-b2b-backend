using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Quiz;

public class MagicBackpackQuestion : BaseEntity<int>
{
  public int QuizQuestionId { get; set; }
  public QuizQuestion QuizQuestion { get; set; }

  public string Theme { get; set; }
  public string AgeGroup { get; set; }

  public ICollection<MagicBackpackItem> Items { get; set; } = new List<MagicBackpackItem>();
  public ICollection<MagicBackpackSequence> Sequence { get; set; } = new List<MagicBackpackSequence>();
}
