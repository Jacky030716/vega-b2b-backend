using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Quiz;

public class Game : BaseEntity<int>
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SkillsTaught { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<Challenge> Challenges { get; set; } = new List<Challenge>();
}
