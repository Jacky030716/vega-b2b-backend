using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Quizzes.Queries.GetQuizQuestions;

public record GetQuizQuestionsQuery(string QuizId) : IRequest<OperationResult<IReadOnlyList<object>>>;
