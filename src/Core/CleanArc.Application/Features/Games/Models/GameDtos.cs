namespace CleanArc.Application.Features.Games.Models;

public class GameCatalogItemDto
{
  public string Key { get; set; }
  public string Label { get; set; }
  public string QuestionType { get; set; }
}

public class GameConfigDto
{
  public string GameType { get; set; }
  public List<GameThemeDto> Themes { get; set; } = new();
  public List<GameDifficultyDto> DifficultyLevels { get; set; } = new();
  public GameDefaultsDto Defaults { get; set; } = new();
}

public class GameThemeDto
{
  public string Id { get; set; }
  public string Name { get; set; }
  public string Emoji { get; set; }
  public string Description { get; set; }
  public List<string> BgGradient { get; set; } = new();
  public List<GameItemDto> Items { get; set; } = new();
}

public class GameItemDto
{
  public string Id { get; set; }
  public string Name { get; set; }
  public string Emoji { get; set; }
}

public class GameDifficultyDto
{
  public int Level { get; set; }
  public string Name { get; set; }
  public int SequenceLength { get; set; }
  public int Speed { get; set; }
  public bool GhostMode { get; set; }
  public string Description { get; set; }
}

public class GameDefaultsDto
{
  public string AgeGroup { get; set; }
  public string ThemeId { get; set; }
  public string Difficulty { get; set; }
  public int Rounds { get; set; }
  public int StartingDifficulty { get; set; }
}
