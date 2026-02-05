using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Features.Quizzes.Models;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Quizzes.Commands.SubmitQuizAttempt;

public class SubmitQuizAttemptCommandHandler(IQuizAttemptRepository attemptRepository, IQuizContentRepository quizRepository)
    : IRequestHandler<SubmitQuizAttemptCommand, OperationResult<SubmitQuizAttemptResponse>>
{
  public async ValueTask<OperationResult<SubmitQuizAttemptResponse>> Handle(SubmitQuizAttemptCommand request, CancellationToken cancellationToken)
  {
    var attempt = await attemptRepository.GetAttemptAsync(request.UserId, request.QuizId, request.AttemptId);
    if (attempt is null)
    {
      return OperationResult<SubmitQuizAttemptResponse>.NotFoundResult("Attempt not found");
    }

    var questions = await quizRepository.GetQuizQuestionsAsync(request.QuizId);
    if (questions.Count == 0)
    {
      return OperationResult<SubmitQuizAttemptResponse>.NotFoundResult("Quiz not found");
    }

    var pointsByQuestionId = new Dictionary<string, int>();
    foreach (var question in questions)
    {
      if (question is Dictionary<string, object> dict && dict.TryGetValue("id", out var idValue))
      {
        var id = idValue?.ToString();
        if (!string.IsNullOrWhiteSpace(id) && dict.TryGetValue("points", out var pointsValue))
        {
          if (int.TryParse(pointsValue?.ToString(), out var points))
          {
            pointsByQuestionId[id] = points;
          }
        }
      }
    }

    var totalPoints = pointsByQuestionId.Values.Sum();
    var score = 0;

    foreach (var answer in request.Request.Answers)
    {
      if (answer.IsCorrect == true && pointsByQuestionId.TryGetValue(answer.QuestionId, out var points))
      {
        score += points;
      }
    }

    var completedAt = request.Request.CompletedAt ?? DateTime.UtcNow;
    await attemptRepository.SetCompletedAsync(request.UserId, request.QuizId, request.AttemptId, completedAt, request.Request.TotalTimeSec, request.Request.Answers);

    var percentage = totalPoints == 0 ? 0 : (int)Math.Round((double)score / totalPoints * 100);
    var xpAwarded = score;
    var diamondsAwarded = Math.Max(0, score / 10);

    var response = new SubmitQuizAttemptResponse
    {
      AttemptId = request.AttemptId,
      Score = score,
      TotalPoints = totalPoints,
      Percentage = percentage,
      XpAwarded = xpAwarded,
      DiamondsAwarded = diamondsAwarded,
      BadgesUnlocked = new List<string>()
    };

    return OperationResult<SubmitQuizAttemptResponse>.SuccessResult(response);
  }
}
