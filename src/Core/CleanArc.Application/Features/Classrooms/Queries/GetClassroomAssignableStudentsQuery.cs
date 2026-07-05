using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Queries;

public record GetClassroomAssignableStudentsQuery(
    int ClassroomId,
    int RequestingUserId,
    bool IsAdmin) : IRequest<OperationResult<List<ClassroomAssignableStudentDto>>>;

public record ClassroomAssignableStudentDto(
    int Id,
    string DisplayName,
    string UserName,
    string Email,
    string? ClassName,
    bool IsActive);
