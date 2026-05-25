using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Domain.Entities.Achievement;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.Quiz;
using CleanArc.Domain.Entities.User;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.SeedDatabaseService;

public class SeedGameData : ISeedGameData
{
    private readonly ApplicationDbContext _dbContext;

    public SeedGameData(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Seed()
    {
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

        // ImageRef stores a Firebase Storage relative path.
        // The frontend resolves it with resolveStorageRefToUrl(imageRef).
        var badgeSeeds = new List<Badge>
        {
            new()
            {
                Name = "Week Warrior",
                Description = "Check in to the app for 7 days in a row.",
                ImageRef = "badges/week-warrior.png",
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
                ImageRef = "badges/5-day-streak.png",
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
                ImageRef = "badges/level-up.png",
                Category = "milestone",
                Rarity = "wood",
                Requirement = "Reach level 2",
                RuleJson = "{\"eventType\":\"LevelMilestone\",\"aggregation\":\"count\",\"threshold\":1,\"predicate\":{\"field\":\"level\",\"operator\":\"gte\",\"value\":2}}",
                IsSecret = false,
                RewardXp = 50,
                RewardDiamonds = 8,
                RewardDreamTokens = 1,
            },

            new()
            {
                Name = "Perfect Score",
                Description = "Achieve 100% on any quiz.",
                ImageRef = "badges/perfect-score.png",
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
                ImageRef = "badges/subject-mastery.png",
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
                ImageRef = "badges/speed-demon.png",
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
                ImageRef = "badges/quick-finisher.png",
                Category = "mastery",
                Rarity = "silver",
                Requirement = "Complete a quiz within 2 minutes for three times",
                RuleJson = "{\"eventType\":\"attempt_completed\",\"aggregation\":\"count\",\"threshold\":3,\"predicate\":{\"field\":\"durationSeconds\",\"operator\":\"lte\",\"value\":120}}",
                IsSecret = false,
                RewardXp = 120,
                RewardDiamonds = 16,
            },

            new()
            {
                Name = "First Step",
                Description = "Complete your very first quiz.",
                ImageRef = "badges/first-step.png",
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
                ImageRef = "badges/word-collector.png",
                Category = "milestone",
                Rarity = "silver",
                Requirement = "Learn or write 50 words",
                RuleJson = "{\"eventType\":\"attempt_completed\",\"aggregation\":\"count\",\"threshold\":50}",
                IsSecret = false,
                RewardXp = 180,
                RewardDiamonds = 28,
            },

            new()
            {
                Name = "Team Player",
                Description = "Join a classroom and collaborate with classmates.",
                ImageRef = "badges/team-player.png",
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
                ImageRef = "badges/sticker-explorer.png",
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
                ImageRef = "badges/badge-curator.png",
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
                ImageRef = "badges/detail-detective.png",
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
                ImageRef = "badges/night-owl.png",
                Category = "discovery",
                Rarity = "crystal",
                Requirement = "Complete a quiz after 11 PM",
                RuleJson = "{\"eventType\":\"attempt_completed\",\"aggregation\":\"count\",\"threshold\":1,\"predicate\":{\"field\":\"completedHourUtc\",\"operator\":\"gte\",\"value\":23}}",
                IsSecret = true,
                RewardXp = 220,
                RewardDiamonds = 40,
            },
            new()
            {
                Code = "FIRST_CHALLENGE",
                Name = "Practice Rookie",
                Description = "Complete your first assigned challenge.",
                ImageRef = "badges/practice-rookie.png",
                Category = "milestone",
                Rarity = "wood",
                Requirement = "Complete 1 challenge",
                IsSecret = false,
                RewardXp = 60,
                RewardDiamonds = 8,
            },
            new()
            {
                Code = "COMPLETE_3_CHALLENGES",
                Name = "Challenge Climber",
                Description = "Complete 3 learning challenges.",
                ImageRef = "badges/challenge-climber.png",
                Category = "milestone",
                Rarity = "silver",
                Requirement = "Complete 3 challenges",
                IsSecret = false,
                RewardXp = 100,
                RewardDiamonds = 15,
            },
            new()
            {
                Code = "COMPLETE_10_CHALLENGES",
                Name = "Challenge Champion",
                Description = "Complete 10 learning challenges.",
                ImageRef = "badges/challenge-champion.png",
                Category = "milestone",
                Rarity = "gold",
                Requirement = "Complete 10 challenges",
                IsSecret = false,
                RewardXp = 220,
                RewardDiamonds = 35,
            },
            new()
            {
                Code = "REACH_LEVEL_5",
                Name = "Level 5 Explorer",
                Description = "Reach level 5.",
                ImageRef = "badges/level-5-explorer.png",
                Category = "milestone",
                Rarity = "silver",
                Requirement = "Reach level 5",
                IsSecret = false,
                RewardXp = 120,
                RewardDiamonds = 20,
            },
            new()
            {
                Code = "REACH_LEVEL_10",
                Name = "Level 10 Legend",
                Description = "Reach level 10.",
                ImageRef = "badges/level-10-legend.png",
                Category = "milestone",
                Rarity = "gold",
                Requirement = "Reach level 10",
                IsSecret = false,
                RewardXp = 260,
                RewardDiamonds = 45,
            },
            new()
            {
                Code = "OWN_3_MASCOTS",
                Name = "Mascot Collector",
                Description = "Own 3 mascot avatars.",
                ImageRef = "badges/mascot-collector.png",
                Category = "discovery",
                Rarity = "silver",
                Requirement = "Own 3 mascots",
                IsSecret = false,
                RewardXp = 140,
                RewardDiamonds = 25,
            },
            new()
            {
                Code = "COMPLETE_1_MODULE",
                Name = "Module Finisher",
                Description = "Complete every challenge in a module.",
                ImageRef = "badges/module-finisher.png",
                Category = "mastery",
                Rarity = "silver",
                Requirement = "Complete 1 module",
                IsSecret = false,
                RewardXp = 180,
                RewardDiamonds = 30,
            },
        };

        foreach (var seedBadge in badgeSeeds)
        {
            seedBadge.Code = string.IsNullOrWhiteSpace(seedBadge.Code)
                ? ToAchievementCode(seedBadge.Name)
                : seedBadge.Code.Trim().ToUpperInvariant();
            seedBadge.IsActive = true;
        }

        var existingBadgesByCode = await _dbContext.Badges
            .Where(badge => badge.Code != "")
            .ToDictionaryAsync(
            badge => badge.Code,
            badge => badge,
            StringComparer.OrdinalIgnoreCase);
        var existingBadgesByName = await _dbContext.Badges.ToDictionaryAsync(
            badge => badge.Name,
            badge => badge,
            StringComparer.OrdinalIgnoreCase);

        var hasBadgeChanges = false;
        foreach (var seedBadge in badgeSeeds)
        {
            if (!existingBadgesByCode.TryGetValue(seedBadge.Code, out var existingBadge)
                && !existingBadgesByName.TryGetValue(seedBadge.Name, out existingBadge)
                && !TryGetLegacyBadge(seedBadge.Code, existingBadgesByCode, existingBadgesByName, out existingBadge))
            {
                await _dbContext.Badges.AddAsync(seedBadge);
                hasBadgeChanges = true;
                continue;
            }

            existingBadge.Code = seedBadge.Code;
            existingBadge.Name = seedBadge.Name;
            existingBadge.Description = seedBadge.Description;
            existingBadge.ImageRef = seedBadge.ImageRef;
            existingBadge.Category = seedBadge.Category;
            existingBadge.Rarity = seedBadge.Rarity;
            existingBadge.Requirement = seedBadge.Requirement;
            existingBadge.RuleJson = seedBadge.RuleJson;
            existingBadge.IsSecret = seedBadge.IsSecret;
            existingBadge.IsActive = seedBadge.IsActive;
            existingBadge.RewardXp = seedBadge.RewardXp;
            existingBadge.RewardDiamonds = seedBadge.RewardDiamonds;
            existingBadge.RewardDreamTokens = seedBadge.RewardDreamTokens;
            hasBadgeChanges = true;
        }

        hasBadgeChanges |= await DeactivateDuplicateLegacyBadges();

        if (hasBadgeChanges)
        {
            await _dbContext.SaveChangesAsync();
        }

        await SeedDemoAchievementTriggers();

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
                EmailConfirmed = true,
                InstitutionId = 1
            };

            // Note: Normally users are seeded via SeedDefaultUsersAsync, but if they don't exist, add the teacher
            var existingTeacher = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == "mr.smith@school.com");
            if (existingTeacher == null)
            {
                _dbContext.Users.Add(testTeacher);
                await _dbContext.SaveChangesAsync();

                var teacherRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "teacher");
                if (teacherRole != null)
                {
                    _dbContext.UserRoles.Add(new UserRole
                    {
                        UserId = testTeacher.Id,
                        RoleId = teacherRole.Id
                    });
                }

                _dbContext.InstitutionUsers.Add(new CleanArc.Domain.Entities.Institution.InstitutionUser
                {
                    InstitutionId = 1,
                    UserId = testTeacher.Id,
                    AccessScope = "Teacher access",
                    IsPrimary = false,
                    IsActive = true,
                    JoinedAt = DateTime.UtcNow
                });

                await _dbContext.SaveChangesAsync();
            }
            else
            {
                testTeacher = existingTeacher;

                var updated = false;
                if (!testTeacher.InstitutionId.HasValue || testTeacher.InstitutionId != 1)
                {
                    testTeacher.InstitutionId = 1;
                    _dbContext.Users.Update(testTeacher);
                    updated = true;
                }

                var teacherRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "teacher");
                if (teacherRole != null && !await _dbContext.UserRoles.AnyAsync(ur => ur.UserId == testTeacher.Id && ur.RoleId == teacherRole.Id))
                {
                    _dbContext.UserRoles.Add(new UserRole
                    {
                        UserId = testTeacher.Id,
                        RoleId = teacherRole.Id
                    });
                    updated = true;
                }

                var existsMembership = await _dbContext.InstitutionUsers.AnyAsync(iu => iu.UserId == testTeacher.Id && iu.InstitutionId == 1);
                if (!existsMembership)
                {
                    _dbContext.InstitutionUsers.Add(new CleanArc.Domain.Entities.Institution.InstitutionUser
                    {
                        InstitutionId = 1,
                        UserId = testTeacher.Id,
                        AccessScope = "Teacher access",
                        IsPrimary = false,
                        IsActive = true,
                        JoinedAt = DateTime.UtcNow
                    });
                    updated = true;
                }

                if (updated)
                {
                    await _dbContext.SaveChangesAsync();
                }
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
            new GameTemplate { Code = "SYLLABLE_SUSHI", Category = "STRUCTURE", Name = "Syllable Sushi", Description = "Assemble words from syllable chunks.", SupportsAdaptiveDifficulty = true, IsActive = true }
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

    private async Task SeedDemoAchievementTriggers()
    {
        var badges = await _dbContext.Badges
            .Where(b => b.IsActive)
            .ToDictionaryAsync(b => b.Code, b => b, StringComparer.OrdinalIgnoreCase);

        var triggerSeeds = new[]
        {
            CreateTrigger("FIRST_CHALLENGE", "CHALLENGE_COMPLETED", "Complete 1 challenge", "count", null, 1m),
            CreateTrigger("COMPLETE_3_CHALLENGES", "CHALLENGE_COMPLETED", "Complete 3 challenges", "count", null, 3m),
            CreateTrigger("COMPLETE_10_CHALLENGES", "CHALLENGE_COMPLETED", "Complete 10 challenges", "count", null, 10m),
            CreateTrigger("REACH_LEVEL_5", "LEVEL_REACHED", "Reach level 5", "max", "level", 5m),
            CreateTrigger("REACH_LEVEL_10", "LEVEL_REACHED", "Reach level 10", "max", "level", 10m),
            CreateTrigger("OWN_3_MASCOTS", "MASCOT_PURCHASED", "Own 3 mascots", "max", "ownedMascotCount", 3m),
            CreateTrigger("COMPLETE_1_MODULE", "MODULE_COMPLETED", "Complete 1 module", "count", null, 1m),
            CreateTrigger("PERFECT_SCORE", "STARS_EARNED", "Earn 3 stars in a challenge", "max", "starsEarned", 3m),
        };

        var hasChanges = false;
        foreach (var seed in triggerSeeds)
        {
            if (!badges.TryGetValue(seed.Code, out var badge))
            {
                continue;
            }

            var existing = await _dbContext.AchievementTriggers
                .FirstOrDefaultAsync(t => t.BadgeId == badge.Id && t.EventType == seed.EventType);

            if (existing is null)
            {
                _dbContext.AchievementTriggers.Add(new AchievementTrigger
                {
                    BadgeId = badge.Id,
                    EventType = seed.EventType,
                    Description = seed.Description,
                    AggregationType = seed.AggregationType,
                    AggregationSourceField = seed.AggregationSourceField,
                    Threshold = seed.Threshold,
                    IsActive = true,
                    EvaluationOrder = 0,
                });
                hasChanges = true;
                continue;
            }

            existing.Description = seed.Description;
            existing.AggregationType = seed.AggregationType;
            existing.AggregationSourceField = seed.AggregationSourceField;
            existing.Threshold = seed.Threshold;
            existing.IsActive = true;
            existing.EvaluationOrder = 0;
            hasChanges = true;
        }

        if (hasChanges)
        {
            await _dbContext.SaveChangesAsync();
        }
    }

    private static AchievementTriggerSeed CreateTrigger(
        string code,
        string eventType,
        string description,
        string aggregationType,
        string? aggregationSourceField,
        decimal threshold)
        => new(code, eventType, description, aggregationType, aggregationSourceField, threshold);

    private static bool TryGetLegacyBadge(
        string seedCode,
        Dictionary<string, Badge> existingBadgesByCode,
        Dictionary<string, Badge> existingBadgesByName,
        out Badge? badge)
    {
        badge = null;

        if (!string.Equals(seedCode, "OWN_3_MASCOTS", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return existingBadgesByCode.TryGetValue("FASHION_ICON", out badge)
            || existingBadgesByName.TryGetValue("Fashion Icon", out badge);
    }

    private async Task<bool> DeactivateDuplicateLegacyBadges()
    {
        var canonicalMascotBadge = await _dbContext.Badges
            .FirstOrDefaultAsync(b => b.Code == "OWN_3_MASCOTS");

        if (canonicalMascotBadge is null)
        {
            return false;
        }

        var legacyMascotBadges = await _dbContext.Badges
            .Where(b =>
                b.Id != canonicalMascotBadge.Id &&
                !b.Code.StartsWith("LEGACY_") &&
                (b.Code == "FASHION_ICON" || b.Name == "Fashion Icon"))
            .ToListAsync();

        var hasChanges = false;
        foreach (var legacyBadge in legacyMascotBadges)
        {
            legacyBadge.IsActive = false;
            legacyBadge.Code = $"LEGACY_{legacyBadge.Code}";
            hasChanges = true;
        }

        return hasChanges;
    }

    private static string ToAchievementCode(string name)
    {
        var chars = name
            .Trim()
            .Select(ch => char.IsLetterOrDigit(ch) ? char.ToUpperInvariant(ch) : '_')
            .ToArray();

        return string.Join("_", new string(chars)
            .Split('_', StringSplitOptions.RemoveEmptyEntries));
    }

    private sealed record AchievementTriggerSeed(
        string Code,
        string EventType,
        string Description,
        string AggregationType,
        string? AggregationSourceField,
        decimal Threshold);
}
