using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Quiz;

public class MagicBackpackAttemptSelection : BaseEntity<int>
{
  public int MagicBackpackAttemptAnswerId { get; set; }
  public MagicBackpackAttemptAnswer MagicBackpackAttemptAnswer { get; set; }

  public string ItemId { get; set; }
  public int Order { get; set; }
}
