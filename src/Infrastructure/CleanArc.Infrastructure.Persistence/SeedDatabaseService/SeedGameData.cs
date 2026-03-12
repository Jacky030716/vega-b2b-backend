using System.Text.Json;
using CleanArc.Domain.Entities.Achievement;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.Quiz;
using CleanArc.Domain.Entities.Quiz.Content;
using CleanArc.Domain.Entities.User;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.SeedDatabaseService;

public class SeedGameData : ISeedGameData
{
    private readonly ApplicationDbContext _dbContext;

    // Serialize contentData as camelCase so it matches the frontend TypeScript types.
    // e.g. WordBridgeContent.Words → {"words":[...]} not {"Words":[...]}
    private static readonly JsonSerializerOptions _camelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SeedGameData(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Seed()
    {
        if (!await _dbContext.Games.AnyAsync())
        {
            var magicBackpackGame = new Game
            {
                Key = "magic_backpack",
                Name = "Magic Backpack: Pack & Remember",
                Description = "Watch as items drop into the backpack, then select the items in the same order.",
                ImageUrl = "https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/thumbnails%2Fbackpack.png?alt=media",
                Category = "School",
                SkillsTaught = "memory"
            };

            var wordBridgeGame = new Game
            {
                Key = "word_bridge",
                Name = "Word Builder Bridge: Spell & Learn",
                Description = "Drag letters to build words! Practice spelling while crossing the bridge.",
                ImageUrl = "https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/thumbnails%2Fbuilder.png?alt=media",
                Category = "Spelling & Vocabulary",
                SkillsTaught = "spelling"
            };

            var storyRecallGame = new Game
            {
                Key = "story_recall",
                Name = "Word Twins",
                Description = "Flip the cards and match the word with the image!",
                ImageUrl = "https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/thumbnails%2Fwordpair.png?alt=media",
                Category = "Story Comprehension Training",
                SkillsTaught = "memory"
            };

            await _dbContext.Games.AddRangeAsync(magicBackpackGame, wordBridgeGame, storyRecallGame);
            await _dbContext.SaveChangesAsync();

            // ── Magic Backpack: 3 adventure map nodes ──────────────────────────
            await _dbContext.Challenges.AddRangeAsync(
                new Challenge
                {
                    GameId = magicBackpackGame.Id,
                    Title = "Remember 3 Items",
                    Description = "Watch 3 school items drop in, then recall them in order!",
                    DifficultyLevel = 1,
                    OrderIndex = 1,
                    MaxStars = 3,
                    ContentData = JsonSerializer.Serialize(new
                    {
                        theme = "school",
                        sequenceLength = 3,
                        items = new[] { "Pencil", "Eraser", "Notebook" }
                    }, _camelCase),
                    IsAIGenerated = false
                },
                new Challenge
                {
                    GameId = magicBackpackGame.Id,
                    Title = "Remember 4 Items",
                    Description = "Step it up — 4 items in the backpack. Can you pack them all?",
                    DifficultyLevel = 2,
                    OrderIndex = 2,
                    MaxStars = 3,
                    ContentData = JsonSerializer.Serialize(new
                    {
                        theme = "camping",
                        sequenceLength = 4,
                        items = new[] { "Tent", "Flashlight", "Sleeping Bag", "Water Bottle" }
                    }, _camelCase),
                    IsAIGenerated = false
                },
                new Challenge
                {
                    GameId = magicBackpackGame.Id,
                    Title = "Speed Round",
                    Description = "5 items, shorter display time. Stay sharp!",
                    DifficultyLevel = 3,
                    OrderIndex = 3,
                    MaxStars = 3,
                    ContentData = JsonSerializer.Serialize(new
                    {
                        theme = "school",
                        sequenceLength = 5,
                        ghostMode = true,
                        items = new[] { "Ruler", "Scissors", "Glue", "Pencil Case", "Highlighter" }
                    }, _camelCase),
                    IsAIGenerated = false
                }
            );

            // ── Word Bridge: 3 adventure map nodes ────────────────────────────
            // Uses strongly-typed WordBridgeContent so schema is enforced in C#.
            // ImageRef stores a Firebase Storage relative path, e.g. "quizzes/word-bridge/cat.jpg".
            // Leave ImageRef null until you upload images to Firebase Storage.
            await _dbContext.Challenges.AddRangeAsync(
                new Challenge
                {
                    GameId = wordBridgeGame.Id,
                    Title = "Short Words",
                    Description = "Build simple 3-letter words by dragging the right letters.",
                    DifficultyLevel = 1,
                    OrderIndex = 1,
                    MaxStars = 3,
                    ContentData = JsonSerializer.Serialize(new WordBridgeContent
                    {
                        Words = new List<WordBridgeWord>
                        {
                            new() { Target = "CAT", Translation = "A small furry animal",      Difficulty = "easy", ImageRef = "quizzes/word-bridge/cat.jpg" },
                            new() { Target = "DOG", Translation = "Man's best friend",         Difficulty = "easy", ImageRef = "quizzes/word-bridge/dog.jpg" },
                            new() { Target = "SUN", Translation = "The star at the center",    Difficulty = "easy", ImageRef = "quizzes/word-bridge/sun.jpg" }
                        }
                    }, _camelCase),
                    IsAIGenerated = false
                },
                new Challenge
                {
                    GameId = wordBridgeGame.Id,
                    Title = "Medium Words",
                    Description = "4-5 letter words. Watch out for the vowels!",
                    DifficultyLevel = 2,
                    OrderIndex = 2,
                    MaxStars = 3,
                    ContentData = JsonSerializer.Serialize(new WordBridgeContent
                    {
                        Words = new List<WordBridgeWord>
                        {
                            new() { Target = "RAIN",  Translation = "Water falling from clouds", Difficulty = "easy", ImageRef = null },
                            new() { Target = "CLOUD", Translation = "White puffs in the sky",    Difficulty = "hard", ImageRef = null },
                            new() { Target = "STORM", Translation = "Strong wind and rain",       Difficulty = "hard", ImageRef = null }
                        }
                    }, _camelCase),
                    IsAIGenerated = false
                },
                new Challenge
                {
                    GameId = wordBridgeGame.Id,
                    Title = "Tricky Spelling",
                    Description = "Longer words with tricky letter patterns. Think before you drag!",
                    DifficultyLevel = 3,
                    OrderIndex = 3,
                    MaxStars = 3,
                    ContentData = JsonSerializer.Serialize(new WordBridgeContent
                    {
                        Words = new List<WordBridgeWord>
                        {
                            new() { Target = "BRIDGE", Translation = "Structure crossing water", Difficulty = "hard", ImageRef = null },
                            new() { Target = "SCHOOL", Translation = "Where you learn",           Difficulty = "hard", ImageRef = null },
                            new() { Target = "FRIEND", Translation = "Someone you like",          Difficulty = "hard", ImageRef = null }
                        }
                    }, _camelCase),
                    IsAIGenerated = false
                }
            );

            // ── Story Recall (Word Twins): 3 adventure map nodes ──────────────
            // Uses strongly-typed WordTwinsContent.
            // ImageRef = Firebase Storage path (e.g. "quizzes/word-twins/apple.jpg").
            // ImageKey = local bundled asset key used as offline fallback.
            // When ImageRef is set, the frontend calls getDownloadURL(ref(storage, imageRef)).
            await _dbContext.Challenges.AddRangeAsync(
                new Challenge
                {
                    GameId = storyRecallGame.Id,
                    Title = "4 Pairs",
                    Description = "Match 4 words with their pictures. Flip and find!",
                    DifficultyLevel = 1,
                    OrderIndex = 1,
                    MaxStars = 3,
                    ContentData = JsonSerializer.Serialize(new WordTwinsContent
                    {
                        Pairs = new List<WordTwinsPair>
                        {
                            new() { Word = "Apple",  ImageKey = "apple",  ImageRef = null },
                            new() { Word = "Banana", ImageKey = "banana", ImageRef = null },
                            new() { Word = "Cat",    ImageKey = "cat",    ImageRef = null },
                            new() { Word = "Dog",    ImageKey = "dog",    ImageRef = null }
                        }
                    }, _camelCase),
                    IsAIGenerated = false
                },
                new Challenge
                {
                    GameId = storyRecallGame.Id,
                    Title = "6 Pairs",
                    Description = "6 pairs now — stay focused and remember where they are!",
                    DifficultyLevel = 2,
                    OrderIndex = 2,
                    MaxStars = 3,
                    ContentData = JsonSerializer.Serialize(new WordTwinsContent
                    {
                        Pairs = new List<WordTwinsPair>
                        {
                            new() { Word = "Sun",   ImageKey = "sun",   ImageRef = null },
                            new() { Word = "Moon",  ImageKey = "moon",  ImageRef = null },
                            new() { Word = "Star",  ImageKey = "star",  ImageRef = null },
                            new() { Word = "Rain",  ImageKey = "rain",  ImageRef = null },
                            new() { Word = "Cloud", ImageKey = "cloud", ImageRef = null },
                            new() { Word = "Snow",  ImageKey = "snow",  ImageRef = null }
                        }
                    }, _camelCase),
                    IsAIGenerated = false
                },
                new Challenge
                {
                    GameId = storyRecallGame.Id,
                    Title = "8 Pairs — Speed Match",
                    Description = "8 pairs with a time limit. Match as fast as you can!",
                    DifficultyLevel = 3,
                    OrderIndex = 3,
                    MaxStars = 3,
                    ContentData = JsonSerializer.Serialize(new WordTwinsContent
                    {
                        Pairs = new List<WordTwinsPair>
                        {
                            new() { Word = "Book",    ImageKey = "book",    ImageRef = null },
                            new() { Word = "Pencil",  ImageKey = "pencil",  ImageRef = null },
                            new() { Word = "School",  ImageKey = "school",  ImageRef = null },
                            new() { Word = "Tree",    ImageKey = "tree",    ImageRef = null },
                            new() { Word = "Flower",  ImageKey = "flower",  ImageRef = null },
                            new() { Word = "House",   ImageKey = "house",   ImageRef = null },
                            new() { Word = "Car",     ImageKey = "car",     ImageRef = null },
                            new() { Word = "Bicycle", ImageKey = "bicycle", ImageRef = null }
                        }
                    }, _camelCase),
                    IsAIGenerated = false
                }
            );

            await _dbContext.SaveChangesAsync();
        }

        if (!await _dbContext.ShopItems.AnyAsync())
        {
            var shopItems = new List<CleanArc.Domain.Entities.Shop.ShopItem>
            {
                new() { Name = "Pirate King", Description = "Arr! A swashbuckling pirate bear variant.", Category = "avatar", Price = 800, Currency = "diamonds", ImageUrl = "https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/mascots%2FPirate.png?alt=media", Rarity = "common", IsAvailable = true },
                new() { Name = "Crown", Description = "A regal bear variant fit for a quiz champion.", Category = "avatar", Price = 1500, Currency = "diamonds", ImageUrl = "https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/mascots%2FKing.png?alt=media", Rarity = "rare", IsAvailable = true },
                new() { Name = "Giyu", Description = "Demon Slayer", Category = "avatar", Price = 1750, Currency = "diamonds", ImageUrl = "https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/mascots%2FGiyu.png?alt=media", Rarity = "rare", IsAvailable = true },
                new() { Name = "Rengoku", Description = "Demon Slayer", Category = "avatar", Price = 1850, Currency = "diamonds", ImageUrl = "https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/mascots%2FRengoku.png?alt=media", Rarity = "rare", IsAvailable = true },
                new() { Name = "Inosuke", Description = "Demon Slayer", Category = "avatar", Price = 1500, Currency = "diamonds", ImageUrl = "https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/mascots%2FInosuke.png?alt=media", Rarity = "rare", IsAvailable = true }
            };

            await _dbContext.ShopItems.AddRangeAsync(shopItems);
            await _dbContext.SaveChangesAsync();
        }

        var wordBridge = await _dbContext.Games.FirstOrDefaultAsync(g => g.Key == "word_bridge");
        if (wordBridge is null)
        {
            wordBridge = new Game
            {
                Key = "word_bridge",
                Name = "Word Builder Bridge: Spell & Learn",
                Description = "Drag letters to build words! Practice spelling while crossing the bridge.",
                ImageUrl = "https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/thumbnails%2Fbuilder.png?alt=media",
                Category = "Spelling & Vocabulary",
                SkillsTaught = "spelling"
            };

            await _dbContext.Games.AddAsync(wordBridge);
            await _dbContext.SaveChangesAsync();
        }

        var storyRecall = await _dbContext.Games.FirstOrDefaultAsync(g => g.Key == "story_recall");
        if (storyRecall is null)
        {
            storyRecall = new Game
            {
                Key = "story_recall",
                Name = "Word Twins",
                Description = "Flip the cards and match the word with the image!",
                ImageUrl = "https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/thumbnails%2Fwordpair.png?alt=media",
                Category = "Story Comprehension Training",
                SkillsTaught = "memory"
            };

            await _dbContext.Games.AddAsync(storyRecall);
            await _dbContext.SaveChangesAsync();
        }

        var hasWordBridgeChallenge = await _dbContext.Challenges.AnyAsync(c => c.GameId == wordBridge.Id);
        if (!hasWordBridgeChallenge)
        {
            await _dbContext.Challenges.AddAsync(new Challenge
            {
                GameId = wordBridge.Id,
                Title = "Classroom Objects (Starter)",
                Description = "Build beginner classroom words by dragging letters in order.",
                DifficultyLevel = 1,
                OrderIndex = 1,
                MaxStars = 3,
                ContentData = JsonSerializer.Serialize(new WordBridgeContent
                {
                    Words = new List<WordBridgeWord>
                    {
                        new() { Target = "PEN",  Translation = "A tool used for writing",    Difficulty = "easy", ImageRef = null },
                        new() { Target = "BOOK", Translation = "Something we read to learn", Difficulty = "easy", ImageRef = null },
                        new() { Target = "DESK", Translation = "A table used by students",   Difficulty = "easy", ImageRef = null }
                    }
                }, _camelCase),
                IsAIGenerated = false
            });
        }

        var hasStoryRecallChallenge = await _dbContext.Challenges.AnyAsync(c => c.GameId == storyRecall.Id);
        if (!hasStoryRecallChallenge)
        {
            await _dbContext.Challenges.AddAsync(new Challenge
            {
                GameId = storyRecall.Id,
                Title = "School Word Twins (Starter)",
                Description = "Match each school word with its picture pair.",
                DifficultyLevel = 1,
                OrderIndex = 1,
                MaxStars = 3,
                ContentData = JsonSerializer.Serialize(new WordTwinsContent
                {
                    Pairs = new List<WordTwinsPair>
                    {
                        new() { Word = "Book",   ImageKey = "book",   ImageRef = null },
                        new() { Word = "Pencil", ImageKey = "pencil", ImageRef = null },
                        new() { Word = "School", ImageKey = "school", ImageRef = null },
                        new() { Word = "Tree",   ImageKey = "tree",   ImageRef = null }
                    }
                }, _camelCase),
                IsAIGenerated = false
            });
        }

        if (!hasWordBridgeChallenge || !hasStoryRecallChallenge)
        {
            await _dbContext.SaveChangesAsync();
        }

        // ── Badge catalog ─────────────────────────────────────────────────────
        // ImageRef stores a Firebase Storage relative path.
        // The frontend resolves it with resolveStorageRefToUrl(imageRef).
        if (!await _dbContext.Badges.AnyAsync())
        {
            var badges = new List<Badge>
            {
                // ── Streak Badges ────────────────────────────────────────────
                new()
                {
                    Name        = "Week Warrior",
                    Description = "Check in to the app for 7 days in a row.",
                    ImageRef    = "badges/7_day_streak.png",
                    Category    = "streak",
                    Rarity      = "silver",
                    Requirement = "Check in for 7 consecutive days",
                    IsSecret    = false,
                },

                // ── Quiz Badges ──────────────────────────────────────────────
                new()
                {
                    Name        = "Perfect Score",
                    Description = "Achieve 100% on any quiz.",
                    ImageRef    = "badges/perfect_score.png",
                    Category    = "quiz",
                    Rarity      = "gold",
                    Requirement = "Get 100% on a quiz",
                    IsSecret    = false,
                },
                new()
                {
                    Name        = "Speed Demon",
                    Description = "Complete a quiz in under 30 seconds.",
                    ImageRef    = "badges/speed_demon.png",
                    Category    = "quiz",
                    Rarity      = "gold",
                    Requirement = "Finish a quiz in under 30 seconds",
                    IsSecret    = false,
                },

                // ── Milestone Badges ─────────────────────────────────────────
                new()
                {
                    Name        = "First Step",
                    Description = "Complete your very first quiz.",
                    ImageRef    = "badges/first_step.png",
                    Category    = "milestone",
                    Rarity      = "wood",
                    Requirement = "Complete your first quiz",
                    IsSecret    = false,
                },
                new()
                {
                    Name        = "Word Collector",
                    Description = "Learn or write 50 words in total.",
                    ImageRef    = "badges/50_words.png",
                    Category    = "milestone",
                    Rarity      = "silver",
                    Requirement = "Learn or write 50 words",
                    IsSecret    = false,
                },

                // ── Social Badges ────────────────────────────────────────────
                new()
                {
                    Name        = "Team Player",
                    Description = "Join a classroom and collaborate with classmates.",
                    ImageRef    = "badges/team_player.png",
                    Category    = "social",
                    Rarity      = "silver",
                    Requirement = "Join a classroom",
                    IsSecret    = false,
                },

                // ── Secret Badges ────────────────────────────────────────────
                new()
                {
                    Name        = "Night Owl",
                    Description = "???",
                    ImageRef    = "badges/night_owl.png",
                    Category    = "secret",
                    Rarity      = "crystal",
                    Requirement = "Complete a quiz after 11 PM",
                    IsSecret    = true,
                },
            };

            await _dbContext.Badges.AddRangeAsync(badges);
            await _dbContext.SaveChangesAsync();
        }

        // ── Classrooms ──────────────────────────────────────────────────────
        if (!await _dbContext.Classrooms.AnyAsync())
        {
            // Create a test teacher first
            var testTeacher = new User
            {
                UserName = "mr_smith_teacher",
                Email = "mr.smith@school.com",
                Name = "Mr. Smith",
                FamilyName = "Smith",
                Experience = 5000,
                Diamonds = 100,
                AvatarId = "0",
                EmailConfirmed = true
            };

            // Note: Normally users are seeded via SeedDefaultUsersAsync, but if they don't exist, add the teacher
            var existingTeacher = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == "mr.smith@school.com");
            if (existingTeacher == null)
            {
                _dbContext.Users.Add(testTeacher);
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                testTeacher = existingTeacher;
            }

            // Create classrooms with join codes
            var classrooms = new[]
            {
                new Classroom
                {
                    Name = "English 101 - Grade 5",
                    Description = "Learn English fundamentals including vocabulary, grammar, and reading comprehension.",
                    Subject = "English",
                    Thumbnail = "https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/thumbnails%2Fenglish.png?alt=media",
                    JoinCode = "ENG5ABCD",
                    TeacherId = testTeacher.Id,
                    IsActive = true
                },
                new Classroom
                {
                    Name = "Math 101 - Grade 5",
                    Description = "Master mathematics with interactive lessons covering basic arithmetic, fractions, and geometry.",
                    Subject = "Mathematics",
                    Thumbnail = "https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/thumbnails%2Fmath.png?alt=media",
                    JoinCode = "MATH5XYZ",
                    TeacherId = testTeacher.Id,
                    IsActive = true
                },
                new Classroom
                {
                    Name = "Science Explorer - Grade 4",
                    Description = "Explore the wonders of science through fun and interactive lessons!",
                    Subject = "Science",
                    Thumbnail = "https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/thumbnails%2Fscience.png?alt=media",
                    JoinCode = "SCI4TEST",
                    TeacherId = testTeacher.Id,
                    IsActive = true
                }
            };

            _dbContext.Classrooms.AddRange(classrooms);
            await _dbContext.SaveChangesAsync();
        }
    }
}
