using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Commands;

public record GenerateWeeklyReportJobCommand(
    int AuditLogId,
    int ClassroomId,
    int TeacherId)
    : IRequest<OperationResult<bool>>;
