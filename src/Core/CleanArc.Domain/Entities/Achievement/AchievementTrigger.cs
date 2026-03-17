#nullable enable

using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Achievement;

/// <summary>
/// Defines how a specific badge can be triggered/unlocked based on achievement events.
/// One badge can have multiple triggers (e.g., "complete 5 quizzes OR reach level 10").
/// This creates the link between achievements and various game activities/missions.
/// </summary>
public class AchievementTrigger : BaseEntity<int>
{
  /// <summary>
  /// The badge this trigger can unlock
  /// </summary>
  public int BadgeId { get; set; }

  /// <summary>
  /// The type of achievement event that triggers progress on this badge.
  /// Must match an AchievementEventType enum value (e.g., "QuizCompleted", "MissionClaimed").
  /// </summary>
  public string EventType { get; set; } = string.Empty;

  /// <summary>
  /// Human-readable description of when this badge is unlocked.
  /// E.g., "Complete Magic Backpack 10 times" or "Earn 5000 experience points"
  /// </summary>
  public string Description { get; set; } = string.Empty;

  /// <summary>
  /// The aggregation type for counting progress toward the badge.
  /// Options: "count" (increment by 1), "sum" (add property value), "max" (track max value).
  /// </summary>
  public string AggregationType { get; set; } = "count";

  /// <summary>
  /// The field name to aggregate if AggregationType is "sum" or "max".
  /// E.g., "score", "xpAmount", "timeSpent".
  /// Null if aggregation is "count".
  /// </summary>
  public string? AggregationSourceField { get; set; }

  /// <summary>
  /// The threshold value that must be reached to unlock the badge.
  /// For "count", this is how many times an event must occur.
  /// For "sum", this is the total to accumulate.
  /// For "max", this is the maximum value required.
  /// </summary>
  public decimal Threshold { get; set; } = 1;

  /// <summary>
  /// Optional JSON filters that must match event properties for progress to count.
  /// E.g., for QuizCompleted: { "minimumScore": 80, "gameType": "word_bridge" }
  /// None means all events of this type count toward progress.
  /// </summary>
  public string? FilterConditionsJson { get; set; }

  /// <summary>
  /// If true, badge progress is reset and the player can unlock this badge multiple times.
  /// If false, once unlocked, it cannot be earned again.
  /// </summary>
  public bool IsRepeatable { get; set; } = false;

  /// <summary>
  /// Supported mission types or activity contexts where this badge can be earned.
  /// CSV format: "quiz,daily_mission,weekly_mission" or null for universal.
  /// Used for filtering which activities contribute to this badge.
  /// </summary>
  public string? SupportedContexts { get; set; }

  /// <summary>
  /// Is this trigger currently active? Allows soft-delete or disabling old rules.
  /// </summary>
  public bool IsActive { get; set; } = true;

  /// <summary>
  /// Priority/order for evaluating multiple triggers (lower = higher priority).
  /// Useful if multiple badges can be unlocked from the same event.
  /// </summary>
  public int EvaluationOrder { get; set; } = 0;

  #region Navigation Properties
  public Badge Badge { get; set; } = null!;
  #endregion
}
