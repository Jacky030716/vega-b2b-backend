using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.AI;

public class AiUsageLog : BaseEntity<int>
{
    public int UserId { get; set; }
    public string FeatureType { get; set; } = string.Empty;
    public string EndpointKey { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string? ModelName { get; set; }
    public int RequestCount { get; set; } = 1;
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
