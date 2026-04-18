using CleanArc.Domain.Common;
using CleanArc.Domain.Entities.Classroom;

namespace CleanArc.Domain.Entities.Quiz;

/// <summary>
/// Aggregated per-student, per-challenge summary used for leaderboards and diagnostics.
/// One row per (UserId, ChallengeId, ClassroomId) — upserted on every attempt completion.
/// </summary>
public class ChallengeProgress : BaseEntity<int>
{
    public int UserId { get; set; }
    public User.User User { get; set; } = null!;

    public int ChallengeId { get; set; }
    public Challenge Challenge { get; set; } = null!;

    /// <summary>Scopes this progress record to a specific classroom for leaderboard queries.</summary>
    public int ClassroomId { get; set; }
    public Classroom.Classroom Classroom { get; set; } = null!;

    // ── Attempt tracking ────────────────────────────────────────────────────

    /// <summary>Total number of attempts made (including abandoned ones).</summary>
    public int AttemptCount { get; set; } = 0;

    /// <summary>True once the student has completed the challenge at least once.</summary>
    public bool HasCompleted { get; set; } = false;

    // ── Best-run performance metrics ────────────────────────────────────────

    /// <summary>Best raw score across all completed attempts.</summary>
    public int BestScore { get; set; } = 0;

    /// <summary>Best star rating (0–3) across all completed attempts.</summary>
    public int BestStars { get; set; } = 0;

    /// <summary>Best accuracy percentage (0–100) parsed from AttemptData JSON. Null if never recorded.</summary>
    public decimal? BestAccuracy { get; set; }

    /// <summary>Fastest successful completion time in seconds. Null if never recorded.</summary>
    public decimal? BestDurationSeconds { get; set; }

    /// <summary>Total XP accrued from this challenge (only first-completion XP counts).</summary>
    public int TotalXpEarned { get; set; } = 0;

    // ── Timestamps ─────────────────────────────────────────────────────────

    /// <summary>Timestamp of the student's most recent attempt.</summary>
    public DateTime LastAttemptAt { get; set; } = DateTime.UtcNow;

    /// <summary>Timestamp of the student's first successful completion. Null if never completed.</summary>
    public DateTime? FirstCompletedAt { get; set; }
}
