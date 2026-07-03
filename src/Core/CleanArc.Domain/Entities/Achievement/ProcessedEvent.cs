using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Achievement;

/// <summary>
/// Idempotent event log to ensure events are processed exactly once and badge awards are deduplicated.
/// </summary>
public class ProcessedEvent : BaseEntity<string>
{
    public ProcessedEvent()
    {
    }

    public ProcessedEvent(string eventId)
    {
        Id = eventId;
    }

    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Comma-separated list of Badge IDs unlocked as a result of this event.
    /// E.g. "123,456"
    /// </summary>
    public string UnlockedBadgeIds { get; set; } = string.Empty;
}
