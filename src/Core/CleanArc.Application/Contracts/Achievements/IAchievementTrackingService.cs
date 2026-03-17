namespace CleanArc.Application.Contracts.Achievements;

public interface IAchievementTrackingService
{
  Task<IReadOnlyList<int>> TrackEventAsync(
      int userId,
      string eventType,
      string eventId,
      string propertiesJson,
      CancellationToken cancellationToken = default);
}
