using System.Text.Json;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Features.Quizzes.Models;
using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Repositories;

internal class QuizAttemptRepository(ApplicationDbContext dbContext) : IQuizAttemptRepository
{
  private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

  public async Task<QuizAttemptDto> CreateAttemptAsync(int userId, string quizId, string mode, string clientVersion)
  {
    var attempt = new QuizAttempt
    {
      AttemptId = Guid.NewGuid().ToString("N"),
      QuizId = quizId,
      UserId = userId,
      StartedAt = DateTime.UtcNow,
      Mode = mode ?? string.Empty,
      ClientVersion = clientVersion ?? string.Empty
    };

    dbContext.QuizAttempts.Add(attempt);
    await dbContext.SaveChangesAsync();

    return MapAttempt(attempt, new List<QuizAttemptAnswer>());
  }

  public async Task<QuizAttemptRecord> GetAttemptAsync(int userId, string quizId, string attemptId)
  {
    var attempt = await dbContext.QuizAttempts
        .Include(a => a.Answers)
            .ThenInclude(a => a.MagicBackpackAttemptAnswer)
                .ThenInclude(m => m.Selections)
        .Include(a => a.Answers)
            .ThenInclude(a => a.WordBridgeAttemptAnswer)
        .Include(a => a.Answers)
            .ThenInclude(a => a.StoryRecallAttemptAnswer)
                .ThenInclude(s => s.Selections)
        .AsNoTracking()
        .FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.UserId == userId && a.QuizId == quizId);

    if (attempt == null)
    {
      return null;
    }

    return MapAttemptRecord(attempt, attempt.Answers.ToList());
  }

  public async Task UpdateAttemptAsync(int userId, string quizId, string attemptId, AttemptAnswerDto answer)
  {
    var attempt = await dbContext.QuizAttempts
        .FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.UserId == userId && a.QuizId == quizId);

    if (attempt == null)
    {
      return;
    }

    await AddAnswerAsync(attempt, quizId, answer);
  }

  public async Task SetCompletedAsync(int userId, string quizId, string attemptId, DateTime completedAt, int? totalTimeSec, IReadOnlyList<AttemptAnswerDto> answers)
  {
    var attempt = await dbContext.QuizAttempts
        .Include(a => a.Answers)
        .FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.UserId == userId && a.QuizId == quizId);

    if (attempt == null)
    {
      return;
    }

    attempt.CompletedAt = completedAt;
    attempt.TotalTimeSec = totalTimeSec;

    if (attempt.Answers.Any())
    {
      dbContext.QuizAttemptAnswers.RemoveRange(attempt.Answers);
    }

    foreach (var answer in answers)
    {
      await AddAnswerAsync(attempt, quizId, answer, save: false);
    }

    await dbContext.SaveChangesAsync();
  }

  private QuizAttemptDto MapAttempt(QuizAttempt attempt, List<QuizAttemptAnswer> answers)
  {
    return new QuizAttemptDto
    {
      AttemptId = attempt.AttemptId,
      QuizId = attempt.QuizId,
      UserId = attempt.UserId,
      StartedAt = attempt.StartedAt,
      CompletedAt = attempt.CompletedAt,
      TotalTimeSec = attempt.TotalTimeSec,
      Answers = MapAnswers(answers)
    };
  }

  private QuizAttemptRecord MapAttemptRecord(QuizAttempt attempt, List<QuizAttemptAnswer> answers)
  {
    return new QuizAttemptRecord
    {
      AttemptId = attempt.AttemptId,
      QuizId = attempt.QuizId,
      UserId = attempt.UserId,
      StartedAt = attempt.StartedAt,
      CompletedAt = attempt.CompletedAt,
      TotalTimeSec = attempt.TotalTimeSec,
      Mode = attempt.Mode,
      ClientVersion = attempt.ClientVersion,
      Answers = MapAnswers(answers)
    };
  }

  private List<AttemptAnswerDto> MapAnswers(List<QuizAttemptAnswer> answers)
  {
    return answers.Select(a => new AttemptAnswerDto
    {
      QuestionId = a.QuestionId,
      Answer = BuildAnswerObject(a),
      IsCorrect = a.IsCorrect,
      TimeSpentSec = a.TimeSpentSec
    }).ToList();
  }

  private async Task AddAnswerAsync(QuizAttempt attempt, string quizId, AttemptAnswerDto answer, bool save = true)
  {
    var questionType = await dbContext.QuizQuestions
        .Where(q => q.Quiz.QuizId == quizId && q.QuestionId == answer.QuestionId)
        .Select(q => q.Type)
        .FirstOrDefaultAsync() ?? string.Empty;

    if (string.IsNullOrWhiteSpace(questionType))
    {
      return;
    }

    var answerEntity = new QuizAttemptAnswer
    {
      QuizAttemptId = attempt.Id,
      QuestionId = answer.QuestionId,
      QuestionType = questionType,
      IsCorrect = answer.IsCorrect,
      TimeSpentSec = answer.TimeSpentSec
    };

    switch (questionType)
    {
      case "magic_backpack":
        answerEntity.MagicBackpackAttemptAnswer = BuildMagicBackpackAnswer(answer.Answer);
        break;
      case "word_bridge":
        answerEntity.WordBridgeAttemptAnswer = BuildWordBridgeAnswer(answer.Answer);
        break;
      case "story_recall":
        answerEntity.StoryRecallAttemptAnswer = BuildStoryRecallAnswer(answer.Answer);
        break;
    }

    dbContext.QuizAttemptAnswers.Add(answerEntity);

    if (save)
    {
      await dbContext.SaveChangesAsync();
    }
  }

  private MagicBackpackAttemptAnswer BuildMagicBackpackAnswer(object answer)
  {
    var result = new MagicBackpackAttemptAnswer();
    if (answer is JsonElement element && element.ValueKind == JsonValueKind.Object)
    {
      if (element.TryGetProperty("isSuccess", out var successProp) && successProp.ValueKind != JsonValueKind.Null)
      {
        result.IsSuccess = successProp.GetBoolean();
      }

      if (element.TryGetProperty("selections", out var selectionsProp) && selectionsProp.ValueKind == JsonValueKind.Array)
      {
        var index = 0;
        foreach (var item in selectionsProp.EnumerateArray())
        {
          var itemId = item.ValueKind switch
          {
            JsonValueKind.String => item.GetString(),
            JsonValueKind.Object when item.TryGetProperty("id", out var idProp) => idProp.GetString(),
            _ => null
          };

          if (!string.IsNullOrWhiteSpace(itemId))
          {
            result.Selections.Add(new MagicBackpackAttemptSelection
            {
              ItemId = itemId,
              Order = index
            });
            index++;
          }
        }
      }
    }

    return result;
  }

  private WordBridgeAttemptAnswer BuildWordBridgeAnswer(object answer)
  {
    var result = new WordBridgeAttemptAnswer();
    if (answer is JsonElement element && element.ValueKind == JsonValueKind.Object)
    {
      if (element.TryGetProperty("success", out var successProp) && successProp.ValueKind != JsonValueKind.Null)
      {
        result.Success = successProp.GetBoolean();
      }

      if (element.TryGetProperty("attempts", out var attemptsProp) && attemptsProp.ValueKind == JsonValueKind.Number)
      {
        result.Attempts = attemptsProp.GetInt32();
      }

      if (element.TryGetProperty("timeMs", out var timeProp) && timeProp.ValueKind == JsonValueKind.Number)
      {
        result.TimeMs = timeProp.GetInt32();
      }

      if (element.TryGetProperty("targetWord", out var wordProp) && wordProp.ValueKind == JsonValueKind.String)
      {
        result.TargetWord = wordProp.GetString();
      }
    }

    result.TargetWord ??= string.Empty;
    return result;
  }

  private StoryRecallAttemptAnswer BuildStoryRecallAnswer(object answer)
  {
    var result = new StoryRecallAttemptAnswer { Phase = string.Empty };
    if (answer is JsonElement element && element.ValueKind == JsonValueKind.Object)
    {
      if (element.TryGetProperty("phase", out var phaseProp) && phaseProp.ValueKind == JsonValueKind.String)
      {
        result.Phase = phaseProp.GetString();
      }

      if (element.TryGetProperty("score", out var scoreProp) && scoreProp.ValueKind == JsonValueKind.Number)
      {
        result.Score = scoreProp.GetInt32();
      }

      if (element.TryGetProperty("answers", out var answersProp) && answersProp.ValueKind == JsonValueKind.Object)
      {
        foreach (var answerProperty in answersProp.EnumerateObject())
        {
          if (answerProperty.Value.ValueKind == JsonValueKind.Number)
          {
            result.Selections.Add(new StoryRecallAttemptSelection
            {
              RecallQuestionId = answerProperty.Name,
              SelectedOption = answerProperty.Value.GetInt32()
            });
          }
        }
      }
    }

    return result;
  }

  private object BuildAnswerObject(QuizAttemptAnswer answer)
  {
    switch (answer.QuestionType)
    {
      case "magic_backpack":
        {
          var detail = answer.MagicBackpackAttemptAnswer;
          return new Dictionary<string, object>
          {
            ["completed"] = true,
            ["isSuccess"] = detail?.IsSuccess,
            ["selections"] = detail?.Selections.OrderBy(s => s.Order).Select(s => s.ItemId).ToList() ?? new List<string>()
          };
        }
      case "word_bridge":
        {
          var detail = answer.WordBridgeAttemptAnswer;
          return new Dictionary<string, object>
          {
            ["completed"] = true,
            ["success"] = detail?.Success ?? false,
            ["attempts"] = detail?.Attempts ?? 0,
            ["timeMs"] = detail?.TimeMs ?? 0,
            ["targetWord"] = detail?.TargetWord ?? string.Empty
          };
        }
      case "story_recall":
        {
          var detail = answer.StoryRecallAttemptAnswer;
          var selections = detail?.Selections.ToDictionary(s => s.RecallQuestionId, s => s.SelectedOption) ?? new Dictionary<string, int>();
          return new Dictionary<string, object>
          {
            ["phase"] = detail?.Phase ?? string.Empty,
            ["score"] = detail?.Score ?? 0,
            ["answers"] = selections
          };
        }
    }

    return new Dictionary<string, object>();
  }
}
