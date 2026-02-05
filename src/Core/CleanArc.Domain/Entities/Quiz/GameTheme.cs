using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Quiz;

public class GameTheme : BaseEntity<int>
{
  public int GameConfigId { get; set; }
  public GameConfig GameConfig { get; set; }

  public string ThemeId { get; set; }
  public string Name { get; set; }
  public string Emoji { get; set; }
  public string Description { get; set; }

  public ICollection<GameThemeGradient> Gradients { get; set; } = new List<GameThemeGradient>();
  public ICollection<GameThemeItem> Items { get; set; } = new List<GameThemeItem>();
}
