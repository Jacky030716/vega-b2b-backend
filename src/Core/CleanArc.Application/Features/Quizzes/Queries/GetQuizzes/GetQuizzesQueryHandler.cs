using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Features.Quizzes.Models;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Quizzes.Queries.GetQuizzes;

public class GetQuizzesQueryHandler(IQuizContentRepository quizRepository)
    : IRequestHandler<GetQuizzesQuery, OperationResult<IReadOnlyList<QuizSummaryDto>>>
{
  public async ValueTask<OperationResult<IReadOnlyList<QuizSummaryDto>>> Handle(GetQuizzesQuery request, CancellationToken cancellationToken)
  {
    var quizzes = await quizRepository.GetQuizzesAsync(request.Type, request.GameType);
    return OperationResult<IReadOnlyList<QuizSummaryDto>>.SuccessResult(quizzes);
  }
}
