using CleanArc.Domain.Entities.Adaptive;

namespace CleanArc.Infrastructure.Persistence.Services.Adaptive;

public static class SpellingTestMemoryLanguage
{
    public static string NormalizeLanguageCode(string? language)
    {
        var normalized = language?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalized is "en" or "english" or "bi" || normalized.StartsWith("en-"))
            return "en";
        if (normalized is "zh" or "cn" or "mandarin" or "chinese" || normalized.StartsWith("zh-"))
            return "zh";
        return "ms";
    }

    public static string ResolveTargetText(VocabularyItem item)
    {
        var language = NormalizeLanguageCode(item.Language);
        var target = language switch
        {
            "en" => GetTranslation(item, "en"),
            "zh" => GetTranslation(item, "zh"),
            _ => GetTranslation(item, "ms")
        };

        return string.IsNullOrWhiteSpace(target)
            ? item.Word.Trim()
            : target.Trim();
    }

    public static IReadOnlyList<VocabularyItem> OrderMixedMemoryWords(IEnumerable<VocabularyItem> words)
    {
        var buckets = words
            .GroupBy(item => NormalizeLanguageCode(item.Language))
            .OrderBy(group => group.Key)
            .Select(group => group.ToList())
            .ToList();
        var result = new List<VocabularyItem>();
        var index = 0;

        while (buckets.Any(bucket => index < bucket.Count))
        {
            foreach (var bucket in buckets)
            {
                if (index < bucket.Count)
                {
                    result.Add(bucket[index]);
                }
            }

            index++;
        }

        return result;
    }

    private static string? GetTranslation(VocabularyItem item, string languageCode)
    {
        return item.Translations
            .FirstOrDefault(translation =>
                string.Equals(
                    NormalizeLanguageCode(translation.LanguageCode),
                    languageCode,
                    StringComparison.OrdinalIgnoreCase))
            ?.TranslationText;
    }
}
