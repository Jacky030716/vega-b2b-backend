using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Infrastructure.Persistence;
using CleanArc.Infrastructure.Persistence.Services.Adaptive;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Tests.Setup.Features.Adaptive;

public class ChallengeGeneratorSpellCatcherTests
{
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
    public async Task GenerateAsync_SpellCatcher_ReturnsDualContractSpec()
    {
        await using var context = CreateContext();
        var module = await SeedModuleWithWordAsync(context, "bahagi", "[\"ba\",\"ha\",\"gi\"]", 2);
        var generator = new ChallengeGenerator(context);

        var preview = await generator.GenerateAsync(
            new GenerateAdaptiveChallengeRequest(
                "class",
                null,
                10,
                "practice_weekly_words",
                "predefined_module",
                module.Id,
                "SPELL_CATCHER",
                null,
                null,
                null,
                null),
            CancellationToken.None);

        var spec = Assert.IsType<SpellCatcherSpecDto>(preview.SpellCatcherSpec);
        Assert.Equal("SPELL_CATCHER", spec.GameType);
        Assert.Equal("bahagi", spec.TargetWord);
        Assert.NotEqual("BAHAGI", spec.ScrambledLetters);
        Assert.Equal(spec.TargetWord.Length + 2, spec.LetterPool.Count);
        Assert.True(spec.UiConfig.PreviewPhase.Enabled);
    }

    [Fact]
    public async Task GenerateAsync_SpellCatcher_DefaultWeaknessFlags_EnableMeaningAndAudioSupport()
    {
        await using var context = CreateContext();
        var module = await SeedModuleWithWordAsync(context, "bermain", "[\"ber\",\"ma\",\"in\"]", 2);
        var generator = new ChallengeGenerator(context);
        var preview = await generator.GenerateAsync(
            new GenerateAdaptiveChallengeRequest(
                "student",
                99,
                null,
                "improve_weak_words",
                "predefined_module",
                module.Id,
                "SPELL_CATCHER",
                null,
                null,
                null,
                null),
            CancellationToken.None);

        var spec = Assert.IsType<SpellCatcherSpecDto>(preview.SpellCatcherSpec);
        Assert.True(spec.UiConfig.ChallengePhase.ShowMeaningHint);
        Assert.False(spec.UiConfig.ChallengePhase.ShowSyllableHint);
        Assert.True(spec.AudioConfig.ShouldAutoPlay);
    }

    private static async Task<SyllabusModule> SeedModuleWithWordAsync(
        ApplicationDbContext context,
        string bmWord,
        string syllablesJson,
        int difficulty)
    {
        var module = new SyllabusModule
        {
            ModuleCode = $"SC-{Guid.NewGuid():N}",
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
            BmText = bmWord,
            ZhText = "zh-test",
            EnText = "en-test",
            Language = "ms",
            Subject = "Bahasa Melayu",
            YearLevel = 1,
            SyllablesJson = syllablesJson,
            SyllableText = string.Join('/', System.Text.Json.JsonSerializer.Deserialize<List<string>>(syllablesJson)!),
            ItemType = "WORD",
            DisplayOrder = 1,
            DifficultyLevel = difficulty,
            IsActive = true
        });

        await context.SaveChangesAsync();
        return module;
    }
}
