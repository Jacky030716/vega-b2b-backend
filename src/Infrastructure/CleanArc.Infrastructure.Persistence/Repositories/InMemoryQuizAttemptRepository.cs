using System.Collections.Concurrent;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Features.Quizzes.Models;

namespace CleanArc.Infrastructure.Persistence.Repositories;

internal class InMemoryQuizAttemptRepository : IQuizAttemptRepository
{
  private static readonly ConcurrentDictionary<string, QuizAttemptRecord> Attempts = new();

  public Task<QuizAttemptDto> CreateAttemptAsync(int userId, string quizId, string mode, string clientVersion)
  {
    var attempt = new QuizAttemptRecord
    {
      AttemptId = Guid.NewGuid().ToString("N"),
      QuizId = quizId,
      UserId = userId,
      StartedAt = DateTime.UtcNow,
      Mode = mode,
      ClientVersion = clientVersion
    };

    Attempts[attempt.AttemptId] = attempt;

    return Task.FromResult<QuizAttemptDto>(attempt);
  }

  public Task<QuizAttemptRecord> GetAttemptAsync(int userId, string quizId, string attemptId)
  {
    if (Attempts.TryGetValue(attemptId, out var attempt))
    {
      if (attempt.UserId == userId && string.Equals(attempt.QuizId, quizId, StringComparison.OrdinalIgnoreCase))
      {
        return Task.FromResult<QuizAttemptRecord>(attempt);
      }
    }

    return Task.FromResult<QuizAttemptRecord>(null);
  }

  public Task UpdateAttemptAsync(int userId, string quizId, string attemptId, AttemptAnswerDto answer)
  {
    if (Attempts.TryGetValue(attemptId, out var attempt))
    {
      if (attempt.UserId == userId && string.Equals(attempt.QuizId, quizId, StringComparison.OrdinalIgnoreCase))
      {
        attempt.Answers.Add(answer);
      }
    }

    return Task.CompletedTask;
  }

  public Task SetCompletedAsync(int userId, string quizId, string attemptId, DateTime completedAt, int? totalTimeSec, IReadOnlyList<AttemptAnswerDto> answers)
  {
    if (Attempts.TryGetValue(attemptId, out var attempt))
    {
      if (attempt.UserId == userId && string.Equals(attempt.QuizId, quizId, StringComparison.OrdinalIgnoreCase))
      {
        attempt.CompletedAt = completedAt;
        attempt.TotalTimeSec = totalTimeSec;
        attempt.Answers = answers.ToList();
      }
    }

    return Task.CompletedTask;
  }
}
