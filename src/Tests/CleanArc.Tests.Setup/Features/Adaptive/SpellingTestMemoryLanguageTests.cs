using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Infrastructure.Persistence.Services.Adaptive;

namespace CleanArc.Tests.Setup.Features.Adaptive;

public class SpellingTestMemoryLanguageTests
{
    [Theory]
    [InlineData("bm", "ms")]
    [InlineData("ms-MY", "ms")]
    [InlineData("english", "en")]
    [InlineData("mandarin", "zh")]
    [InlineData("zh-CN", "zh")]
    public void NormalizeLanguageCode_MapsKnownAliases(string input, string expected)
    {
        Assert.Equal(expected, SpellingTestMemoryLanguage.NormalizeLanguageCode(input));
    }

    [Fact]
    public void ResolveTargetText_UsesEnglishTextForEnglishItems()
    {
        var item = CreateItem("read", "en", ("ms", "baca"), ("en", "read"), ("zh", "读"));

        var target = SpellingTestMemoryLanguage.ResolveTargetText(item);

        Assert.Equal("read", target);
    }

    [Fact]
    public void ResolveTargetText_UsesMandarinTextForMandarinItems()
    {
        var item = CreateItem("datuk", "zh", ("ms", "datuk"), ("en", "grandfather"), ("zh", "爷爷"));

        var target = SpellingTestMemoryLanguage.ResolveTargetText(item);

        Assert.Equal("爷爷", target);
    }

    [Fact]
    public void ResolveTargetText_FallsBackToWordForMalayItemsWithoutBmTranslation()
    {
        var item = CreateItem("belajar", "bm", ("en", "study"));

        var target = SpellingTestMemoryLanguage.ResolveTargetText(item);

        Assert.Equal("belajar", target);
    }

    [Fact]
    public void OrderMixedMemoryWords_InterleavesLanguagesBeforeWordCountIsApplied()
    {
        var words = new[]
        {
            CreateItem("bm-1", "ms"),
            CreateItem("bm-2", "ms"),
            CreateItem("bm-3", "ms"),
            CreateItem("en-1", "en"),
            CreateItem("zh-1", "zh")
        };

        var selected = SpellingTestMemoryLanguage
            .OrderMixedMemoryWords(words)
            .Take(3)
            .Select(item => SpellingTestMemoryLanguage.NormalizeLanguageCode(item.Language))
            .ToList();

        Assert.Contains("ms", selected);
        Assert.Contains("en", selected);
        Assert.Contains("zh", selected);
    }

    private static VocabularyItem CreateItem(
        string word,
        string language,
        params (string LanguageCode, string Text)[] translations)
    {
        return new VocabularyItem
        {
            Word = word,
            Language = language,
            Translations = translations
                .Select(translation => new VocabularyTranslation
                {
                    LanguageCode = translation.LanguageCode,
                    TranslationText = translation.Text
                })
                .ToList()
        };
    }
}
