using CleanArc.Domain.Common;
using CleanArc.Domain.Entities.Quiz;

namespace CleanArc.Domain.Entities.Adaptive;

public class GameTemplate : BaseEntity<int>
{
    public string Code { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool SupportsAdaptiveDifficulty { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Challenge> Challenges { get; set; } = new List<Challenge>();
}
