using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Progression;

public class Level : BaseEntity<int>
{
  public int LevelNumber { get; set; }
  public string Name { get; set; }
  public int RequiredXP { get; set; }
  public string UnlocksGameType { get; set; } // nullable — what game type this level unlocks
}
