using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Quizzes.Queries.GetQuizQuestions;

public class GetQuizQuestionsQueryHandler(IQuizContentRepository quizRepository)
    : IRequestHandler<GetQuizQuestionsQuery, OperationResult<IReadOnlyList<object>>>
{
  public async ValueTask<OperationResult<IReadOnlyList<object>>> Handle(GetQuizQuestionsQuery request, CancellationToken cancellationToken)
  {
    var questions = await quizRepository.GetQuizQuestionsAsync(request.QuizId);
    if (questions.Count == 0)
    {
      return OperationResult<IReadOnlyList<object>>.NotFoundResult("Quiz not found");
    }

    return OperationResult<IReadOnlyList<object>>.SuccessResult(questions);
  }
}
