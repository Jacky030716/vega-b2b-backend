using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Infrastructure.Persistence;
using CleanArc.Infrastructure.Persistence.Services.Adaptive;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Tests.Setup.Features.Adaptive;

public class ChallengeGeneratorSyllableSushiTests
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
    public async Task GenerateAsync_SyllableSushi_ReturnsDualContractSpec()
    {
        await using var context = CreateContext();
        var module = await SeedModuleWithWordAsync(context, "bercuti", "[\"ber\",\"cu\",\"ti\"]", "ber/cu/ti", 2);
        var generator = new ChallengeGenerator(context);

        var preview = await generator.GenerateAsync(
            new GenerateAdaptiveChallengeRequest(
                "class",
                null,
                10,
                "practice_weekly_words",
                "predefined_module",
                module.Id,
                "SYLLABLE_SUSHI",
                null,
                null,
                null,
                null),
            CancellationToken.None);

        Assert.Equal("SYLLABLE_SUSHI", preview.GameTemplateCode);
        Assert.NotNull(preview.SyllableSushiSpec);
        Assert.Equal("bercuti", preview.SyllableSushiSpec!.TargetWord);
        Assert.Equal(new[] { "ber", "cu", "ti" }, preview.SyllableSushiSpec.CorrectSyllables);
        Assert.Equal(new[] { 0, 1, 2 }, preview.SyllableSushiSpec.CorrectOrder);
        Assert.True(preview.SyllableSushiSpec.SyllablePool.Count > preview.SyllableSushiSpec.CorrectSyllables.Count);
        Assert.DoesNotContain(preview.SyllableSushiSpec.SyllablePool, value => string.Equals(value, "bercuti", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(preview.SyllableSushiSpec.Distractors, value => string.Equals(value, "bercuti", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(preview.SyllableSushiSpec.Distractors, value => value.StartsWith("b", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GenerateAsync_SyllableSushi_EasyDifficulty_UsesAtLeastThreeDistractors()
    {
        await using var context = CreateContext();
        var module = await SeedModuleWithWordAsync(context, "abang", "[\"a\",\"bang\"]", "a/bang", 1);
        var generator = new ChallengeGenerator(context);

        var preview = await generator.GenerateAsync(
            new GenerateAdaptiveChallengeRequest(
                "class",
                null,
                10,
                "practice_weekly_words",
                "predefined_module",
                module.Id,
                "SYLLABLE_SUSHI",
                null,
                null,
                null,
                null),
            CancellationToken.None);

        var spec = Assert.IsType<SyllableSushiSpecDto>(preview.SyllableSushiSpec);
        Assert.True(spec.Distractors.Count >= 3);
        Assert.DoesNotContain(spec.SyllablePool, value => string.Equals(value, "abang", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(spec.Distractors, value => string.Equals(value, "abang", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GenerateAsync_SyllableSushi_RepairsWholeWordSyllableChoice()
    {
        await using var context = CreateContext();
        var module = await SeedModuleWithWordAsync(context, "abang", "[\"abang\"]", "abang", 1);
        var generator = new ChallengeGenerator(context);

        var preview = await generator.GenerateAsync(
            new GenerateAdaptiveChallengeRequest(
                "class",
                null,
                10,
                "practice_weekly_words",
                "predefined_module",
                module.Id,
                "SYLLABLE_SUSHI",
                null,
                null,
                null,
                null),
            CancellationToken.None);

        var spec = Assert.IsType<SyllableSushiSpecDto>(preview.SyllableSushiSpec);
        Assert.DoesNotContain(spec.CorrectSyllables, value => string.Equals(value, "abang", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(spec.SyllablePool, value => string.Equals(value, "abang", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GenerateAsync_SyllableSushi_PhraseUsesStoredMalaySyllablesAndRelevantDistractors()
    {
        await using var context = CreateContext();
        var module = await SeedModuleWithWordAsync(context, "bekal makanan", "[\"be\",\"kal\",\"ma\",\"ka\",\"nan\"]", "be/kal ma/ka/nan", 1);
        var generator = new ChallengeGenerator(context);

        var preview = await generator.GenerateAsync(
            new GenerateAdaptiveChallengeRequest(
                "class",
                null,
                10,
                "practice_weekly_words",
                "predefined_module",
                module.Id,
                "SYLLABLE_SUSHI",
                null,
                null,
                null,
                null),
            CancellationToken.None);

        var spec = Assert.IsType<SyllableSushiSpecDto>(preview.SyllableSushiSpec);
        Assert.Equal(new[] { "be", "kal", "ma", "ka", "nan" }, spec.CorrectSyllables);
        Assert.Contains("ber", spec.Distractors);
        Assert.Contains("nang", spec.Distractors);
        Assert.DoesNotContain("bek", spec.Distractors);
        Assert.DoesNotContain("maka", spec.Distractors);
    }

    [Fact]
    public async Task GenerateAsync_SyllableSushi_HardDifficulty_UsesAtLeastFiveDistractors()
    {
        await using var context = CreateContext();
        var module = await SeedModuleWithWordAsync(context, "sekolah", "[\"se\",\"ko\",\"lah\"]", "se/ko/lah", 3);
        var generator = new ChallengeGenerator(context);

        var preview = await generator.GenerateAsync(
            new GenerateAdaptiveChallengeRequest(
                "class",
                null,
                10,
                "practice_weekly_words",
                "predefined_module",
                module.Id,
                "SYLLABLE_SUSHI",
                null,
                null,
                null,
                null),
            CancellationToken.None);

        var spec = Assert.IsType<SyllableSushiSpecDto>(preview.SyllableSushiSpec);
        Assert.True(spec.Distractors.Count >= 5);
    }

    [Fact]
    public async Task GenerateAsync_SyllableSushi_IsDeterministicForSameInput()
    {
        await using var context = CreateContext();
        var module = await SeedModuleWithWordAsync(context, "bermain", "[\"ber\",\"ma\",\"in\"]", "ber/ma/in", 2);
        var generator = new ChallengeGenerator(context);

        var request = new GenerateAdaptiveChallengeRequest(
            "class",
            null,
            10,
            "practice_weekly_words",
            "predefined_module",
            module.Id,
            "SYLLABLE_SUSHI",
            null,
            null,
            null,
            null);

        var first = await generator.GenerateAsync(request, CancellationToken.None);
        var second = await generator.GenerateAsync(request, CancellationToken.None);

        Assert.NotNull(first.SyllableSushiSpec);
        Assert.NotNull(second.SyllableSushiSpec);
        Assert.Equal(first.SyllableSushiSpec!.Distractors, second.SyllableSushiSpec!.Distractors);
        Assert.Equal(first.SyllableSushiSpec.SyllablePool, second.SyllableSushiSpec.SyllablePool);
    }

    private static async Task<SyllabusModule> SeedModuleWithWordAsync(
        ApplicationDbContext context,
        string bmWord,
        string syllablesJson,
        string syllableText,
        int difficulty)
    {
        var module = new SyllabusModule
        {
            ModuleCode = $"T-{Guid.NewGuid():N}",
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
            SyllableText = syllableText,
            ItemType = "WORD",
            DisplayOrder = 1,
            DifficultyLevel = difficulty,
            IsActive = true
        });

        await context.SaveChangesAsync();
        return module;
    }
}
