using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Quiz;

public class GameThemeGradient : BaseEntity<int>
{
  public int GameThemeId { get; set; }
  public GameTheme GameTheme { get; set; }

  public int Order { get; set; }
  public string Color { get; set; }
}
