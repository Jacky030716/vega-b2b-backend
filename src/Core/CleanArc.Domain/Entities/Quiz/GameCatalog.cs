using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Quiz;

public class GameCatalog : BaseEntity<int>
{
  public string Key { get; set; }
  public string Label { get; set; }
  public string QuestionType { get; set; }
}
