#nullable enable

using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Mission;

/// <summary>
/// Represents a mission or challenge that students can complete to earn rewards.
/// Missions are independent from achievements but can trigger achievement events.
/// </summary>
public class Mission : BaseEntity<int>
{
  public string Title { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;

  /// <summary>
  /// Mission type: "daily", "weekly", "story", "seasonal", custom.
  /// Enables filtering and scheduling different mission types.
  /// </summary>
  public string Type { get; set; } = "daily";

  /// <summary>
  /// Category for UI grouping: "quiz", "social", "engagement", etc.
  /// </summary>
  public string Category { get; set; } = "quest";

  /// <summary>
  /// The target value for mission completion.
  /// For "complete 5 quizzes", this would be 5.
  /// </summary>
  public int TargetValue { get; set; } = 1;

  /// <summary>
  /// Human-readable description of the objective.
  /// E.g., "Complete 5 quizzes with 80% accuracy"
  /// </summary>
  public string Objective { get; set; } = string.Empty;

  /// <summary>
  /// Achievement event type that triggers progress on this mission.
  /// E.g., "QuizCompleted" or "MissionClaimed"
  /// </summary>
  public string TriggerEventType { get; set; } = string.Empty;

  /// <summary>
  /// Optional JSON filter conditions for the trigger event.
  /// E.g., for QuizCompleted: { "minimumScore": 80 }
  /// </summary>
  public string? TriggerConditionsJson { get; set; }

  /// <summary>
  /// Reward XP for completing this mission
  /// </summary>
  public int RewardXp { get; set; } = 0;

  /// <summary>
  /// Reward coins/diamonds for completing this mission
  /// </summary>
  public int RewardCoins { get; set; } = 0;

  /// <summary>
  /// Optional badge ID awarded upon completion
  /// </summary>
  public int? RewardBadgeId { get; set; }

  /// <summary>
  /// Optional shop item ID awarded upon completion
  /// </summary>
  public int? RewardItemId { get; set; }

  /// <summary>
  /// When this mission expires and is no longer available.
  /// Null means no expiration.
  /// </summary>
  public DateTime? ExpiresAt { get; set; }

  /// <summary>
  /// Is this mission currently active and available to users?
  /// </summary>
  public bool IsActive { get; set; } = true;

  /// <summary>
  /// Priority/order for UI display (lower = higher priority)
  /// </summary>
  public int DisplayOrder { get; set; } = 0;

  /// <summary>
  /// Optional image/icon reference for the mission UI
  /// </summary>
  public string? ImageRef { get; set; }

  #region Navigation Properties
  public ICollection<UserMission> UserMissions { get; set; } = new List<UserMission>();
  public ICollection<UserMissionProgress> UserMissionProgresses { get; set; } =
    new List<UserMissionProgress>();
  #endregion
}

/// <summary>
/// Tracks a user's progress and completion state for a specific mission.
/// </summary>
public class UserMission : BaseEntity<int>
{
  public int UserId { get; set; }
  public int MissionId { get; set; }

  /// <summary>
  /// Current progress value toward mission completion.
  /// For "complete 5 quizzes", this would track 0-5.
  /// </summary>
  public int CurrentProgress { get; set; } = 0;

  /// <summary>
  /// When the user completed this mission objective.
  /// Null if not yet completed.
  /// </summary>
  public DateTime? CompletedAt { get; set; }

  /// <summary>
  /// When the user claimed the mission rewards.
  /// Null if rewards not yet claimed.
  /// </summary>
  public DateTime? ClaimedAt { get; set; }

  /// <summary>
  /// Optional metadata JSON for tracking multi-step mission progress.
  /// E.g., { "completedQuizIds": [1, 2, 3], "categories": ["math", "english"] }
  /// </summary>
  public string? ProgressMetadataJson { get; set; }

  #region Navigation Properties
  public User.User User { get; set; } = null!;
  public Mission Mission { get; set; } = null!;
  #endregion
}

/// <summary>
/// Tracks incremental progress events for a mission (e.g., each quiz completion).
/// Useful for validating that mission progress is earned legitimately.
/// </summary>
public class UserMissionProgress : BaseEntity<int>
{
  public int UserId { get; set; }
  public int MissionId { get; set; }

  /// <summary>
  /// The achievement event that contributed progress to this mission.
  /// E.g., "QuizCompleted"
  /// </summary>
  public string EventType { get; set; } = string.Empty;

  /// <summary>
  /// Reference ID to the triggering event (e.g., quiz attempt ID).
  /// Ensures the same event can't be counted twice.
  /// </summary>
  public string EventReferenceId { get; set; } = string.Empty;

  /// <summary>
  /// How much progress was awarded for this event (typically 1).
  /// </summary>
  public int ProgressValue { get; set; } = 1;

  /// <summary>
  /// When this progress was recorded.
  /// </summary>
  public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

  #region Navigation Properties
  public User.User User { get; set; } = null!;
  public Mission Mission { get; set; } = null!;
  #endregion
}
