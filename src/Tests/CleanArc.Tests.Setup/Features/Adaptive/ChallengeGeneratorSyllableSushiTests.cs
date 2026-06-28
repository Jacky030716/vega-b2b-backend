using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Infrastructure.Persistence;
using CleanArc.Infrastructure.Persistence.Services.Adaptive;
using CleanArc.Infrastructure.Persistence.Services.Adaptive.Strategies;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Tests.Setup.Features.Adaptive;

public class ChallengeGeneratorSyllableSushiTests
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
    public async Task GenerateAsync_SyllableSushi_ReturnsDualContractSpec()
    {
        await using var context = CreateContext();
        var module = await SeedModuleWithWordAsync(context, "bercuti", "[\"ber\",\"cu\",\"ti\"]", "ber/cu/ti", 2);
        var generator = new ChallengeGenerator(context, TestStrategies);

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
        var generator = new ChallengeGenerator(context, TestStrategies);

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
        var generator = new ChallengeGenerator(context, TestStrategies);

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
        var generator = new ChallengeGenerator(context, TestStrategies);

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
        Assert.Contains("bel", spec.Distractors);
        Assert.Contains("nang", spec.Distractors);
        Assert.DoesNotContain("bek", spec.Distractors);
        Assert.DoesNotContain("maka", spec.Distractors);
    }

    [Fact]
    public async Task GenerateAsync_SyllableSushi_HardDifficulty_UsesAtLeastFiveDistractors()
    {
        await using var context = CreateContext();
        var module = await SeedModuleWithWordAsync(context, "sekolah", "[\"se\",\"ko\",\"lah\"]", "se/ko/lah", 3);
        var generator = new ChallengeGenerator(context, TestStrategies);

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
        var generator = new ChallengeGenerator(context, TestStrategies);

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

    [Fact]
    public async Task GenerateAsync_SyllableSushi_EnglishWord_UsesLetterSegmentation()
    {
        await using var context = CreateContext();
        var module = new SyllabusModule
        {
            ModuleCode = $"T-EN-{Guid.NewGuid():N}",
            Subject = "English",
            Language = "en",
            YearLevel = 1,
            Term = "T1",
            Week = 1,
            Title = "English Test",
            Description = "English test module",
            SourceType = "test_seed",
            IsActive = true
        };
        context.SyllabusModules.Add(module);
        await context.SaveChangesAsync();

        context.VocabularyItems.Add(new VocabularyItem
        {
            ModuleId = module.Id,
            Word = "apple",
            NormalizedWord = "apple",
            Language = "en",
            Subject = "English",
            YearLevel = 1,
            DifficultyLevel = 1,
            IsActive = true,
            Translations = new List<VocabularyTranslation> { new VocabularyTranslation { LanguageCode = "en", TranslationText = "apple" } },
            SyllableInfo = new VocabularySyllableInfo { SyllablesJson = "[\"ap\",\"ple\"]", SyllableText = "ap-ple" }
        });
        await context.SaveChangesAsync();

        var generator = new ChallengeGenerator(context, TestStrategies);
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
        Assert.Equal("apple", preview.SyllableSushiSpec!.TargetWord);
        Assert.Equal(new[] { "a", "p", "p", "l", "e" }, preview.SyllableSushiSpec.CorrectSyllables);
        Assert.All(preview.SyllableSushiSpec.Distractors, d => Assert.Single(d));
    }

    [Fact]
    public async Task GenerateAsync_SyllableSushi_MandarinWord_UsesCharacterSegmentation()
    {
        await using var context = CreateContext();
        var module = new SyllabusModule
        {
            ModuleCode = $"T-ZH-{Guid.NewGuid():N}",
            Subject = "Mandarin",
            Language = "zh",
            YearLevel = 1,
            Term = "T1",
            Week = 1,
            Title = "Mandarin Test",
            Description = "Mandarin test module",
            SourceType = "test_seed",
            IsActive = true
        };
        context.SyllabusModules.Add(module);
        await context.SaveChangesAsync();

        context.VocabularyItems.Add(new VocabularyItem
        {
            ModuleId = module.Id,
            Word = "熊猫",
            NormalizedWord = "熊猫",
            Language = "zh",
            Subject = "Mandarin",
            YearLevel = 1,
            DifficultyLevel = 1,
            IsActive = true,
            Translations = new List<VocabularyTranslation> { new VocabularyTranslation { LanguageCode = "zh", TranslationText = "熊猫" } }
        });
        await context.SaveChangesAsync();

        var generator = new ChallengeGenerator(context, TestStrategies);
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
        Assert.Equal("熊猫", preview.SyllableSushiSpec!.TargetWord);
        Assert.Equal(new[] { "熊", "猫" }, preview.SyllableSushiSpec.CorrectSyllables);
        Assert.All(preview.SyllableSushiSpec.Distractors, d => Assert.Single(d));
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
            SyllableInfo = new VocabularySyllableInfo { SyllablesJson = syllablesJson, SyllableText = syllableText }
        });

        await context.SaveChangesAsync();
        return module;
    }
}
