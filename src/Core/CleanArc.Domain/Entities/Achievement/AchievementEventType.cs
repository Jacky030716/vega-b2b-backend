#nullable enable

namespace CleanArc.Domain.Entities.Achievement;

public enum AchievementEventType
{
  // Runtime event keys used by the tracking engine (snake_case for payload compatibility)
  attempt_completed = 1,
  daily_check_in = 2,
  diamond_earned = 3,
  diamond_spent = 4,
  classroom_joined = 5,

  // Quiz-related events
  QuizCompleted = 100,
  QuizPerfectScore = 101,
  QuizStreakReached = 102,
  QuizGamePlayed = 103, // specific game type like magic_backpack, word_bridge, etc.

  // Mission-related events
  MissionCompleted = 110,
  MissionClaimed = 111,
  DailyMissionCompleted = 112,
  WeeklyMissionCompleted = 113,

  // Social events
  FriendshipForged = 120,
  GiftSent = 121,
  HighFiveGiven = 122,
  ClassroomJoined = 123,

  // Progression events
  LevelMilestone = 130,
  ExperienceEarned = 131,
  PointsMilestone = 132,

  // Streak events
  DailyStreakReached = 140,
  ConsecutiveQuizzesCompleted = 141,

  // Shop/Avatar events
  ItemPurchased = 150,
  AvatarChanged = 151,
  CollectibleUnlocked = 152,

  // Engagement events
  SessionStarted = 160,
  SessionEnded = 161,
  GameLaunched = 162,
  AdventureMapProgressed = 163,

  // Discovery interaction events
  AchievementScreenOpened = 170,
  FavoriteBadgeAssigned = 171,
  BadgeDetailOpened = 172,
}

/// <summary>
/// Helper class for working with achievement events.
/// </summary>
public static class AchievementEventTypeExtensions
{
  private static readonly Dictionary<string, AchievementEventType> EventAliases =
    new(StringComparer.OrdinalIgnoreCase)
    {
      ["achievement_screen_opened"] = AchievementEventType.AchievementScreenOpened,
      ["favorite_badge_assigned"] = AchievementEventType.FavoriteBadgeAssigned,
      ["badge_detail_opened"] = AchievementEventType.BadgeDetailOpened,
      ["classroom_joined"] = AchievementEventType.ClassroomJoined,
      ["daily_check_in"] = AchievementEventType.daily_check_in,
      ["attempt_completed"] = AchievementEventType.attempt_completed,
      ["diamond_earned"] = AchievementEventType.diamond_earned,
      ["diamond_spent"] = AchievementEventType.diamond_spent,
    };

  /// <summary>Gets the string representation of an event type for storage/API.</summary>
  public static string GetEventTypeString(this AchievementEventType eventType)
  {
    return eventType.ToString();
  }

  /// <summary>
  /// Converts an incoming event type string into the canonical enum-backed event type string.
  /// Returns null when the value cannot be recognized.
  /// </summary>
  public static string? NormalizeEventType(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
      return null;

    if (TryParseEventType(value, out var parsed))
      return parsed.GetEventTypeString();

    var key = value.Trim();
    if (EventAliases.TryGetValue(key, out var aliasType))
      return aliasType.GetEventTypeString();

    return null;
  }

  /// <summary>
  /// Compares two event type strings by normalizing both sides to canonical enum-backed values.
  /// </summary>
  public static bool EventTypeMatches(string? left, string? right)
  {
    var normalizedLeft = NormalizeEventType(left);
    var normalizedRight = NormalizeEventType(right);

    return !string.IsNullOrWhiteSpace(normalizedLeft)
      && string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>Tries to parse a string to an AchievementEventType.</summary>
  public static bool TryParseEventType(string? value, out AchievementEventType eventType)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      eventType = AchievementEventType.QuizCompleted;
      return false;
    }

    if (Enum.TryParse<AchievementEventType>(value, ignoreCase: true, out eventType))
      return true;

    return EventAliases.TryGetValue(value.Trim(), out eventType);
  }
}
