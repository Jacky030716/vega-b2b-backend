using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Quiz;

public class GameEnemy : BaseEntity<int>
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ImageRef { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
