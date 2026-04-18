using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Queries;

// ─── DTOs ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Summary of a challenge assigned to a classroom, enriched with student completion counts.
/// </summary>
public record ClassroomChallengeDto(
    int ChallengeId,
    string GameKey,
    string Title,
    string Description,
    int DifficultyLevel,
    int OrderIndex,
    bool IsAIGenerated,
    DateTime CreatedAt,
    /// <summary>Number of students who have completed this challenge at least once.</summary>
    int CompletedStudentCount,
    /// <summary>Total number of students in the classroom.</summary>
    int TotalStudentCount
);

// ─── Query ────────────────────────────────────────────────────────────────────

/// <summary>
/// Returns all challenges created for a specific classroom,
/// enriched with per-challenge student completion counts.
/// </summary>
public record GetClassroomChallengesQuery(int ClassroomId, int RequestingTeacherId)
    : IRequest<OperationResult<List<ClassroomChallengeDto>>>;
