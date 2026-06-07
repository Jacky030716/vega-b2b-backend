using System.Collections.Generic;

namespace CleanArc.Application.Contracts.AI;

public record VegaRecommendationDto(
    string Id,
    string Type, // "CHALLENGE_DRAFT" | "STUDENT_RISK" | "ENGAGEMENT_MISSION"
    string Title,
    string Reason,
    int Confidence,
    int AffectedStudentsCount,
    IReadOnlyList<string> Evidence,
    string ProposedAction,
    VegaChallengeDraftPayload? DraftPayload
);

public record VegaChallengeDraftPayload(
    string GameType,
    int? ModuleId,
    string Title,
    IReadOnlyList<string> Words,
    int DifficultyLevel,
    int QuestionCount
);
