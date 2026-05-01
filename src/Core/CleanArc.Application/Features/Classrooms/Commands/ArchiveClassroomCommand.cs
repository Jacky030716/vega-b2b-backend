using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Commands;

public record ArchiveClassroomCommand(int ClassroomId, int RequestingUserId, bool IsAdmin)
    : IRequest<OperationResult<ArchiveClassroomResult>>;

public record ArchiveClassroomResult(bool Success, string Message);
