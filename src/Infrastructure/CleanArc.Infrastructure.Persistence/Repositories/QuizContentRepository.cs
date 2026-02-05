using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Features.Games.Models;
using CleanArc.Application.Features.Quizzes.Models;
using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Repositories;

internal class QuizContentRepository(ApplicationDbContext dbContext) : IQuizContentRepository
{

  public async Task<IReadOnlyList<QuizSummaryDto>> GetQuizzesAsync(string type, string gameType)
  {
    var query = dbContext.Quizzes.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(type))
    {
      query = query.Where(q => q.Category == type || q.Type == type);
    }

    if (!string.IsNullOrWhiteSpace(gameType))
    {
      query = query.Where(q => q.Questions.Any(qq => qq.Type == gameType));
    }

    var quizzes = await query
        .Select(q => new QuizSummaryDto
        {
          Id = q.QuizId,
          Title = q.Title,
          Description = q.Description,
          Theme = q.Theme,
          Type = q.Type,
          Difficulty = q.Difficulty,
          EstimatedTime = q.EstimatedTime,
          TotalPoints = q.TotalPoints,
          CreatedAt = q.CreatedAt,
          ImageUrl = q.ImageUrl,
          Category = q.Category,
          QuestionCount = q.Questions.Count
        })
        .ToListAsync();

    return quizzes;
  }

  public async Task<QuizDetailDto> GetQuizByIdAsync(string quizId)
  {
    var quiz = await dbContext.Quizzes
        .AsNoTracking()
        .Include(q => q.Questions)
            .ThenInclude(q => q.MagicBackpackQuestion)
                .ThenInclude(m => m.Items)
        .Include(q => q.Questions)
            .ThenInclude(q => q.MagicBackpackQuestion)
                .ThenInclude(m => m.Sequence)
        .Include(q => q.Questions)
            .ThenInclude(q => q.WordBridgeQuestion)
        .Include(q => q.Questions)
            .ThenInclude(q => q.StoryRecallQuestion)
                .ThenInclude(s => s.RecallQuestions)
                    .ThenInclude(r => r.Options)
        .FirstOrDefaultAsync(q => q.QuizId == quizId);

    if (quiz == null)
    {
      return null;
    }

    return new QuizDetailDto
    {
      Id = quiz.QuizId,
      Title = quiz.Title,
      Description = quiz.Description,
      Theme = quiz.Theme,
      Type = quiz.Type,
      Difficulty = quiz.Difficulty,
      EstimatedTime = quiz.EstimatedTime,
      TotalPoints = quiz.TotalPoints,
      CreatedAt = quiz.CreatedAt,
      ImageUrl = quiz.ImageUrl,
      Category = quiz.Category,
      QuestionCount = quiz.Questions.Count,
      Questions = quiz.Questions.Select(BuildQuestionObject).ToList()
    };
  }

  public async Task<IReadOnlyList<object>> GetQuizQuestionsAsync(string quizId)
  {
    var questions = await dbContext.QuizQuestions
        .AsNoTracking()
        .Include(q => q.MagicBackpackQuestion)
            .ThenInclude(m => m.Items)
        .Include(q => q.MagicBackpackQuestion)
            .ThenInclude(m => m.Sequence)
        .Include(q => q.WordBridgeQuestion)
        .Include(q => q.StoryRecallQuestion)
            .ThenInclude(s => s.RecallQuestions)
                .ThenInclude(r => r.Options)
        .Where(q => q.Quiz.QuizId == quizId)
        .ToListAsync();

    return questions.Select(BuildQuestionObject).ToList();
  }

  public async Task<string> GetQuestionTypeAsync(string quizId, string questionId)
  {
    var question = await dbContext.QuizQuestions
        .AsNoTracking()
        .Where(q => q.Quiz.QuizId == quizId && q.QuestionId == questionId)
        .Select(q => q.Type)
        .FirstOrDefaultAsync();

    return question ?? string.Empty;
  }

  public async Task<IReadOnlyList<GameCatalogItemDto>> GetGameCatalogAsync()
  {
    var items = await dbContext.GameCatalogs
        .AsNoTracking()
        .Select(c => new GameCatalogItemDto
        {
          Key = c.Key,
          Label = c.Label,
          QuestionType = c.QuestionType
        })
        .ToListAsync();

    return items;
  }

  public async Task<GameConfigDto> GetGameConfigAsync(string gameType)
  {
    var config = await dbContext.GameConfigs
        .AsNoTracking()
        .Include(c => c.Themes)
            .ThenInclude(t => t.Items)
        .Include(c => c.Themes)
            .ThenInclude(t => t.Gradients)
        .Include(c => c.DifficultyLevels)
        .FirstOrDefaultAsync(c => c.GameType == gameType);

    if (config == null)
    {
      return null;
    }

    return new GameConfigDto
    {
      GameType = config.GameType,
      Themes = config.Themes.Select(t => new GameThemeDto
      {
        Id = t.ThemeId,
        Name = t.Name,
        Emoji = t.Emoji,
        Description = t.Description,
        BgGradient = t.Gradients.OrderBy(g => g.Order).Select(g => g.Color).ToList(),
        Items = t.Items.Select(i => new GameItemDto
        {
          Id = i.ItemId,
          Name = i.Name,
          Emoji = i.Emoji
        }).ToList()
      }).ToList(),
      DifficultyLevels = config.DifficultyLevels.Select(d => new GameDifficultyDto
      {
        Level = d.Level,
        Name = d.Name,
        SequenceLength = d.SequenceLength,
        Speed = d.Speed,
        GhostMode = d.GhostMode,
        Description = d.Description
      }).ToList(),
      Defaults = new GameDefaultsDto
      {
        AgeGroup = config.DefaultAgeGroup,
        ThemeId = config.DefaultThemeId,
        Difficulty = config.DefaultDifficulty,
        Rounds = config.DefaultRounds,
        StartingDifficulty = config.DefaultStartingDifficulty
      }
    };
  }

  private object BuildQuestionObject(QuizQuestion question)
  {
    var payload = new Dictionary<string, object>
    {
      ["id"] = question.QuestionId,
      ["type"] = question.Type,
      ["question"] = question.QuestionText,
      ["points"] = question.Points
    };

    if (!string.IsNullOrWhiteSpace(question.Explanation))
    {
      payload["explanation"] = question.Explanation;
    }

    switch (question.Type)
    {
      case "magic_backpack":
        {
          var detail = question.MagicBackpackQuestion;
          if (detail != null)
          {
            payload["theme"] = detail.Theme;
            payload["ageGroup"] = detail.AgeGroup;
            payload["items"] = detail.Items.Select(i => new Dictionary<string, object>
            {
              ["id"] = i.ItemId,
              ["name"] = i.Name,
              ["emoji"] = i.Emoji
            }).ToList();
            payload["sequence"] = detail.Sequence.OrderBy(s => s.Order).Select(s => s.ItemId).ToList();
          }

          break;
        }
      case "word_bridge":
        {
          var detail = question.WordBridgeQuestion;
          if (detail != null)
          {
            payload["targetWord"] = detail.TargetWord;
            payload["translation"] = detail.Translation;
            payload["difficulty"] = detail.Difficulty;
            if (!string.IsNullOrWhiteSpace(detail.ImageUrl))
            {
              payload["imageUrl"] = detail.ImageUrl;
            }
          }

          break;
        }
      case "story_recall":
        {
          var detail = question.StoryRecallQuestion;
          if (detail != null)
          {
            payload["theme"] = detail.Theme;
            payload["storyAudioUrl"] = detail.StoryAudioUrl;
            payload["storyText"] = detail.StoryText;
            payload["recallQuestions"] = detail.RecallQuestions.Select(r => new Dictionary<string, object>
            {
              ["id"] = r.RecallQuestionId,
              ["question"] = r.QuestionText,
              ["options"] = r.Options.OrderBy(o => o.Order).Select(o => o.Text).ToList(),
              ["correctAnswer"] = r.CorrectAnswer
            }).ToList();
          }

          break;
        }
    }

    return payload;
  }
}
