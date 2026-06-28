using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Commands;

public record GenerateClassroomThumbnailJobCommand(
    int AuditLogId,
    int UserId,
    string ClassroomName,
    string? Description,
    string ThumbnailPrompt)
    : IRequest<OperationResult<bool>>;
