using CleanArc.Application.Features.Games.Queries;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Queries;

/// <summary>
/// Returns the student's adventure-map nodes for challenges assigned to a specific classroom.
/// </summary>
public record GetClassroomAdventureMapQuery(int ClassroomId, int UserId, string GameKey)
    : IRequest<OperationResult<List<ChallengeNodeDto>>>;