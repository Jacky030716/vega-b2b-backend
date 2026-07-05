using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Commands;

public sealed record RemoveClassroomStudentCommand(
    int ClassroomId,
    int StudentId,
    int RequestingUserId,
    bool IsAdmin) : IRequest<OperationResult<int>>;
