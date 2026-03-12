using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Queries;

public record GetClassroomMembersQuery(int ClassroomId, int RequestingUserId) : IRequest<OperationResult<List<ClassroomMemberDto>>>;

/// <summary>Lightweight student profile for the classroom crew view.</summary>
public record ClassroomMemberDto(
    int UserId,
    string UserName,
    string DisplayName,
    string? AvatarId,
    int Experience,
    int Diamonds,
    DateTime JoinedAt
);
