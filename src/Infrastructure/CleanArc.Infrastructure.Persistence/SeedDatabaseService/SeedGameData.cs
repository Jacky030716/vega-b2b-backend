using System.Text.Json;
using CleanArc.Domain.Entities.Adaptive;
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

            var wordPairGame = new Game
            {
                Key = "word_pair",
                Name = "Word Twins",
                Description = "Flip the cards and match the word with the image!",
                ImageUrl = "https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/thumbnails%2Fwordpair.png?alt=media",
                Category = "Story Comprehension Training",
                SkillsTaught = "memory"
            };

            await _dbContext.Games.AddRangeAsync(magicBackpackGame, wordBridgeGame, wordPairGame);
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
                    GameId = wordPairGame.Id,
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
                    GameId = wordPairGame.Id,
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
                    GameId = wordPairGame.Id,
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
                new() { Name = "Pirate King", Description = "Arr! A swashbuckling pirate mascot variant.", Category = "avatar", Theme = "Classic", Price = 800, Currency = "diamonds", ImageUrl = "https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/mascots%2FPirate.png?alt=media", Rarity = "common", IsAvailable = true },
                new() { Name = "Crown", Description = "A regal mascot variant fit for a quiz champion.", Category = "avatar", Theme = "Classic", Price = 1500, Currency = "diamonds", ImageUrl = "https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/mascots%2FKing.png?alt=media", Rarity = "rare", IsAvailable = true },
                new() { Name = "Giyu", Description = "Demon Slayer", Category = "avatar", Theme = "Demon Slayer", Price = 1750, Currency = "diamonds", ImageUrl = "https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/mascots%2FGiyu.png?alt=media", Rarity = "rare", IsAvailable = true },
                new() { Name = "Rengoku", Description = "Demon Slayer", Category = "avatar", Theme = "Demon Slayer", Price = 1850, Currency = "diamonds", ImageUrl = "https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/mascots%2FRengoku.png?alt=media", Rarity = "rare", IsAvailable = true },
                new() { Name = "Inosuke", Description = "Demon Slayer", Category = "avatar", Theme = "Demon Slayer", Price = 1500, Currency = "diamonds", ImageUrl = "https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/mascots%2FInosuke.png?alt=media", Rarity = "rare", IsAvailable = true }
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

        var wordPair = await _dbContext.Games.FirstOrDefaultAsync(g => g.Key == "word_pair");
        if (wordPair is null)
        {
            wordPair = new Game
            {
                Key = "word_pair",
                Name = "Word Twins",
                Description = "Flip the cards and match the word with the image!",
                ImageUrl = "https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/thumbnails%2Fwordpair.png?alt=media",
                Category = "Story Comprehension Training",
                SkillsTaught = "memory"
            };

            await _dbContext.Games.AddAsync(wordPair);
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

        var hasWordPairChallenge = await _dbContext.Challenges.AnyAsync(c => c.GameId == wordPair.Id);
        if (!hasWordPairChallenge)
        {
            await _dbContext.Challenges.AddAsync(new Challenge
            {
                GameId = wordPair.Id,
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

        if (!hasWordBridgeChallenge || !hasWordPairChallenge)
        {
            await _dbContext.SaveChangesAsync();
        }

        // ── Badge catalog ─────────────────────────────────────────────────────
        // ImageRef stores a Firebase Storage relative path.
        // The frontend resolves it with resolveStorageRefToUrl(imageRef).
        var badgeSeeds = new List<Badge>
        {
            // ── Streak ───────────────────────────────────────────────────────
            new()
            {
                Name = "Week Warrior",
                Description = "Check in to the app for 7 days in a row.",
                ImageRef = "badges/7_day_streak.png",
                Category = "streak",
                Rarity = "silver",
                Requirement = "Check in for 7 consecutive days",
                RuleJson = "{\"eventType\":\"daily_check_in\",\"aggregation\":\"count\",\"threshold\":7}",
                IsSecret = false,
                RewardXp = 80,
                RewardDiamonds = 12,
            },
            new()
            {
                Name = "5-Day Streak",
                Description = "Check in to the app for 5 days in a row.",
                ImageRef = "badges/7_day_streak.png",
                Category = "streak",
                Rarity = "silver",
                Requirement = "Check in for 5 consecutive days",
                RuleJson = "{\"eventType\":\"daily_check_in\",\"aggregation\":\"count\",\"threshold\":5}",
                IsSecret = false,
                RewardXp = 60,
                RewardDiamonds = 10,
                RewardDreamTokens = 1,
            },
            new()
            {
                Name = "Level Up",
                Description = "Reach level 2 for the first time.",
                ImageRef = "badges/first_step.png",
                Category = "milestone",
                Rarity = "wood",
                Requirement = "Reach level 2",
                RuleJson = "{\"eventType\":\"LevelMilestone\",\"aggregation\":\"count\",\"threshold\":1,\"predicate\":{\"field\":\"level\",\"operator\":\"gte\",\"value\":2}}",
                IsSecret = false,
                RewardXp = 50,
                RewardDiamonds = 8,
                RewardDreamTokens = 1,
            },

            // ── Mastery ─────────────────────────────────────────────────────
            new()
            {
                Name = "Perfect Score",
                Description = "Achieve 100% on any quiz.",
                ImageRef = "badges/perfect_score.png",
                Category = "mastery",
                Rarity = "gold",
                Requirement = "Get 100% on a quiz",
                RuleJson = "{\"eventType\":\"attempt_completed\",\"aggregation\":\"count\",\"threshold\":1,\"predicate\":{\"field\":\"accuracy\",\"operator\":\"gte\",\"value\":1}}",
                IsSecret = false,
                RewardXp = 140,
                RewardDiamonds = 20,
            },
            new()
            {
                Name = "Subject Mastery",
                Description = "Complete 5 high-accuracy quiz attempts.",
                ImageRef = "badges/perfect_score.png",
                Category = "mastery",
                Rarity = "gold",
                Requirement = "Complete 5 quizzes with at least 90% accuracy",
                RuleJson = "{\"eventType\":\"attempt_completed\",\"aggregation\":\"count\",\"threshold\":5,\"predicate\":{\"field\":\"accuracy\",\"operator\":\"gte\",\"value\":0.9}}",
                IsSecret = false,
                RewardXp = 180,
                RewardDiamonds = 30,
                RewardDreamTokens = 1,
            },
            new()
            {
                Name = "Speed Demon",
                Description = "Complete a quiz in under 30 seconds.",
                ImageRef = "badges/speed_demon.png",
                Category = "mastery",
                Rarity = "gold",
                Requirement = "Finish a quiz in under 30 seconds",
                RuleJson = "{\"eventType\":\"attempt_completed\",\"aggregation\":\"count\",\"threshold\":1,\"predicate\":{\"field\":\"durationSeconds\",\"operator\":\"lte\",\"value\":30}}",
                IsSecret = false,
                RewardXp = 160,
                RewardDiamonds = 24,
            },
            new()
            {
                Name = "Quick Finisher",
                Description = "Complete any quiz within 2 minutes, 3 times.",
                ImageRef = "badges/speed_demon.png",
                Category = "mastery",
                Rarity = "silver",
                Requirement = "Complete a quiz within 2 minutes for three times",
                RuleJson = "{\"eventType\":\"attempt_completed\",\"aggregation\":\"count\",\"threshold\":3,\"predicate\":{\"field\":\"durationSeconds\",\"operator\":\"lte\",\"value\":120}}",
                IsSecret = false,
                RewardXp = 120,
                RewardDiamonds = 16,
            },

            // ── Milestones ──────────────────────────────────────────────────
            new()
            {
                Name = "First Step",
                Description = "Complete your very first quiz.",
                ImageRef = "badges/first_step.png",
                Category = "milestone",
                Rarity = "wood",
                Requirement = "Complete your first quiz",
                RuleJson = "{\"eventType\":\"attempt_completed\",\"aggregation\":\"count\",\"threshold\":1,\"predicate\":{\"field\":\"isFirstCompletion\",\"operator\":\"eq\",\"value\":true}}",
                IsSecret = false,
                RewardXp = 60,
                RewardDiamonds = 8,
            },
            new()
            {
                Name = "Word Collector",
                Description = "Learn or write 50 words in total.",
                ImageRef = "badges/50_words.png",
                Category = "milestone",
                Rarity = "silver",
                Requirement = "Learn or write 50 words",
                RuleJson = "{\"eventType\":\"attempt_completed\",\"aggregation\":\"count\",\"threshold\":50}",
                IsSecret = false,
                RewardXp = 180,
                RewardDiamonds = 28,
            },

            // ── Discovery ───────────────────────────────────────────────────
            new()
            {
                Name = "Team Player",
                Description = "Join a classroom and collaborate with classmates.",
                ImageRef = "badges/team_player.png",
                Category = "discovery",
                Rarity = "silver",
                Requirement = "Join a classroom",
                RuleJson = "{\"eventType\":\"ClassroomJoined\",\"aggregation\":\"count\",\"threshold\":1}",
                IsSecret = false,
                RewardXp = 80,
                RewardDiamonds = 10,
            },
            new()
            {
                Name = "Sticker Explorer",
                Description = "Open the Sticker Book for the first time.",
                ImageRef = "badges/first_step.png",
                Category = "discovery",
                Rarity = "wood",
                Requirement = "Open the sticker book once",
                RuleJson = "{\"eventType\":\"AchievementScreenOpened\",\"aggregation\":\"count\",\"threshold\":1}",
                IsSecret = false,
                RewardXp = 40,
                RewardDiamonds = 5,
            },
            new()
            {
                Name = "Badge Curator",
                Description = "Assign your first favorite badge.",
                ImageRef = "badges/team_player.png",
                Category = "discovery",
                Rarity = "silver",
                Requirement = "Set one featured badge",
                RuleJson = "{\"eventType\":\"FavoriteBadgeAssigned\",\"aggregation\":\"count\",\"threshold\":1}",
                IsSecret = false,
                RewardXp = 70,
                RewardDiamonds = 9,
            },
            new()
            {
                Name = "Detail Detective",
                Description = "Open your first badge detail modal.",
                ImageRef = "badges/perfect_score.png",
                Category = "discovery",
                Rarity = "wood",
                Requirement = "Inspect one badge detail",
                RuleJson = "{\"eventType\":\"BadgeDetailOpened\",\"aggregation\":\"count\",\"threshold\":1}",
                IsSecret = false,
                RewardXp = 50,
                RewardDiamonds = 7,
            },
            new()
            {
                Name = "Night Owl",
                Description = "???",
                ImageRef = "badges/night_owl.png",
                Category = "discovery",
                Rarity = "crystal",
                Requirement = "Complete a quiz after 11 PM",
                RuleJson = "{\"eventType\":\"attempt_completed\",\"aggregation\":\"count\",\"threshold\":1,\"predicate\":{\"field\":\"completedHourUtc\",\"operator\":\"gte\",\"value\":23}}",
                IsSecret = true,
                RewardXp = 220,
                RewardDiamonds = 40,
            },
        };

        var existingBadges = await _dbContext.Badges.ToDictionaryAsync(
            badge => badge.Name,
            badge => badge,
            StringComparer.OrdinalIgnoreCase);

        var hasBadgeChanges = false;
        foreach (var seedBadge in badgeSeeds)
        {
            if (!existingBadges.TryGetValue(seedBadge.Name, out var existingBadge))
            {
                await _dbContext.Badges.AddAsync(seedBadge);
                hasBadgeChanges = true;
                continue;
            }

            existingBadge.Description = seedBadge.Description;
            existingBadge.ImageRef = seedBadge.ImageRef;
            existingBadge.Category = seedBadge.Category;
            existingBadge.Rarity = seedBadge.Rarity;
            existingBadge.Requirement = seedBadge.Requirement;
            existingBadge.RuleJson = seedBadge.RuleJson;
            existingBadge.IsSecret = seedBadge.IsSecret;
            existingBadge.RewardXp = seedBadge.RewardXp;
            existingBadge.RewardDiamonds = seedBadge.RewardDiamonds;
            existingBadge.RewardDreamTokens = seedBadge.RewardDreamTokens;
            hasBadgeChanges = true;
        }

        if (hasBadgeChanges)
        {
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
                    JoinCode = "E5A1",
                    TeacherId = testTeacher.Id,
                    IsActive = true
                },
                new Classroom
                {
                    Name = "Math 101 - Grade 5",
                    Description = "Master mathematics with interactive lessons covering basic arithmetic, fractions, and geometry.",
                    Subject = "Mathematics",
                    Thumbnail = "https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/thumbnails%2Fmath.png?alt=media",
                    JoinCode = "M5X2",
                    TeacherId = testTeacher.Id,
                    IsActive = true
                },
                new Classroom
                {
                    Name = "Science Explorer - Grade 4",
                    Description = "Explore the wonders of science through fun and interactive lessons!",
                    Subject = "Science",
                    Thumbnail = "https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/thumbnails%2Fscience.png?alt=media",
                    JoinCode = "S4T3",
                    TeacherId = testTeacher.Id,
                    IsActive = true
                }
            };

            _dbContext.Classrooms.AddRange(classrooms);
            await _dbContext.SaveChangesAsync();
        }

        await SeedAdaptiveLayerAsync();

        // ── Visual Icons (Picture Passwords) ────────────────────────────────
        if (!await _dbContext.VisualIcons.AnyAsync())
        {
            var icons = new[]
            {
                new VisualIcon { Emoji = "🍎", Label = "Apple" },
                new VisualIcon { Emoji = "🐶", Label = "Dog" },
                new VisualIcon { Emoji = "🐱", Label = "Cat" },
                new VisualIcon { Emoji = "🌞", Label = "Sun" },
                new VisualIcon { Emoji = "🌙", Label = "Moon" },
                new VisualIcon { Emoji = "⭐", Label = "Star" },
                new VisualIcon { Emoji = "📚", Label = "Book" },
                new VisualIcon { Emoji = "🌳", Label = "Tree" },
                new VisualIcon { Emoji = "🚗", Label = "Car" },
                new VisualIcon { Emoji = "🐦", Label = "Bird" },
                new VisualIcon { Emoji = "🚤", Label = "Boat" },
                new VisualIcon { Emoji = "🔑", Label = "Key" }
            };

            await _dbContext.VisualIcons.AddRangeAsync(icons);
            await _dbContext.SaveChangesAsync();
        }
    }

    private async Task SeedAdaptiveLayerAsync()
    {
        var templates = new[]
        {
            new GameTemplate { Code = "SPELL_CATCHER", Category = "RECALL", Name = "Spell Catcher", Description = "Catch and order letters to recall full spelling.", SupportsAdaptiveDifficulty = true, IsActive = true },
            new GameTemplate { Code = "VOICE_BRIDGE", Category = "SPEAKING", Name = "Voice Bridge", Description = "Speak target words and receive pronunciation recall feedback.", SupportsAdaptiveDifficulty = true, IsActive = true },
            new GameTemplate { Code = "SYLLABLE_SUSHI", Category = "STRUCTURE", Name = "Syllable Sushi", Description = "Assemble words from syllable chunks.", SupportsAdaptiveDifficulty = true, IsActive = true },
            new GameTemplate { Code = "word_bridge", Category = "RECALL", Name = "Word Bridge", Description = "Existing spelling bridge game.", SupportsAdaptiveDifficulty = false, IsActive = true },
            new GameTemplate { Code = "word_pair", Category = "RECOGNITION", Name = "Word Twins", Description = "Existing recognition and memory matching game.", SupportsAdaptiveDifficulty = false, IsActive = true },
            new GameTemplate { Code = "magic_backpack", Category = "RECALL", Name = "Magic Backpack", Description = "Existing sequence memory game.", SupportsAdaptiveDifficulty = false, IsActive = true }
        };

        foreach (var template in templates)
        {
            if (!await _dbContext.GameTemplates.AnyAsync(t => t.Code == template.Code))
            {
                await _dbContext.GameTemplates.AddAsync(template);
            }
        }

        var adaptiveGames = new[]
        {
            new Game { Key = "spell_catcher", Name = "Spell Catcher", Description = "Catch letters and spell syllabus words.", ImageUrl = string.Empty, Category = "RECALL", SkillsTaught = "spelling recall" },
            new Game { Key = "voice_bridge", Name = "Voice Bridge", Description = "Practice oral recall and pronunciation.", ImageUrl = string.Empty, Category = "SPEAKING", SkillsTaught = "pronunciation recall" },
            new Game { Key = "syllable_sushi", Name = "Syllable Sushi", Description = "Build words from syllables.", ImageUrl = string.Empty, Category = "STRUCTURE", SkillsTaught = "syllable assembly" }
        };

        foreach (var game in adaptiveGames)
        {
            if (!await _dbContext.Games.AnyAsync(g => g.Key == game.Key))
            {
                await _dbContext.Games.AddAsync(game);
            }
        }

        await _dbContext.SaveChangesAsync();

        if (await _dbContext.SyllabusModules.AnyAsync())
        {
            return;
        }

        var bmModule = new SyllabusModule
        {
            Subject = "Bahasa Melayu",
            Language = "ms",
            YearLevel = 1,
            Term = "Term 1",
            Week = 1,
            Title = "Perkataan Asas Tahun 1",
            Description = "Starter Bahasa Melayu words for Malaysian primary learners.",
            SourceType = "predefined",
            IsActive = true
        };

        var englishModule = new SyllabusModule
        {
            Subject = "English",
            Language = "en",
            YearLevel = 1,
            Term = "Term 1",
            Week = 1,
            Title = "Year 1 Everyday Words",
            Description = "Starter English words for Malaysian primary learners.",
            SourceType = "predefined",
            IsActive = true
        };

        await _dbContext.SyllabusModules.AddRangeAsync(bmModule, englishModule);
        await _dbContext.SaveChangesAsync();

        await _dbContext.VocabularyItems.AddRangeAsync(
            new VocabularyItem { ModuleId = bmModule.Id, Word = "buku", NormalizedWord = "buku", Language = "ms", Subject = bmModule.Subject, YearLevel = 1, SyllablesJson = "[\"bu\",\"ku\"]", PhoneticHint = "bu-ku", PronunciationText = "buku", DifficultyLevel = 1, MeaningText = "book", ExampleSentence = "Saya baca buku.", IsActive = true },
            new VocabularyItem { ModuleId = bmModule.Id, Word = "mata", NormalizedWord = "mata", Language = "ms", Subject = bmModule.Subject, YearLevel = 1, SyllablesJson = "[\"ma\",\"ta\"]", PhoneticHint = "ma-ta", PronunciationText = "mata", DifficultyLevel = 1, MeaningText = "eye", ExampleSentence = "Mata saya sihat.", IsActive = true },
            new VocabularyItem { ModuleId = bmModule.Id, Word = "sekolah", NormalizedWord = "sekolah", Language = "ms", Subject = bmModule.Subject, YearLevel = 1, SyllablesJson = "[\"se\",\"ko\",\"lah\"]", PhoneticHint = "se-ko-lah", PronunciationText = "sekolah", DifficultyLevel = 2, MeaningText = "school", ExampleSentence = "Saya pergi ke sekolah.", IsActive = true },
            new VocabularyItem { ModuleId = bmModule.Id, Word = "makan", NormalizedWord = "makan", Language = "ms", Subject = bmModule.Subject, YearLevel = 1, SyllablesJson = "[\"ma\",\"kan\"]", PhoneticHint = "ma-kan", PronunciationText = "makan", DifficultyLevel = 1, MeaningText = "eat", ExampleSentence = "Ali makan nasi.", IsActive = true },
            new VocabularyItem { ModuleId = englishModule.Id, Word = "school", NormalizedWord = "school", Language = "en", Subject = englishModule.Subject, YearLevel = 1, SyllablesJson = "[\"school\"]", PhoneticHint = "skool", PronunciationText = "school", DifficultyLevel = 2, MeaningText = "place to learn", ExampleSentence = "I go to school.", IsActive = true },
            new VocabularyItem { ModuleId = englishModule.Id, Word = "pencil", NormalizedWord = "pencil", Language = "en", Subject = englishModule.Subject, YearLevel = 1, SyllablesJson = "[\"pen\",\"cil\"]", PhoneticHint = "pen-sil", PronunciationText = "pencil", DifficultyLevel = 1, MeaningText = "tool for writing", ExampleSentence = "This is my pencil.", IsActive = true },
            new VocabularyItem { ModuleId = englishModule.Id, Word = "friend", NormalizedWord = "friend", Language = "en", Subject = englishModule.Subject, YearLevel = 1, SyllablesJson = "[\"friend\"]", PhoneticHint = "frend", PronunciationText = "friend", DifficultyLevel = 2, MeaningText = "someone you like", ExampleSentence = "She is my friend.", IsActive = true },
            new VocabularyItem { ModuleId = englishModule.Id, Word = "apple", NormalizedWord = "apple", Language = "en", Subject = englishModule.Subject, YearLevel = 1, SyllablesJson = "[\"ap\",\"ple\"]", PhoneticHint = "ap-pel", PronunciationText = "apple", DifficultyLevel = 1, MeaningText = "a fruit", ExampleSentence = "I eat an apple.", IsActive = true }
        );

        await _dbContext.SaveChangesAsync();
    }
}
