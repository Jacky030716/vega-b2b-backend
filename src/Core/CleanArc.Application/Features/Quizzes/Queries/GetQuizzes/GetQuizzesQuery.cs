using CleanArc.Application.Features.Quizzes.Models;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Quizzes.Queries.GetQuizzes;

public record GetQuizzesQuery(string Type, string GameType)
    : IRequest<OperationResult<IReadOnlyList<QuizSummaryDto>>>;
