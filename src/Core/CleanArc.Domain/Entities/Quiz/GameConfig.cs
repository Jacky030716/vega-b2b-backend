using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Quiz;

public class GameConfig : BaseEntity<int>
{
  public string GameType { get; set; }
  public string DefaultAgeGroup { get; set; }
  public string DefaultThemeId { get; set; }
  public string DefaultDifficulty { get; set; }
  public int DefaultRounds { get; set; }
  public int DefaultStartingDifficulty { get; set; }

  public ICollection<GameTheme> Themes { get; set; } = new List<GameTheme>();
  public ICollection<GameDifficulty> DifficultyLevels { get; set; } = new List<GameDifficulty>();
}
