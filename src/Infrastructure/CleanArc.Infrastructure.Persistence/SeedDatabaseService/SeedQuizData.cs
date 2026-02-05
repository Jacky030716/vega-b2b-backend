using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.SeedDatabaseService;

public interface ISeedQuizData
{
    Task Seed();
}

public class SeedQuizData(ApplicationDbContext dbContext) : ISeedQuizData
{
    public async Task Seed()
    {
        if (await dbContext.Quizzes.AsNoTracking().AnyAsync())
        {
            var hasNormalizedData = await dbContext.MagicBackpackQuestions.AsNoTracking().AnyAsync();
            if (hasNormalizedData)
            {
                return;
            }

            dbContext.MagicBackpackAttemptSelections.RemoveRange(dbContext.MagicBackpackAttemptSelections);
            dbContext.MagicBackpackAttemptAnswers.RemoveRange(dbContext.MagicBackpackAttemptAnswers);
            dbContext.WordBridgeAttemptAnswers.RemoveRange(dbContext.WordBridgeAttemptAnswers);
            dbContext.StoryRecallAttemptSelections.RemoveRange(dbContext.StoryRecallAttemptSelections);
            dbContext.StoryRecallAttemptAnswers.RemoveRange(dbContext.StoryRecallAttemptAnswers);
            dbContext.QuizAttemptAnswers.RemoveRange(dbContext.QuizAttemptAnswers);
            dbContext.QuizAttempts.RemoveRange(dbContext.QuizAttempts);

            dbContext.StoryRecallOptions.RemoveRange(dbContext.StoryRecallOptions);
            dbContext.StoryRecallItems.RemoveRange(dbContext.StoryRecallItems);
            dbContext.StoryRecallQuestions.RemoveRange(dbContext.StoryRecallQuestions);
            dbContext.WordBridgeQuestions.RemoveRange(dbContext.WordBridgeQuestions);
            dbContext.MagicBackpackSequences.RemoveRange(dbContext.MagicBackpackSequences);
            dbContext.MagicBackpackItems.RemoveRange(dbContext.MagicBackpackItems);
            dbContext.MagicBackpackQuestions.RemoveRange(dbContext.MagicBackpackQuestions);
            dbContext.QuizQuestions.RemoveRange(dbContext.QuizQuestions);
            dbContext.Quizzes.RemoveRange(dbContext.Quizzes);

            dbContext.GameThemeGradients.RemoveRange(dbContext.GameThemeGradients);
            dbContext.GameThemeItems.RemoveRange(dbContext.GameThemeItems);
            dbContext.GameThemes.RemoveRange(dbContext.GameThemes);
            dbContext.GameDifficulties.RemoveRange(dbContext.GameDifficulties);
            dbContext.GameConfigs.RemoveRange(dbContext.GameConfigs);
            dbContext.GameCatalogs.RemoveRange(dbContext.GameCatalogs);
            await dbContext.SaveChangesAsync();
        }

        var magicBackpackQuiz = new Quiz
        {
            QuizId = "magic_backpack_001",
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
            Questions = new List<QuizQuestion>
            {
                new()
                {
                    QuestionId = "mb_1",
                    Type = "magic_backpack",
                    QuestionText = "Pack it right! Remember which items go into the backpack.",
                    Points = 50,
                    Explanation = "Pack items correctly to build vocabulary and memory strength!",
                    MagicBackpackQuestion = new MagicBackpackQuestion
                    {
                        Theme = "School",
                        AgeGroup = "kindergarten",
                        Items = new List<MagicBackpackItem>
                        {
                            new() { ItemId = "pencil", Name = "Pencil", Emoji = "✏️" },
                            new() { ItemId = "ruler", Name = "Ruler", Emoji = "📏" },
                            new() { ItemId = "apple", Name = "Apple", Emoji = "🍎" },
                            new() { ItemId = "notebook", Name = "Notebook", Emoji = "📒" },
                            new() { ItemId = "eraser", Name = "Eraser", Emoji = "🧽" }
                        },
                        Sequence = new List<MagicBackpackSequence>
                        {
                            new() { ItemId = "pencil", Order = 0 },
                            new() { ItemId = "apple", Order = 1 },
                            new() { ItemId = "ruler", Order = 2 }
                        }
                    }
                }
            }
        };

        var wordBridgeQuiz = new Quiz
        {
            QuizId = "word_bridge_001",
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
            Questions = new List<QuizQuestion>
            {
                new()
                {
                    QuestionId = "wb_1",
                    Type = "word_bridge",
                    QuestionText = "🌉 Spell the Animal: TIGER",
                    Points = 30,
                    Explanation = "Great spelling! Tigers are amazing big cats.",
                    WordBridgeQuestion = new WordBridgeQuestion
                    {
                        TargetWord = "TIGER",
                        Translation = "Harimau",
                        Difficulty = "easy",
                        ImageUrl = string.Empty
                    }
                },
                new()
                {
                    QuestionId = "wb_2",
                    Type = "word_bridge",
                    QuestionText = "🌉 Spell the Fruit: APPLE",
                    Points = 30,
                    Explanation = "Delicious! An apple a day keeps the doctor away.",
                    WordBridgeQuestion = new WordBridgeQuestion
                    {
                        TargetWord = "APPLE",
                        Translation = "Epal",
                        Difficulty = "easy",
                        ImageUrl = string.Empty
                    }
                }
            }
        };

        var storyRecallQuiz = new Quiz
        {
            QuizId = "story_recall_001",
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
            Questions = new List<QuizQuestion>
            {
                new()
                {
                    QuestionId = "story1",
                    Type = "story_recall",
                    QuestionText = "📚 Story Time: The Little Bear's Adventure",
                    Points = 50,
                    Explanation = string.Empty,
                    StoryRecallQuestion = new StoryRecallQuestion
                    {
                        Theme = "bear_adventure",
                        StoryAudioUrl = "/assets/audio/bear_adventure.mp3",
                        StoryText = "A little bear went on an adventure...",
                        RecallQuestions = new List<StoryRecallItem>
                        {
                            new()
                            {
                                RecallQuestionId = "q1",
                                QuestionText = "Where did the bear go?",
                                CorrectAnswer = 0,
                                Options = new List<StoryRecallOption>
                                {
                                    new() { Order = 0, Text = "Forest" },
                                    new() { Order = 1, Text = "Beach" },
                                    new() { Order = 2, Text = "City" },
                                    new() { Order = 3, Text = "Mountain" }
                                }
                            }
                        }
                    }
                }
            }
        };

        var gameCatalogs = new List<GameCatalog>
        {
            new() { Key = "magic_backpack", Label = "Magic Backpack", QuestionType = "magic_backpack" },
            new() { Key = "word_bridge", Label = "Word Bridge", QuestionType = "word_bridge" },
            new() { Key = "story_recall", Label = "Story Recall", QuestionType = "story_recall" },
            new() { Key = "picture_word_association", Label = "Picture Word Association", QuestionType = "picture_word_association" },
            new() { Key = "translation", Label = "Translation", QuestionType = "translation" }
        };

        var magicBackpackConfig = new GameConfig
        {
            GameType = "magic_backpack",
            DefaultAgeGroup = "kindergarten",
            DefaultThemeId = "school",
            DefaultDifficulty = "easy",
            DefaultRounds = 5,
            DefaultStartingDifficulty = 1,
            Themes = new List<GameTheme>
            {
                new()
                {
                    ThemeId = "school",
                    Name = "School",
                    Emoji = "🎒",
                    Description = "School supplies and classroom items",
                    Gradients = new List<GameThemeGradient>
                    {
                        new() { Order = 0, Color = "#6366F1" },
                        new() { Order = 1, Color = "#4F46E5" }
                    },
                    Items = new List<GameThemeItem>
                    {
                        new() { ItemId = "pencil", Name = "Pencil", Emoji = "✏️" },
                        new() { ItemId = "ruler", Name = "Ruler", Emoji = "📏" },
                        new() { ItemId = "apple", Name = "Apple", Emoji = "🍎" },
                        new() { ItemId = "notebook", Name = "Notebook", Emoji = "📒" },
                        new() { ItemId = "eraser", Name = "Eraser", Emoji = "🧽" }
                    }
                },
                new()
                {
                    ThemeId = "camping",
                    Name = "Camping",
                    Emoji = "🏕️",
                    Description = "Camping and outdoor items",
                    Gradients = new List<GameThemeGradient>
                    {
                        new() { Order = 0, Color = "#10B981" },
                        new() { Order = 1, Color = "#059669" }
                    },
                    Items = new List<GameThemeItem>
                    {
                        new() { ItemId = "tent", Name = "Tent", Emoji = "⛺" },
                        new() { ItemId = "fire", Name = "Campfire", Emoji = "🔥" },
                        new() { ItemId = "flashlight", Name = "Flashlight", Emoji = "🔦" },
                        new() { ItemId = "map", Name = "Map", Emoji = "🗺️" },
                        new() { ItemId = "water", Name = "Water", Emoji = "💧" }
                    }
                }
            },
            DifficultyLevels = new List<GameDifficulty>
            {
                new() { Level = 1, Name = "Easy", SequenceLength = 3, Speed = 800, GhostMode = false, Description = "Short sequences" },
                new() { Level = 2, Name = "Medium", SequenceLength = 4, Speed = 650, GhostMode = false, Description = "Moderate sequences" },
                new() { Level = 3, Name = "Hard", SequenceLength = 5, Speed = 500, GhostMode = true, Description = "Faster with ghost mode" }
            }
        };

        var wordBridgeConfig = new GameConfig
        {
            GameType = "word_bridge",
            DefaultAgeGroup = "primary",
            DefaultThemeId = string.Empty,
            DefaultDifficulty = "easy",
            DefaultRounds = 0,
            DefaultStartingDifficulty = 1,
            Themes = new List<GameTheme>(),
            DifficultyLevels = new List<GameDifficulty>
            {
                new() { Level = 1, Name = "Easy", SequenceLength = 0, Speed = 0, GhostMode = false, Description = "Short words and hints" },
                new() { Level = 2, Name = "Hard", SequenceLength = 0, Speed = 0, GhostMode = false, Description = "Longer words and fewer hints" }
            }
        };

        await dbContext.Quizzes.AddRangeAsync(magicBackpackQuiz, wordBridgeQuiz, storyRecallQuiz);
        await dbContext.GameCatalogs.AddRangeAsync(gameCatalogs);
        await dbContext.GameConfigs.AddRangeAsync(magicBackpackConfig, wordBridgeConfig);

        await dbContext.SaveChangesAsync();
    }
}
