using CleanArc.Domain.Common;
using CleanArc.Domain.Entities.Quiz;

namespace CleanArc.Domain.Entities.AI;

public class AiAuditLog : BaseEntity<int>
{
    public string UseCase { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string? ModelName { get; set; }
    public string PromptVersion { get; set; } = string.Empty;
    public string InputPayloadJson { get; set; } = "{}";
    public string? RawOutputJson { get; set; }
    public string? ParsedOutputJson { get; set; }
    public string ValidationStatus { get; set; } = "PENDING";
    public string ValidationErrorsJson { get; set; } = "[]";
    public int? RelatedUserId { get; set; }
    public int? RelatedClassroomId { get; set; }
    public int? RelatedModuleId { get; set; }
    public int? RelatedChallengeId { get; set; }
    public Challenge? RelatedChallenge { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
