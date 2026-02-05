using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Features.Games.Models;
using CleanArc.Application.Features.Quizzes.Models;

namespace CleanArc.Infrastructure.Persistence.Repositories;

internal class InMemoryQuizContentRepository : IQuizContentRepository
{
  private static readonly List<QuizDefinition> Quizzes = BuildQuizzes();
  private static readonly List<GameCatalogItemDto> GameCatalog = BuildGameCatalog();
  private static readonly Dictionary<string, GameConfigDto> GameConfigs = BuildGameConfigs();

  public Task<IReadOnlyList<QuizSummaryDto>> GetQuizzesAsync(string type, string gameType)
  {
    var list = Quizzes.AsEnumerable();

    if (!string.IsNullOrWhiteSpace(type))
    {
      list = list.Where(q => string.Equals(q.Summary.Category, type, StringComparison.OrdinalIgnoreCase)
                             || string.Equals(q.Summary.Type, type, StringComparison.OrdinalIgnoreCase));
    }

    if (!string.IsNullOrWhiteSpace(gameType))
    {
      list = list.Where(q => q.QuestionTypes.Contains(gameType, StringComparer.OrdinalIgnoreCase));
    }

    return Task.FromResult<IReadOnlyList<QuizSummaryDto>>(list.Select(q => q.Summary).ToList());
  }

  public Task<QuizDetailDto> GetQuizByIdAsync(string quizId)
  {
    var quiz = Quizzes.FirstOrDefault(q => q.Summary.Id.Equals(quizId, StringComparison.OrdinalIgnoreCase));
    if (quiz is null)
    {
      return Task.FromResult<QuizDetailDto>(null);
    }

    var detail = new QuizDetailDto
    {
      Id = quiz.Summary.Id,
      Title = quiz.Summary.Title,
      Description = quiz.Summary.Description,
      Theme = quiz.Summary.Theme,
      Type = quiz.Summary.Type,
      Difficulty = quiz.Summary.Difficulty,
      EstimatedTime = quiz.Summary.EstimatedTime,
      TotalPoints = quiz.Summary.TotalPoints,
      CreatedAt = quiz.Summary.CreatedAt,
      ImageUrl = quiz.Summary.ImageUrl,
      Category = quiz.Summary.Category,
      QuestionCount = quiz.Summary.QuestionCount,
      Questions = quiz.Questions
    };

    return Task.FromResult<QuizDetailDto>(detail);
  }

  public Task<IReadOnlyList<object>> GetQuizQuestionsAsync(string quizId)
  {
    var quiz = Quizzes.FirstOrDefault(q => q.Summary.Id.Equals(quizId, StringComparison.OrdinalIgnoreCase));
    return Task.FromResult<IReadOnlyList<object>>(quiz?.Questions ?? new List<object>());
  }

  public Task<string> GetQuestionTypeAsync(string quizId, string questionId)
  {
    var quiz = Quizzes.FirstOrDefault(q => q.Summary.Id.Equals(quizId, StringComparison.OrdinalIgnoreCase));
    if (quiz == null)
    {
      return Task.FromResult(string.Empty);
    }

    foreach (var question in quiz.Questions)
    {
      if (question is Dictionary<string, object> dict && dict.TryGetValue("id", out var idValue))
      {
        if (string.Equals(idValue?.ToString(), questionId, StringComparison.OrdinalIgnoreCase) && dict.TryGetValue("type", out var typeValue))
        {
          return Task.FromResult(typeValue?.ToString() ?? string.Empty);
        }
      }
    }

    return Task.FromResult(string.Empty);
  }

  public Task<IReadOnlyList<GameCatalogItemDto>> GetGameCatalogAsync()
  {
    return Task.FromResult<IReadOnlyList<GameCatalogItemDto>>(GameCatalog);
  }

  public Task<GameConfigDto> GetGameConfigAsync(string gameType)
  {
    if (GameConfigs.TryGetValue(gameType, out var config))
    {
      return Task.FromResult<GameConfigDto>(config);
    }

    return Task.FromResult<GameConfigDto>(null);
  }

  private static List<GameCatalogItemDto> BuildGameCatalog()
  {
    return new List<GameCatalogItemDto>
        {
            new() { Key = "magic_backpack", Label = "Magic Backpack", QuestionType = "magic_backpack" },
            new() { Key = "word_bridge", Label = "Word Bridge", QuestionType = "word_bridge" },
            new() { Key = "story_recall", Label = "Story Recall", QuestionType = "story_recall" },
            new() { Key = "picture_word_association", Label = "Picture Word Association", QuestionType = "picture_word_association" },
            new() { Key = "translation", Label = "Translation", QuestionType = "translation" }
        };
  }

  private static Dictionary<string, GameConfigDto> BuildGameConfigs()
  {
    var magicBackpack = new GameConfigDto
    {
      GameType = "magic_backpack",
      Themes = new List<GameThemeDto>
            {
                new()
                {
                    Id = "school",
                    Name = "School",
                    Emoji = "🎒",
                    Description = "School supplies and classroom items",
                    BgGradient = new List<string> { "#6366F1", "#4F46E5" },
                    Items = new List<GameItemDto>
                    {
                        new() { Id = "pencil", Name = "Pencil", Emoji = "✏️" },
                        new() { Id = "ruler", Name = "Ruler", Emoji = "📏" },
                        new() { Id = "apple", Name = "Apple", Emoji = "🍎" },
                        new() { Id = "notebook", Name = "Notebook", Emoji = "📒" },
                        new() { Id = "eraser", Name = "Eraser", Emoji = "🧽" }
                    }
                },
                new()
                {
                    Id = "camping",
                    Name = "Camping",
                    Emoji = "🏕️",
                    Description = "Camping and outdoor items",
                    BgGradient = new List<string> { "#10B981", "#059669" },
                    Items = new List<GameItemDto>
                    {
                        new() { Id = "tent", Name = "Tent", Emoji = "⛺" },
                        new() { Id = "fire", Name = "Campfire", Emoji = "🔥" },
                        new() { Id = "flashlight", Name = "Flashlight", Emoji = "🔦" },
                        new() { Id = "map", Name = "Map", Emoji = "🗺️" },
                        new() { Id = "water", Name = "Water", Emoji = "💧" }
                    }
                }
            },
      DifficultyLevels = new List<GameDifficultyDto>
            {
                new() { Level = 1, Name = "Easy", SequenceLength = 3, Speed = 800, GhostMode = false, Description = "Short sequences" },
                new() { Level = 2, Name = "Medium", SequenceLength = 4, Speed = 650, GhostMode = false, Description = "Moderate sequences" },
                new() { Level = 3, Name = "Hard", SequenceLength = 5, Speed = 500, GhostMode = true, Description = "Faster with ghost mode" }
            },
      Defaults = new GameDefaultsDto
      {
        AgeGroup = "kindergarten",
        ThemeId = "school",
        Difficulty = "easy",
        Rounds = 5,
        StartingDifficulty = 1
      }
    };

    var wordBridge = new GameConfigDto
    {
      GameType = "word_bridge",
      Themes = new List<GameThemeDto>(),
      DifficultyLevels = new List<GameDifficultyDto>
            {
                new() { Level = 1, Name = "Easy", SequenceLength = 0, Speed = 0, GhostMode = false, Description = "Short words and hints" },
                new() { Level = 2, Name = "Hard", SequenceLength = 0, Speed = 0, GhostMode = false, Description = "Longer words and fewer hints" }
            },
      Defaults = new GameDefaultsDto
      {
        AgeGroup = "primary",
        ThemeId = "",
        Difficulty = "easy",
        Rounds = 0,
        StartingDifficulty = 1
      }
    };

    return new Dictionary<string, GameConfigDto>(StringComparer.OrdinalIgnoreCase)
    {
      ["magic_backpack"] = magicBackpack,
      ["word_bridge"] = wordBridge
    };
  }

  private static List<QuizDefinition> BuildQuizzes()
  {
    var magicBackpackQuestions = new List<object>
        {
            new Dictionary<string, object>
            {
                ["id"] = "mb_1",
                ["type"] = "magic_backpack",
                ["question"] = "Pack it right! Remember which items go into the backpack.",
                ["points"] = 50,
                ["theme"] = "School",
                ["ageGroup"] = "kindergarten",
                ["items"] = new List<Dictionary<string, object>>
                {
                    new() { ["id"] = "pencil", ["name"] = "Pencil", ["emoji"] = "✏️" },
                    new() { ["id"] = "ruler", ["name"] = "Ruler", ["emoji"] = "📏" },
                    new() { ["id"] = "apple", ["name"] = "Apple", ["emoji"] = "🍎" },
                    new() { ["id"] = "notebook", ["name"] = "Notebook", ["emoji"] = "📒" },
                    new() { ["id"] = "eraser", ["name"] = "Eraser", ["emoji"] = "🧽" }
                },
                ["sequence"] = new List<string> { "pencil", "apple", "ruler" },
                ["explanation"] = "Pack items correctly to build vocabulary and memory strength!"
            }
        };

    var wordBridgeQuestions = new List<object>
        {
            new Dictionary<string, object>
            {
                ["id"] = "wb_1",
                ["type"] = "word_bridge",
                ["question"] = "🌉 Spell the Animal: TIGER",
                ["points"] = 30,
                ["targetWord"] = "TIGER",
                ["translation"] = "Harimau",
                ["difficulty"] = "easy",
                ["explanation"] = "Great spelling! Tigers are amazing big cats."
            },
            new Dictionary<string, object>
            {
                ["id"] = "wb_2",
                ["type"] = "word_bridge",
                ["question"] = "🌉 Spell the Fruit: APPLE",
                ["points"] = 30,
                ["targetWord"] = "APPLE",
                ["translation"] = "Epal",
                ["difficulty"] = "easy",
                ["explanation"] = "Delicious! An apple a day keeps the doctor away."
            }
        };

    var storyRecallQuestions = new List<object>
        {
            new Dictionary<string, object>
            {
                ["id"] = "story1",
                ["type"] = "story_recall",
                ["question"] = "📚 Story Time: The Little Bear's Adventure",
                ["points"] = 50,
                ["theme"] = "bear_adventure",
                ["storyAudioUrl"] = "/assets/audio/bear_adventure.mp3",
                ["storyText"] = "A little bear went on an adventure...",
                ["recallQuestions"] = new List<Dictionary<string, object>>
                {
                    new()
                    {
                        ["id"] = "q1",
                        ["question"] = "Where did the bear go?",
                        ["options"] = new List<string> { "Forest", "Beach", "City", "Mountain" },
                        ["correctAnswer"] = 0
                    }
                }
            }
        };

    var quizzes = new List<QuizDefinition>
        {
            new()
            {
                Summary = new QuizSummaryDto
                {
                    Id = "magic_backpack_001",
                    Title = "Magic Backpack: Pack & Remember",
                    Description = "Watch as items drop into the backpack, then select the items in the same order.",
                    Theme = "School",
                    Type = "default",
                    Difficulty = "beginner",
                    EstimatedTime = 6,
                    TotalPoints = 50,
                    CreatedAt = "2025-01-01T00:00:00Z",
                    ImageUrl = "https://example.com/images/magic_backpack.png",
                    Category = "memory",
                    QuestionCount = magicBackpackQuestions.Count
                },
                Questions = magicBackpackQuestions,
                QuestionTypes = new List<string> { "magic_backpack" }
            },
            new()
            {
                Summary = new QuizSummaryDto
                {
                    Id = "word_bridge_001",
                    Title = "Word Builder Bridge: Spell & Learn",
                    Description = "Drag letters to build words! Practice spelling while crossing the bridge.",
                    Theme = "Spelling & Vocabulary",
                    Type = "default",
                    Difficulty = "beginner",
                    EstimatedTime = 10,
                    TotalPoints = 150,
                    CreatedAt = "2024-01-01T00:00:00Z",
                    ImageUrl = "https://example.com/images/word_bridge.png",
                    Category = "memory",
                    QuestionCount = wordBridgeQuestions.Count
                },
                Questions = wordBridgeQuestions,
                QuestionTypes = new List<string> { "word_bridge", "memory_match" }
            },
            new()
            {
                Summary = new QuizSummaryDto
                {
                    Id = "story_recall_001",
                    Title = "Story Recall: Memory Adventures",
                    Description = "Listen to stories and test your memory! Improve comprehension and recall skills.",
                    Theme = "Story Comprehension Training",
                    Type = "default",
                    Difficulty = "beginner",
                    EstimatedTime = 15,
                    TotalPoints = 100,
                    CreatedAt = "2024-01-01T00:00:00Z",
                    ImageUrl = "https://example.com/images/story_recall.png",
                    Category = "memory",
                    QuestionCount = storyRecallQuestions.Count
                },
                Questions = storyRecallQuestions,
                QuestionTypes = new List<string> { "story_recall" }
            }
        };

    return quizzes;
  }

  private class QuizDefinition
  {
    public QuizSummaryDto Summary { get; set; }
    public IReadOnlyList<object> Questions { get; set; } = Array.Empty<object>();
    public List<string> QuestionTypes { get; set; } = new();
  }
}
