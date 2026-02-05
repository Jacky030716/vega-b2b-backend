using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Quiz;

public class GameDifficulty : BaseEntity<int>
{
  public int GameConfigId { get; set; }
  public GameConfig GameConfig { get; set; }

  public int Level { get; set; }
  public string Name { get; set; }
  public int SequenceLength { get; set; }
  public int Speed { get; set; }
  public bool GhostMode { get; set; }
  public string Description { get; set; }
}
