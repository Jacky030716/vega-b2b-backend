using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Features.Quizzes.Models;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Quizzes.Commands.UpdateQuizAttempt;

public class UpdateQuizAttemptCommandHandler(IQuizAttemptRepository attemptRepository)
    : IRequestHandler<UpdateQuizAttemptCommand, OperationResult<UpdateQuizAttemptResponse>>
{
  public async ValueTask<OperationResult<UpdateQuizAttemptResponse>> Handle(UpdateQuizAttemptCommand request, CancellationToken cancellationToken)
  {
    var attempt = await attemptRepository.GetAttemptAsync(request.UserId, request.QuizId, request.AttemptId);
    if (attempt is null)
    {
      return OperationResult<UpdateQuizAttemptResponse>.NotFoundResult("Attempt not found");
    }

    var answer = new AttemptAnswerDto
    {
      QuestionId = request.Request.QuestionId,
      Answer = request.Request.Answer,
      IsCorrect = request.Request.IsCorrect,
      TimeSpentSec = request.Request.ElapsedSec
    };

    await attemptRepository.UpdateAttemptAsync(request.UserId, request.QuizId, request.AttemptId, answer);

    var updatedAttempt = await attemptRepository.GetAttemptAsync(request.UserId, request.QuizId, request.AttemptId);
    var answersCount = updatedAttempt?.Answers.Count ?? attempt.Answers.Count;

    var response = new UpdateQuizAttemptResponse
    {
      AttemptId = request.AttemptId,
      QuizId = request.QuizId,
      AnswersCount = answersCount
    };

    return OperationResult<UpdateQuizAttemptResponse>.SuccessResult(response);
  }
}
