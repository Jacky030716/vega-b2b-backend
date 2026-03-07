using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Commands;

public record AssignQuizCommand(int ClassroomId, int TeacherId, string QuizId, DateTime? DueDate = null)
    : IRequest<OperationResult<bool>>;
