using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Quiz;

public class MagicBackpackItem : BaseEntity<int>
{
  public int MagicBackpackQuestionId { get; set; }
  public MagicBackpackQuestion MagicBackpackQuestion { get; set; }

  public string ItemId { get; set; }
  public string Name { get; set; }
  public string Emoji { get; set; }
}
