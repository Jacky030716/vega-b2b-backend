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
}

/// <summary>
/// Helper class for working with achievement events.
/// </summary>
public static class AchievementEventTypeExtensions
{
  /// <summary>Gets the string representation of an event type for storage/API.</summary>
  public static string GetEventTypeString(this AchievementEventType eventType)
  {
    return eventType.ToString();
  }

  /// <summary>Tries to parse a string to an AchievementEventType.</summary>
  public static bool TryParseEventType(string? value, out AchievementEventType eventType)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      eventType = AchievementEventType.QuizCompleted;
      return false;
    }

    return Enum.TryParse<AchievementEventType>(value, ignoreCase: true, out eventType);
  }
}
