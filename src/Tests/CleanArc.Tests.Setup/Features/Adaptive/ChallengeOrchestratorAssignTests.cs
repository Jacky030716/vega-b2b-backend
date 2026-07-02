using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.User;
using CleanArc.Infrastructure.Persistence;
using CleanArc.Infrastructure.Persistence.Services.Adaptive;
using CleanArc.Infrastructure.Persistence.Services.Adaptive.Strategies;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace CleanArc.Tests.Setup.Features.Adaptive;

public class ChallengeOrchestratorAssignTests
{
    private static readonly IGameStrategy[] TestStrategies =
    [
        new SpellCatcherGameStrategy(),
        new SyllableSushiGameStrategy(),
        new VoiceBridgeGameStrategy(),
        new TranslationGameStrategy(),
        new EchoSequenceGameStrategy()
    ];

    private static ApplicationDbContext CreateContext()
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = ":memory:" }.ToString());
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task AssignAsync_AttachesPredefinedLibraryModuleAndSupportsAllGameTypes()
    {
        await using var context = CreateContext();
        var teacher = await AddUserAsync(context, "teacher-library");
        var classroom = await AddClassroomAsync(context, teacher.Id);
        var module = await AddModuleWithVocabularyAsync(context, SyllabusModule.PredefinedModuleType);
        var generator = new ChallengeGenerator(context, TestStrategies);
        var orchestrator = new ChallengeOrchestrator(
            context,
            generator,
            Substitute.For<IRecommendationEngine>(),
            TestStrategies);

        foreach (var gameType in new[] { "SPELL_CATCHER", "SYLLABLE_SUSHI", "VOICE_BRIDGE", "TRANSLATION", "ECHO_SEQUENCE" })
        {
            var preview = await generator.GenerateAsync(
                new GenerateAdaptiveChallengeRequest(
                    "class",
                    null,
                    classroom.Id,
                    "module_practice",
                    "predefined_module",
                    module.Id,
                    gameType,
                    null,
                    null,
                    null,
                    null,
                    "ms",
                    gameType == "TRANSLATION" ? "zh" : "en"),
                CancellationToken.None);

            var assigned = await orchestrator.AssignAsync(
                new AssignAdaptiveChallengeRequest(
                    teacher.Id,
                    null,
                    classroom.Id,
                    null,
                    preview,
                    module.Subject),
                CancellationToken.None);

            Assert.Equal(classroom.Id, assigned.ClassId);
            Assert.Equal(3, assigned.ItemCount);
        }

        Assert.True(await context.ClassroomModules.AnyAsync(link =>
            link.ClassroomId == classroom.Id && link.ModuleId == module.Id));
        Assert.Equal(1, await context.ClassroomModules.CountAsync(link =>
            link.ClassroomId == classroom.Id && link.ModuleId == module.Id));
        Assert.Equal(5, await context.Challenges.CountAsync(challenge =>
            challenge.ClassroomId == classroom.Id && challenge.ModuleId == module.Id));
    }

    [Fact]
    public async Task AssignAsync_AttachesCustomLibraryModule()
    {
        await using var context = CreateContext();
        var teacher = await AddUserAsync(context, "teacher-custom-library");
        var classroom = await AddClassroomAsync(context, teacher.Id);
        var module = await AddModuleWithVocabularyAsync(context, SyllabusModule.CustomModuleType);
        var generator = new ChallengeGenerator(context, TestStrategies);
        var orchestrator = new ChallengeOrchestrator(
            context,
            generator,
            Substitute.For<IRecommendationEngine>(),
            TestStrategies);

        var preview = await generator.GenerateAsync(
            new GenerateAdaptiveChallengeRequest(
                "class",
                null,
                classroom.Id,
                "module_practice",
                "custom_module",
                module.Id,
                "ECHO_SEQUENCE",
                null,
                null,
                null,
                null),
            CancellationToken.None);

        await orchestrator.AssignAsync(
            new AssignAdaptiveChallengeRequest(
                teacher.Id,
                null,
                classroom.Id,
                null,
                preview,
                module.Subject),
            CancellationToken.None);

        Assert.True(await context.ClassroomModules.AnyAsync(link =>
            link.ClassroomId == classroom.Id && link.ModuleId == module.Id));
    }

    private static async Task<User> AddUserAsync(ApplicationDbContext context, string userName)
    {
        var user = new User
        {
            UserName = userName,
            Email = $"{userName}@example.com",
            Name = userName,
            Experience = 1
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private static async Task<Classroom> AddClassroomAsync(ApplicationDbContext context, int teacherId)
    {
        var classroom = new Classroom
        {
            Name = "Library Assignment",
            Description = "Classroom for assigning library challenges",
            Subject = "Bahasa Melayu",
            YearLevel = 1,
            TeacherId = teacherId,
            JoinCode = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant(),
            IsActive = true
        };

        context.Classrooms.Add(classroom);
        await context.SaveChangesAsync();
        return classroom;
    }

    private static async Task<SyllabusModule> AddModuleWithVocabularyAsync(ApplicationDbContext context, string moduleType)
    {
        var module = new SyllabusModule
        {
            ModuleCode = $"{moduleType}-{Guid.NewGuid():N}",
            Subject = "Bahasa Melayu",
            Language = "ms",
            YearLevel = 1,
            Term = "T1",
            Week = 1,
            UnitNumber = 1,
            UnitTitle = "Daily Words",
            Title = "Daily Words",
            Description = "Words for assignment testing",
            SourceType = "test_seed",
            ModuleType = moduleType,
            IsActive = true
        };
        context.SyllabusModules.Add(module);
        await context.SaveChangesAsync();

        var words = new[] { "bahagi", "bermain", "keluarga" };
        for (var index = 0; index < words.Length; index++)
        {
            var word = words[index];
            context.VocabularyItems.Add(new VocabularyItem
            {
                ModuleId = module.Id,
                Word = word,
                NormalizedWord = word,
                Language = "ms",
                Subject = "Bahasa Melayu",
                YearLevel = 1,
                ItemType = "WORD",
                DisplayOrder = index + 1,
                DifficultyLevel = 2,
                MeaningText = $"Meaning for {word}",
                ExampleSentence = $"{word} example.",
                IsActive = true,
                Translations =
                [
                    new VocabularyTranslation { LanguageCode = "ms", TranslationText = word },
                    new VocabularyTranslation { LanguageCode = "en", TranslationText = $"en-{word}" },
                    new VocabularyTranslation { LanguageCode = "zh", TranslationText = $"zh-{word}" }
                ],
                SyllableInfo = new VocabularySyllableInfo
                {
                    SyllablesJson = "[\"ba\",\"ha\"]",
                    SyllableText = "ba/ha"
                }
            });
        }

        await context.SaveChangesAsync();
        return module;
    }
}
