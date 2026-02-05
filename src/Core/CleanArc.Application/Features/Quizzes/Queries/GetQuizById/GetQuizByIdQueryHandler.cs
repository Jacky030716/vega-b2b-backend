using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Features.Quizzes.Models;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Quizzes.Queries.GetQuizById;

public class GetQuizByIdQueryHandler(IQuizContentRepository quizRepository)
    : IRequestHandler<GetQuizByIdQuery, OperationResult<QuizDetailDto>>
{
  public async ValueTask<OperationResult<QuizDetailDto>> Handle(GetQuizByIdQuery request, CancellationToken cancellationToken)
  {
    var quiz = await quizRepository.GetQuizByIdAsync(request.QuizId);
    if (quiz is null)
    {
      return OperationResult<QuizDetailDto>.NotFoundResult("Quiz not found");
    }

    return OperationResult<QuizDetailDto>.SuccessResult(quiz);
  }
}
