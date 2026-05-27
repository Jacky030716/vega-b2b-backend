using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Queries;

public record GenerateWeeklyReportQuery(
    int ClassroomId,
    int TeacherId)
    : IRequest<OperationResult<WeeklyReportDto>>;

public record WeeklyReportDto(string ReportMarkdown);
