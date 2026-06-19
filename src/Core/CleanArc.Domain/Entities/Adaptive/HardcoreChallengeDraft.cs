using System;
using CleanArc.Domain.Common;
using CleanArc.Domain.Entities.Quiz;

namespace CleanArc.Domain.Entities.Adaptive;

public class HardcoreChallengeDraft : BaseEntity<int>
{
    public int StudentId { get; set; }
    public User.User Student { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string GameType { get; set; } = string.Empty; // SPELL_CATCHER, SYLLABLE_SUSHI, VOICE_BRIDGE, SPELLING_TEST
    public int DifficultyLevel { get; set; }
    public string TargetWordsJson { get; set; } = "[]";
    public string ContentData { get; set; } = string.Empty; // Pre-generated game config / spec data

    // Configurables
    public int RewardXp { get; set; }
    public int RewardDiamonds { get; set; }
    public bool MascotEligibility { get; set; }
    public string? MascotName { get; set; }
    public string? BadgeCode { get; set; }

    // Lifecycle
    public string Status { get; set; } = "PENDING"; // PENDING, ACCEPTED, DECLINED, COMPLETED, EXPIRED
    public DateTime ExpiryAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Auditing / Traceability
    public string TriggeringMetricsJson { get; set; } = "{}";
    public string DecisionReason { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }

    // Linked Playable Session Instances
    public int? LinkedChallengeId { get; set; }
    public Challenge? LinkedChallenge { get; set; }

    public int? LinkedSpellingTestId { get; set; }
    public SpellingTest? LinkedSpellingTest { get; set; }
}
