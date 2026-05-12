namespace CleanArc.Application.Contracts.Achievements;

public interface IAchievementTrackingService
{
  Task<IReadOnlyList<int>> TrackEventAsync(
      int userId,
      string eventType,
      string eventId,
      string propertiesJson,
      CancellationToken cancellationToken = default);

  Task<IReadOnlyList<StudentAchievementDto>> GetStudentAchievementsAsync(
      int userId,
      CancellationToken cancellationToken = default);

  Task<IReadOnlyList<int>> SyncStudentAchievementsAsync(
      int userId,
      CancellationToken cancellationToken = default);
}

public sealed record StudentAchievementDto(
  int Id,
  string Code,
  string Title,
  string Description,
  string Category,
  string? EventType,
  decimal ProgressValue,
  decimal TargetValue,
  bool IsUnlocked,
  DateTime? UnlockedAt,
  int RewardXp,
  int RewardDiamonds,
  string? IconUrl,
  string? ImageRef);
