using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Commands;

public record CreateClassroomCommand(int TeacherId, string Name, string Description, string Subject, string? Thumbnail)
    : IRequest<OperationResult<int>>;
