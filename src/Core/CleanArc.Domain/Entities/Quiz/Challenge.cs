using CleanArc.Domain.Common;
using CleanArc.Domain.Entities.User;

namespace CleanArc.Domain.Entities.Quiz;

public class Challenge : BaseEntity<int>
{
    public int GameId { get; set; }
    public Game Game { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Level 1 = Beginner, 5 = Expert
    public int DifficultyLevel { get; set; }

    // JSON string containing the level data (words, pairs, sequences etc.)
    // Each game type has its own JSON schema — kept flexible for scalability.
    public string ContentData { get; set; } = string.Empty;

    // Position on the adventure map (1 = first node, 2 = second, etc.)
    public int OrderIndex { get; set; } = 0;

    // Stars earned ranges (0-3) stored per attempt, used for the map node rating display
    public int MaxStars { get; set; } = 3;

    public int? CreatedById { get; set; }
    // Optionally link to the teacher who created it
    public User.User? CreatedBy { get; set; }

    public bool IsAIGenerated { get; set; } = false;

    // Navigation properties
    public ICollection<Attempt> Attempts { get; set; } = new List<Attempt>();
}
