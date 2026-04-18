using System.Text.Json;

namespace CleanArc.Web.Api.Contracts.Requests.Achievements;

public record SetFeaturedBadgeRequest(int BadgeId, int SlotIndex);

public record TrackAchievementEventRequest(string EventType, string EventId, JsonElement Properties);
