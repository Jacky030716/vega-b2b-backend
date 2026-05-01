using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Commands;

public record UpdateClassroomCommand(
    int ClassroomId,
    int RequestingUserId,
    bool IsAdmin,
    string Name,
    string Subject,
    int? YearLevel,
    string? Description) : IRequest<OperationResult<UpdatedClassroomDto>>;

public record UpdatedClassroomDto(
    int Id,
    string Name,
    string Subject,
    int YearLevel,
    string Description,
    DateTime UpdatedAt);
