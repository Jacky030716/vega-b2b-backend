using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Commands;

public record CreateClassroomCommand(
    int TeacherId,
    string Name,
    string Description,
    string Subject,
    IReadOnlyCollection<string>? Subjects,
    string? Thumbnail,
    int YearLevel = 1)
    : IRequest<OperationResult<int>>;
