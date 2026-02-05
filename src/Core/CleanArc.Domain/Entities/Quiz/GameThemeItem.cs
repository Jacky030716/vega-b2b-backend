using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Quiz;

public class GameThemeItem : BaseEntity<int>
{
  public int GameThemeId { get; set; }
  public GameTheme GameTheme { get; set; }

  public string ItemId { get; set; }
  public string Name { get; set; }
  public string Emoji { get; set; }
}
