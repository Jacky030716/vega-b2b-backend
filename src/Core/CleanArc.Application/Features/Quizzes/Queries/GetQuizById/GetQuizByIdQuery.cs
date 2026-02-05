using CleanArc.Application.Features.Quizzes.Models;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Quizzes.Queries.GetQuizById;

public record GetQuizByIdQuery(string QuizId) : IRequest<OperationResult<QuizDetailDto>>;
