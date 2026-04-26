using CleanArc.Domain.Common;
using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Domain.Entities.Classroom;
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

    // Nullable adaptive-layer metadata. Existing challenges keep these null.
    public int? StudentId { get; set; }
    public User.User? Student { get; set; }
    public int? ModuleId { get; set; }
    public SyllabusModule? Module { get; set; }
    public int? GameTemplateId { get; set; }
    public GameTemplate? GameTemplate { get; set; }
    public string? ChallengeMode { get; set; }
    public string? SourceType { get; set; }
    public string? Subject { get; set; }
    public int? CustomModuleId { get; set; }
    public CustomModule? CustomModule { get; set; }
    public string ConfigJson { get; set; } = "{}";
    public string Status { get; set; } = "assigned";
    public DateTime? AssignedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public ChallengeLifecycleState LifecycleState { get; set; } = ChallengeLifecycleState.Draft;
    public bool IsPinned { get; set; } = false;
    public double RecommendedScore { get; set; } = 0;
    public DateTime? LastActivityAt { get; set; }

    /// <summary>
    /// The classroom this challenge was created for.
    /// Null for global/platform challenges; set for teacher-created classroom challenges.
    /// Replaces the legacy ClassroomQuiz string-keyed join table.
    /// </summary>
    public int? ClassroomId { get; set; }
    public Classroom.Classroom? Classroom { get; set; }

    // Navigation properties
    public ICollection<Attempt> Attempts { get; set; } = new List<Attempt>();
    public ICollection<ChallengeProgress> Progresses { get; set; } = new List<ChallengeProgress>();
    public ICollection<ChallengeItem> ChallengeItems { get; set; } = new List<ChallengeItem>();
}
