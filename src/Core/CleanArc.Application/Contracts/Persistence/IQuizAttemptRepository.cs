using CleanArc.Application.Features.Quizzes.Models;

namespace CleanArc.Application.Contracts.Persistence;

public interface IQuizAttemptRepository
{
  Task<QuizAttemptDto> CreateAttemptAsync(int userId, string quizId, string mode, string clientVersion);
  Task<QuizAttemptRecord> GetAttemptAsync(int userId, string quizId, string attemptId);
  Task UpdateAttemptAsync(int userId, string quizId, string attemptId, AttemptAnswerDto answer);
  Task SetCompletedAsync(int userId, string quizId, string attemptId, DateTime completedAt, int? totalTimeSec, IReadOnlyList<AttemptAnswerDto> answers);
}
