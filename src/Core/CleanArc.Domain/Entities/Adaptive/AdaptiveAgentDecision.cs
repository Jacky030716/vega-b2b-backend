using System;
using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Adaptive;

public class AdaptiveAgentDecision : BaseEntity<int>
{
    public string AgentName { get; set; } = string.Empty; // e.g. "AdaptiveLearningAgent"
    public int StudentId { get; set; }
    public User.User Student { get; set; } = null!;

    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
    public bool IsEligible { get; set; }
    public string TriggeringMetricsJson { get; set; } = "{}";
    public string DecisionReason { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }

    public int? GeneratedDraftId { get; set; }
    public HardcoreChallengeDraft? GeneratedDraft { get; set; }
}
