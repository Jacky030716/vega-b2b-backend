using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Commands;

public record UpdateClassroomCommand(
    int ClassroomId,
    int RequestingUserId,
    bool IsAdmin,
    string Name,
    string? Description,
    ClassroomThumbnailRequest? ThumbnailInfo = null) : IRequest<OperationResult<UpdatedClassroomDto>>;

public record UpdatedClassroomDto(
    int Id,
    string Name,
    string Description,
    DateTime UpdatedAt);
