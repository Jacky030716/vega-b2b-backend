using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Features.Quizzes.Models;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Quizzes.Commands.CreateQuizAttempt;

public class CreateQuizAttemptCommandHandler(IQuizAttemptRepository attemptRepository, IQuizContentRepository quizRepository)
    : IRequestHandler<CreateQuizAttemptCommand, OperationResult<CreateQuizAttemptResponse>>
{
  public async ValueTask<OperationResult<CreateQuizAttemptResponse>> Handle(CreateQuizAttemptCommand request, CancellationToken cancellationToken)
  {
    var quiz = await quizRepository.GetQuizByIdAsync(request.QuizId);
    if (quiz is null)
    {
      return OperationResult<CreateQuizAttemptResponse>.NotFoundResult("Quiz not found");
    }

    var mode = request.Request.Mode ?? string.Empty;
    var clientVersion = request.Request.ClientVersion ?? string.Empty;
    var attempt = await attemptRepository.CreateAttemptAsync(request.UserId, request.QuizId, mode, clientVersion);

    var response = new CreateQuizAttemptResponse
    {
      AttemptId = attempt.AttemptId,
      QuizId = attempt.QuizId,
      StartedAt = attempt.StartedAt,
      Seed = null
    };

    return OperationResult<CreateQuizAttemptResponse>.SuccessResult(response);
  }
}
