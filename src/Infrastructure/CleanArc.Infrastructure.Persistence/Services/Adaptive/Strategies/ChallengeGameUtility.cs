using System.Text.Json;
using System.Text.Json.Nodes;
using CleanArc.Application.Contracts.Adaptive;

namespace CleanArc.Infrastructure.Persistence.Services.Adaptive.Strategies;

internal static class ChallengeGameUtility
{
    public static readonly string[] SyllableHints =
    {
        "show_syllable_count",
        "reveal_first_syllable",
        "show_syllable_pattern"
    };
    public const string SpellCatcherAudioLanguage = "ms-MY";

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string DetectLanguage(AdaptiveChallengeItemDto item)
    {
        if (!string.IsNullOrWhiteSpace(item.Language))
            return item.Language.ToLowerInvariant().Trim();

        return DetectLanguage(item.Word);
    }

    public static string DetectLanguage(string word, string? defaultLanguage = null)
    {
        if (string.IsNullOrWhiteSpace(word))
            return defaultLanguage ?? "ms";

        // Chinese character range check
        if (word.Any(ch => ch >= 0x4e00 && ch <= 0x9fff))
            return "zh";

        // Simple default checks
        var clean = word.Trim().ToLowerInvariant();
        if (clean.Contains("the") || clean.Contains("and") || clean.Contains("book") || clean.Contains("pencil"))
            return "en";

        return defaultLanguage ?? "ms";
    }

    public static List<string> ParseCorrectSyllables(AdaptiveChallengeItemDto item, string targetWord, string language)
    {
        if (language == "zh")
        {
            return targetWord
                .Where(ch => !char.IsWhiteSpace(ch))
                .Select(ch => ch.ToString())
                .ToList();
        }
        if (language == "en")
        {
            return targetWord
                .Where(char.IsLetter)
                .Select(ch => ch.ToString())
                .ToList();
        }

        var parsed = ParseJsonArray(item.SyllablesJson);
        if (parsed.Count == 0 && !string.IsNullOrWhiteSpace(item.SyllableText))
        {
            parsed = item.SyllableText
                .Split(new[] { '/', ' ', '\t', '\r', '\n' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(NormalizeSyllable)
                .Where(s => s.Length > 0)
                .ToList();
        }

        var normalizedTarget = NormalizeSyllable(targetWord);
        var syllables = parsed
            .Select(NormalizeSyllable)
            .Where(s => s.Length > 0 && !string.Equals(s, normalizedTarget, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return syllables.Count > 0 ? syllables : SplitFallbackSyllables(targetWord);
    }

    public static List<string> SplitFallbackSyllables(string targetWord)
    {
        var normalized = NormalizeSyllable(targetWord);
        if (normalized.Length <= 2)
            return normalized.Select(character => character.ToString()).ToList();

        var chunks = new List<string>();
        for (var index = 0; index < normalized.Length; index += 2)
        {
            chunks.Add(normalized.Substring(index, Math.Min(2, normalized.Length - index)));
        }

        return chunks;
    }

    public static List<string> ParseJsonArray(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new List<string>();

        try
        {
            var parsed = JsonSerializer.Deserialize<List<string>>(raw, JsonOptions);
            return parsed?.Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static readonly string[] CommonChineseCharacters = new[]
    {
        "熊", "猫", "狗", "猫", "鱼", "鸟", "花", "草", "树", "木", "水", "火", "山", "石", "田", "土",
        "天", "地", "人", "父", "母", "兄", "弟", "姐", "妹", "大", "小", "多", "少", "长", "短", "高",
        "矮", "胖", "瘦", "新", "旧", "好", "坏", "对", "错", "真", "假", "有", "无", "出", "入", "上",
        "下", "左", "右", "前", "后", "东", "西", "南", "北", "春", "夏", "秋", "冬", "风", "雨", "雷",
        "电", "云", "雾", "冰", "雪", "日", "月", "星", "光", "阴", "晴", "暖", "冷", "热", "风", "沙"
    };

    public static List<string> GenerateDistractors(
        IReadOnlyList<string> correctSyllables,
        string targetWord,
        int targetCount,
        string language)
    {
        if (language == "zh")
        {
            var correctSet = new HashSet<string>(correctSyllables, StringComparer.OrdinalIgnoreCase);
            var candidates = CommonChineseCharacters
                .Where(ch => !correctSet.Contains(ch))
                .ToList();
            return DeterministicShuffle(candidates, $"{targetWord}|fallback_zh")
                .Take(targetCount)
                .ToList();
        }
        if (language == "en")
        {
            var correctSet = new HashSet<string>(correctSyllables, StringComparer.OrdinalIgnoreCase);
            var letters = "abcdefghijklmnopqrstuvwxyz"
                .Select(ch => ch.ToString())
                .Where(l => !correctSet.Contains(l))
                .ToList();
            return DeterministicShuffle(letters, $"{targetWord}|fallback_en")
                .Take(targetCount)
                .ToList();
        }

        var generated = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < correctSyllables.Count; index++)
        {
            var syllable = correctSyllables[index];
            foreach (var candidate in CreatePhoneticCandidates(syllable, correctSyllables, index, targetWord))
            {
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;
                if (correctSyllables.Contains(candidate, StringComparer.OrdinalIgnoreCase))
                    continue;
                if (!IsValidDistractor(candidate, targetWord))
                    continue;
                generated.Add(candidate);
            }
        }

        var ranked = generated
            .OrderByDescending(candidate => SimilarityScore(candidate, correctSyllables))
            .ThenBy(candidate => candidate, StringComparer.Ordinal)
            .Take(Math.Max(targetCount, 1))
            .ToList();

        if (ranked.Count < targetCount)
        {
            var existing = new HashSet<string>(
                ranked.Concat(correctSyllables),
                StringComparer.OrdinalIgnoreCase);
            foreach (var fallback in GenerateFallbackSyllableDistractors(targetWord, existing))
            {
                if (ranked.Count >= targetCount)
                    break;
                ranked.Add(fallback);
                existing.Add(fallback);
            }
        }

        return ranked;
    }

    private static IEnumerable<string> CreatePhoneticCandidates(
        string syllable,
        IReadOnlyList<string> correctSyllables,
        int index,
        string targetWord)
    {
        var normalized = NormalizeSyllable(syllable);
        if (string.IsNullOrWhiteSpace(normalized))
            yield break;

        foreach (var vowelSwap in VowelSwaps(normalized))
            yield return vowelSwap;

        foreach (var consonantSwap in ConsonantSwaps(normalized))
            yield return consonantSwap;

        foreach (var codaVariant in CodaVariants(normalized, index))
            yield return codaVariant;
    }

    private static IEnumerable<string> VowelSwaps(string syllable)
    {
        const string vowels = "aeiou";
        var chars = syllable.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (!vowels.Contains(chars[i]))
                continue;

            foreach (var vowel in vowels)
            {
                if (vowel == chars[i])
                    continue;

                var mutated = (char[])chars.Clone();
                mutated[i] = vowel;
                yield return new string(mutated);
            }
        }
    }

    private static IEnumerable<string> ConsonantSwaps(string syllable)
    {
        var swaps = new[] { 'b', 'p', 'm', 'k', 't', 's', 'h', 'l', 'r', 'n', 'g' };
        if (syllable.Length == 0)
            yield break;

        var first = syllable[0];
        foreach (var swap in swaps)
        {
            if (swap == first)
                continue;

            yield return swap + syllable[1..];
        }
    }

    private static IEnumerable<string> CodaVariants(string syllable, int index)
    {
        if (!TryParseMalaySyllableShape(syllable, out var onset, out var vowel, out var coda))
            yield break;

        var codas = coda.Length == 0
            ? new[] { "n", "ng", "r", "l", "m" }
            : new[] { "n", "ng", "r", "l", "m", "t", "h" };

        foreach (var nextCoda in codas)
        {
            if (string.Equals(nextCoda, coda, StringComparison.OrdinalIgnoreCase))
                continue;

            yield return $"{onset}{vowel}{nextCoda}";
        }

        if (index == 0 && string.Equals(syllable, "be", StringComparison.OrdinalIgnoreCase))
            yield return "ber";
    }

    private static IEnumerable<string> GenerateFallbackSyllableDistractors(
        string targetWord,
        HashSet<string> existing)
    {
        var seedSyllables = new[]
        {
            "ba", "be", "bi", "bo", "bu",
            "ma", "me", "mi", "mo", "mu",
            "pa", "pe", "sa", "se", "ka", "ke", "ta", "te"
        };

        foreach (var candidate in DeterministicShuffle(seedSyllables, $"{targetWord}|fallback"))
        {
            if (existing.Contains(candidate))
                continue;
            if (!IsValidDistractor(candidate, targetWord))
                continue;

            yield return candidate;
        }
    }

    private static bool IsValidDistractor(string candidate, string targetWord)
    {
        var normalized = NormalizeSyllable(candidate);
        var normalizedTarget = NormalizeSyllable(targetWord);
        return normalized.Length > 0
            && normalized.All(char.IsLetter)
            && !string.Equals(normalized, normalizedTarget, StringComparison.OrdinalIgnoreCase)
            && !normalizedTarget.Contains(normalized, StringComparison.OrdinalIgnoreCase)
            && TryParseMalaySyllableShape(normalized, out _, out _, out _);
    }

    private static bool TryParseMalaySyllableShape(
        string syllable,
        out string onset,
        out string vowel,
        out string coda)
    {
        onset = string.Empty;
        vowel = string.Empty;
        coda = string.Empty;

        var normalized = NormalizeSyllable(syllable);
        var vowelIndex = normalized.IndexOfAny(new[] { 'a', 'e', 'i', 'o', 'u' });
        if (vowelIndex < 0)
            return false;

        onset = normalized[..vowelIndex];
        vowel = normalized[vowelIndex].ToString();
        coda = normalized[(vowelIndex + 1)..];

        return onset.All(IsConsonant)
            && coda.All(IsConsonant)
            && (coda.Length <= 1 || string.Equals(coda, "ng", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsConsonant(char character)
        => char.IsLetter(character) && !"aeiou".Contains(character);

    private static int SimilarityScore(string candidate, IReadOnlyList<string> correctSyllables)
    {
        var best = 0;
        foreach (var correct in correctSyllables)
        {
            var prefix = 0;
            var limit = Math.Min(candidate.Length, correct.Length);
            for (var i = 0; i < limit; i++)
            {
                if (candidate[i] != correct[i])
                    break;
                prefix++;
            }
            best = Math.Max(best, prefix);
        }
        return best;
    }

    public static SpellCatcherSpecDto BuildSpellCatcherSpec(AdaptiveChallengeItemDto item, bool needsMeaningSupport, bool needsSyllableSupport, bool needsAudioSupport)
    {
        var targetWord = (string.IsNullOrWhiteSpace(item.BmText) ? item.Word : item.BmText).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(targetWord))
            throw new InvalidOperationException("Spell Catcher requires a valid target word.");

        var difficulty = Math.Clamp(item.DifficultyLevel, 1, 3);
        var letterPool = targetWord.Where(char.IsLetter).Select(ch => ch.ToString()).ToList();
        if (letterPool.Count == 0)
            throw new InvalidOperationException("Spell Catcher target word must contain letters.");

        var scrambledLetters = BuildScrambledLetters(letterPool, targetWord);
        if (string.Equals(scrambledLetters, targetWord.ToUpperInvariant(), StringComparison.Ordinal))
            throw new InvalidOperationException("Spell Catcher scramble must not match the original word.");

        var distractorCount = difficulty switch
        {
            1 => 0,
            2 => 2,
            _ => 4
        };
        var distractors = GenerateLetterDistractors(letterPool, targetWord, distractorCount);
        var pool = DeterministicShuffle(
            letterPool.Concat(distractors).ToList(),
            $"{targetWord}|spell|{difficulty}");

        var language = DetectLanguage(item);
        var syllables = ParseCorrectSyllables(item, targetWord, language);
        var meaning = new SpellCatcherMeaningDto(item.EnText ?? item.MeaningText ?? string.Empty, item.ZhText ?? string.Empty);
        var showMeaning = difficulty < 3 || needsMeaningSupport;
        var showFirstLetter = difficulty == 1;
        var showSyllableHint = needsSyllableSupport;
        var playAudio = difficulty == 1 || needsAudioSupport;
        var level2Hint = showFirstLetter
            ? BuildFirstLetterHint(targetWord)
            : showSyllableHint && syllables.Count > 0
                ? string.Join("/", syllables)
                : BuildFirstLetterHint(targetWord);
        var level1Hint = $"{meaning.En} {meaning.Zh}".Trim();
        if (string.IsNullOrWhiteSpace(level1Hint))
            level1Hint = "Use the meaning clue.";

        var spec = new SpellCatcherSpecDto(
            "SPELL_CATCHER",
            targetWord,
            scrambledLetters,
            pool,
            meaning,
            syllables,
            difficulty,
            new SpellCatcherUiConfigDto(
                new SpellCatcherPreviewPhaseDto(
                    Enabled: true,
                    DurationMs: 2000,
                    ShowMeaning: showMeaning,
                    PlayAudio: playAudio),
                new SpellCatcherChallengePhaseDto(
                    ShowMeaningHint: showMeaning || needsMeaningSupport,
                    ShowFirstLetter: showFirstLetter,
                    ShowSyllableHint: showSyllableHint,
                    AllowRetry: true,
                    MaxAttempts: 3,
                    EnableTimePressure: difficulty == 3)),
            new SpellCatcherAudioConfigDto(
                TtsText: targetWord,
                Language: SpellCatcherAudioLanguage,
                ShouldAutoPlay: playAudio),
            new SpellCatcherHintsDto(
                Level1: level1Hint,
                Level2: level2Hint,
                Level3: targetWord));

        ValidateSpellCatcherSpec(spec);
        return spec;
    }

    private static string BuildFirstLetterHint(string targetWord)
    {
        if (string.IsNullOrWhiteSpace(targetWord))
            return string.Empty;
        return $"Starts with '{char.ToUpperInvariant(targetWord[0])}'";
    }

    public static SyllableSushiSpecDto BuildSyllableSushiSpec(AdaptiveChallengeItemDto item)
    {
        var targetWord = (string.IsNullOrWhiteSpace(item.BmText) ? item.Word : item.BmText).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(targetWord))
            throw new InvalidOperationException("Syllable Sushi requires a valid target word.");

        var language = DetectLanguage(item);

        var correctSyllables = ParseCorrectSyllables(item, targetWord, language);
        if (correctSyllables.Count == 0)
            throw new InvalidOperationException($"Syllable Sushi requires syllables for word '{targetWord}'.");

        var difficulty = Math.Clamp(item.DifficultyLevel, 1, 3);
        var distractorTarget = difficulty switch
        {
            1 => 3,
            2 => 4,
            _ => 5
        };

        var distractors = GenerateDistractors(correctSyllables, targetWord, distractorTarget, language);
        if (distractors.Count == 0)
            throw new InvalidOperationException($"Syllable Sushi distractor generation failed for word '{targetWord}'.");

        var syllablePool = DeterministicShuffle(
            correctSyllables.Concat(distractors).ToList(),
            $"{targetWord}|{difficulty}");

        if (syllablePool.Count <= correctSyllables.Count)
            throw new InvalidOperationException("Syllable Sushi requires distractors in the syllable pool.");

        return new SyllableSushiSpecDto(
            "SYLLABLE_SUSHI",
            targetWord,
            new SyllableSushiMeaningDto(item.EnText ?? item.MeaningText ?? string.Empty, item.ZhText ?? string.Empty),
            correctSyllables,
            syllablePool,
            Enumerable.Range(0, correctSyllables.Count).ToList(),
            distractors,
            difficulty,
            new SyllableSushiUiConfigDto(
                true,
                0,
                true,
                3,
                SyllableHints));
    }

    public static string BuildScrambledLetters(IReadOnlyList<string> letters, string targetWord)
    {
        var targetUpper = targetWord.ToUpperInvariant();
        var shuffled = DeterministicShuffle(letters, $"{targetWord}|scramble");
        var value = string.Concat(shuffled).ToUpperInvariant();
        if (string.Equals(value, targetUpper, StringComparison.Ordinal))
        {
            var reversed = letters.AsEnumerable().Reverse().ToList();
            value = string.Concat(reversed).ToUpperInvariant();
        }
        return value;
    }

    public static List<T> DeterministicShuffle<T>(IReadOnlyList<T> list, string seed)
    {
        var hash = GetDeterministicHash(seed);
        var random = new Random(hash);
        return list.OrderBy(_ => random.Next()).ToList();
    }

    private static int GetDeterministicHash(string value)
    {
        unchecked
        {
            var hash = 17;
            foreach (var ch in value)
                hash = (hash * 31) + ch;
            return hash;
        }
    }

    public static List<string> GenerateLetterDistractors(
        IReadOnlyList<string> correctLetters,
        string targetWord,
        int count)
    {
        if (count <= 0)
            return new List<string>();

        var alphabet = "abcdefghijklmnopqrstuvwxyz".Select(c => c.ToString()).ToList();
        var correctSet = new HashSet<string>(correctLetters, StringComparer.OrdinalIgnoreCase);
        var candidates = alphabet.Where(c => !correctSet.Contains(c)).ToList();
        return DeterministicShuffle(candidates, $"{targetWord}|letter_fallback")
            .Take(count)
            .ToList();
    }

    public static void ValidateSpellCatcherSpec(SpellCatcherSpecDto spec)
    {
        if (string.IsNullOrWhiteSpace(spec.TargetWord))
            throw new InvalidOperationException("Spell Catcher target word cannot be empty.");
        if (string.IsNullOrWhiteSpace(spec.ScrambledLetters))
            throw new InvalidOperationException("Spell Catcher scrambled target word cannot be empty.");
        if (spec.LetterPool == null || spec.LetterPool.Count == 0)
            throw new InvalidOperationException("Spell Catcher letter pool cannot be empty.");
    }

    public static string NormalizeSyllable(string value)
        => value.Trim().ToLowerInvariant().Replace(" ", string.Empty);

    public static IReadOnlyList<string> ExtractLearningTerms(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();

        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "and", "are", "as", "at", "be", "for", "from", "in", "is", "it", "of", "on",
            "or", "the", "this", "to", "with", "year", "words", "word", "challenge", "practice", "generate"
        };

        return text
            .Split(new[] { ' ', '\n', '\r', '\t', ',', '.', ';', ':', '/', '\\', '|', '-', '(', ')', '[', ']', '{', '}', '"', '\'' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length is >= 2 and <= 32)
            .Where(token => !token.Any(char.IsDigit))
            .Where(token => !stopWords.Contains(token))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();
    }

    public static string NormalizeTemplateCode(string? preferred, string? objective, string? focus)
    {
        var candidate = preferred?.Trim().ToUpperInvariant();
        if (candidate is "SPELL_CATCHER" or "VOICE_BRIDGE" or "SYLLABLE_SUSHI" or "TRANSLATION")
            return candidate;

        var signal = $"{objective} {focus}".ToLowerInvariant();
        if (signal.Contains("syllable")) return "SYLLABLE_SUSHI";
        if (signal.Contains("voice") || signal.Contains("pronunciation") || signal.Contains("oral")) return "VOICE_BRIDGE";
        if (signal.Contains("translate") || signal.Contains("translation")) return "TRANSLATION";
        return "SPELL_CATCHER";
    }

    public static string ToGameKey(string templateCode) => templateCode switch
    {
        "SPELL_CATCHER" => "spell_catcher",
        "VOICE_BRIDGE" => "voice_bridge",
        "SYLLABLE_SUSHI" => "syllable_sushi",
        "TRANSLATION" => "translation",
        _ => templateCode.ToLowerInvariant()
    };

    public static string ToCategory(string templateCode) => templateCode switch
    {
        "VOICE_BRIDGE" => "SPEAKING",
        "SYLLABLE_SUSHI" => "STRUCTURE",
        "SPELL_CATCHER" => "RECALL",
        "TRANSLATION" => "RECALL",
        _ => "RECALL"
    };

    public static JsonNode? TryParseJson(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        try { return JsonNode.Parse(raw); }
        catch { return null; }
    }
}
