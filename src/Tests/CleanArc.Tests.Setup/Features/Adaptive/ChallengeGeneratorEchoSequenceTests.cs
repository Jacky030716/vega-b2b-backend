using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Infrastructure.Persistence;
using CleanArc.Infrastructure.Persistence.Services.Adaptive;
using CleanArc.Infrastructure.Persistence.Services.Adaptive.Strategies;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Tests.Setup.Features.Adaptive;

public class ChallengeGeneratorEchoSequenceTests
{
    private static readonly IGameStrategy[] TestStrategies = new IGameStrategy[]
    {
        new SpellCatcherGameStrategy(),
        new SyllableSushiGameStrategy(),
        new VoiceBridgeGameStrategy(),
        new TranslationGameStrategy(),
        new EchoSequenceGameStrategy()
    };

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
    public async Task GenerateAsync_EchoSequence_ReturnsValidPlayableContent()
    {
        await using var context = CreateContext();
        var module = await SeedModuleWithWordAsync(context, "bahagi", "[\"ba\",\"ha\",\"gi\"]", 2);
        var generator = new ChallengeGenerator(context, TestStrategies);

        var preview = await generator.GenerateAsync(
            new GenerateAdaptiveChallengeRequest(
                "class",
                null,
                10,
                "practice_weekly_words",
                "predefined_module",
                module.Id,
                "ECHO_SEQUENCE",
                null,
                null,
                null,
                null),
            CancellationToken.None);

        Assert.Equal("ECHO_SEQUENCE", preview.GameTemplateCode);
        Assert.Equal("echo_sequence", preview.GameKey);
        Assert.Equal("RECALL", preview.Category);
        Assert.NotEmpty(preview.Items);
        Assert.Contains("ECHO_SEQUENCE", preview.ContentData);
    }

    private static async Task<SyllabusModule> SeedModuleWithWordAsync(
        ApplicationDbContext context,
        string bmWord,
        string syllablesJson,
        int difficulty)
    {
        var module = new SyllabusModule
        {
            ModuleCode = $"ES-{Guid.NewGuid():N}",
            Subject = "Bahasa Melayu",
            Language = "ms",
            YearLevel = 1,
            Term = "T1",
            Week = 1,
            UnitNumber = 1,
            UnitTitle = "Ujian",
            Title = "Ujian",
            Description = "Test module",
            SourceType = "test_seed",
            IsActive = true
        };
        context.SyllabusModules.Add(module);
        await context.SaveChangesAsync();

        context.VocabularyItems.Add(new VocabularyItem
        {
            ModuleId = module.Id,
            Word = bmWord,
            NormalizedWord = bmWord.ToLowerInvariant(),
            Language = "ms",
            Subject = "Bahasa Melayu",
            YearLevel = 1,
            ItemType = "WORD",
            DisplayOrder = 1,
            DifficultyLevel = difficulty,
            IsActive = true,
            Translations = new List<VocabularyTranslation>
            {
                new VocabularyTranslation { LanguageCode = "ms", TranslationText = bmWord },
                new VocabularyTranslation { LanguageCode = "zh", TranslationText = "zh-test" },
                new VocabularyTranslation { LanguageCode = "en", TranslationText = "en-test" }
            },
            SyllableInfo = new VocabularySyllableInfo 
            { 
                SyllablesJson = syllablesJson, 
                SyllableText = string.Join('/', System.Text.Json.JsonSerializer.Deserialize<List<string>>(syllablesJson)!) 
            }
        });

        await context.SaveChangesAsync();
        return module;
    }
}
