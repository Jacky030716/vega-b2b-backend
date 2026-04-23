using System.Text.Json;
using System.Text.Json.Nodes;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Services.Adaptive;
public class ChallengeGenerator(ApplicationDbContext dbContext) : IChallengeGenerator
{
    private static readonly string[] SyllableHints =
    {
        "show_syllable_count",
        "reveal_first_syllable",
        "reveal_full_word"
    };
    private const string SpellCatcherAudioLanguage = "ms-MY";

    public async Task<GeneratedAdaptiveChallengePreviewDto> GenerateAsync(
        GenerateAdaptiveChallengeRequest request,
        CancellationToken cancellationToken)
    {
        var templateCode = NormalizeTemplateCode(request.PreferredGameTemplateCode, request.Objective, request.LearningFocus);
        var gameKey = ToGameKey(templateCode);
        var category = ToCategory(templateCode);

        List<AdaptiveChallengeItemDto> items = new();
        SyllabusModule? module = null;
        var sourceType = request.SourceType?.Trim().ToLowerInvariant() ?? "predefined_module";

        if (sourceType == "manual_input")
        {
            items = CreateAdHocItems(request.ManualWords ?? Array.Empty<string>());
        }
        else if (sourceType == "upload")
        {
            items = CreateAdHocItems(ExtractLearningTerms(request.SourceText));
        }
        else if (sourceType == "ai_prompt")
        {
            items = CreateAdHocItems(ExtractLearningTerms(request.AiPrompt));
        }

        if (items.Count == 0 && request.ModuleId is int moduleId)
        {
            module = await dbContext.SyllabusModules.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == moduleId, cancellationToken);

            items = await dbContext.VocabularyItems.AsNoTracking()
                .Where(v => v.ModuleId == moduleId && v.IsActive)
                .OrderBy(v => v.DisplayOrder)
                .ThenBy(v => v.Word)
                .Take(12)
                .Select(v => new AdaptiveChallengeItemDto(
                    null,
                    v.Id,
                    v.Word,
                    v.NormalizedWord,
                    v.PhoneticHint ?? v.MeaningText,
                    v.MeaningText,
                    v.ExampleSentence,
                    v.SyllablesJson,
                    v.DifficultyLevel,
                    v.BmText,
                    v.ZhText,
                    v.EnText,
                    v.SyllableText,
                    v.ItemType,
                    v.DisplayOrder,
                    null,
                    null,
                    null,
                    null))
                .ToListAsync(cancellationToken);
        }

        if (items.Count == 0 && request.ManualWords?.Count > 0)
        {
            items = CreateAdHocItems(request.ManualWords);
        }

        if (items.Count == 0)
        {
            items = await GetWeakOrReviewItemsAsync(request, cancellationToken);
        }

        if (items.Count == 0)
            throw new InvalidOperationException("No vocabulary items are available for this challenge");

        var titleBase = module?.Title ?? request.Objective.Replace('_', ' ');
        var title = templateCode switch
        {
            "SPELL_CATCHER" => $"Spell Catcher: {titleBase}",
            "SYLLABLE_SUSHI" => $"Syllable Sushi: {titleBase}",
            "VOICE_BRIDGE" => $"Voice Bridge: {titleBase}",
            _ => $"Adaptive Practice: {titleBase}"
        };

        var difficulty = Math.Clamp((int)Math.Round(items.Average(i => i.DifficultyLevel)), 1, 5);
        List<SyllableSushiSpecDto>? syllableSushiSpecs = null;
        SyllableSushiSpecDto? primarySyllableSushiSpec = null;
        List<SpellCatcherSpecDto>? spellCatcherSpecs = null;
        SpellCatcherSpecDto? primarySpellCatcherSpec = null;
        Dictionary<int, SpellCatcherWeakness>? spellCatcherWeakness = null;

        if (templateCode == "SPELL_CATCHER" && request.StudentId is int studentId)
        {
            spellCatcherWeakness = await BuildSpellCatcherWeaknessMapAsync(studentId, items, cancellationToken);
        }

        if (templateCode == "SPELL_CATCHER")
        {
            spellCatcherSpecs = items
                .Select(item => BuildSpellCatcherSpec(item, ResolveSpellCatcherWeakness(item, spellCatcherWeakness)))
                .ToList();
            primarySpellCatcherSpec = spellCatcherSpecs.FirstOrDefault();
            items = items.Zip(spellCatcherSpecs, (item, spec) => item with
            {
                DifficultyLevel = spec.DifficultyLevel,
                SpellCatcherSpecJson = JsonSerializer.Serialize(spec, JsonOptions)
            }).ToList();
        }

        if (templateCode == "SYLLABLE_SUSHI")
        {
            syllableSushiSpecs = items.Select(BuildSyllableSushiSpec).ToList();
            primarySyllableSushiSpec = syllableSushiSpecs.FirstOrDefault();
            items = items.Zip(syllableSushiSpecs, (item, spec) => item with
            {
                SyllablePoolJson = JsonSerializer.Serialize(spec.SyllablePool, JsonOptions),
                DistractorsJson = JsonSerializer.Serialize(spec.Distractors, JsonOptions),
                CorrectOrderJson = JsonSerializer.Serialize(spec.CorrectOrder, JsonOptions),
                DifficultyLevel = spec.DifficultyLevel
            }).ToList();
        }

        var contentData = JsonSerializer.Serialize(new
        {
            gameTemplateCode = templateCode,
            category,
            objective = request.Objective,
            moduleId = request.ModuleId,
            spellCatcherSpec = primarySpellCatcherSpec,
            spellCatcherSpecs = spellCatcherSpecs,
            syllableSushiSpec = primarySyllableSushiSpec,
            syllableSushiSpecs = syllableSushiSpecs,
            items = items.Select(i => new
            {
                vocabularyItemId = i.VocabularyItemId,
                word = i.Word,
                normalizedWord = i.NormalizedWord,
                hint = i.Hint,
                meaningText = i.MeaningText,
                exampleSentence = i.ExampleSentence,
                syllablesJson = TryParseJson(i.SyllablesJson) ?? JsonNode.Parse("[]"),
                difficultyLevel = i.DifficultyLevel,
                bmText = i.BmText,
                zhText = i.ZhText,
                enText = i.EnText,
                syllableText = i.SyllableText,
                itemType = i.ItemType,
                displayOrder = i.DisplayOrder,
                syllablePoolJson = TryParseJson(i.SyllablePoolJson) ?? JsonNode.Parse("[]"),
                distractorsJson = TryParseJson(i.DistractorsJson) ?? JsonNode.Parse("[]"),
                correctOrderJson = TryParseJson(i.CorrectOrderJson) ?? JsonNode.Parse("[]"),
                spellCatcherSpecJson = TryParseJson(i.SpellCatcherSpecJson)
            })
        }, JsonOptions);

        return new GeneratedAdaptiveChallengePreviewDto(
            title,
            $"Rule-based {category.ToLowerInvariant()} practice generated from {request.SourceType}.",
            templateCode,
            gameKey,
            category,
            difficulty,
            request.ModuleId,
            request.StudentId,
            request.ClassId,
            request.Objective,
            request.SourceType,
            contentData,
            JsonSerializer.Serialize(new
            {
                request.TargetType,
                request.Objective,
                request.SourceType,
                request.LearningFocus
            }, JsonOptions),
            items,
            primarySyllableSushiSpec,
            primarySpellCatcherSpec);
    }

    private async Task<List<AdaptiveChallengeItemDto>> GetWeakOrReviewItemsAsync(
        GenerateAdaptiveChallengeRequest request,
        CancellationToken cancellationToken)
    {
        if (request.StudentId is null)
            return new List<AdaptiveChallengeItemDto>();

        var now = DateTime.UtcNow;
        var masteryQuery = dbContext.StudentWordMasteries.AsNoTracking()
            .Include(m => m.VocabularyItem)
            .Where(m => m.StudentId == request.StudentId.Value);

        if (request.Objective.Contains("overdue", StringComparison.OrdinalIgnoreCase))
        {
            masteryQuery = masteryQuery.Where(m => m.NextReviewAt != null && m.NextReviewAt <= now);
        }
        else
        {
            masteryQuery = masteryQuery.Where(m => m.MasteryScore < 65);
        }

        return await masteryQuery
            .OrderBy(m => m.MasteryScore)
            .ThenBy(m => m.NextReviewAt)
            .Take(12)
            .Select(m => new AdaptiveChallengeItemDto(
                null,
                m.VocabularyItemId,
                m.VocabularyItem.Word,
                m.VocabularyItem.NormalizedWord,
                m.VocabularyItem.PhoneticHint ?? m.VocabularyItem.MeaningText,
                m.VocabularyItem.MeaningText,
                m.VocabularyItem.ExampleSentence,
                m.VocabularyItem.SyllablesJson,
                m.VocabularyItem.DifficultyLevel,
                m.VocabularyItem.BmText,
                m.VocabularyItem.ZhText,
                m.VocabularyItem.EnText,
                m.VocabularyItem.SyllableText,
                m.VocabularyItem.ItemType,
                m.VocabularyItem.DisplayOrder,
                null,
                null,
                null,
                null))
            .ToListAsync(cancellationToken);
    }

    private static List<AdaptiveChallengeItemDto> CreateAdHocItems(IEnumerable<string> words) =>
        words
            .Select(word => word.Trim())
            .Where(word => word.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .Select(word => new AdaptiveChallengeItemDto(
                null,
                null,
                word,
                SyllabusModuleService.NormalizeWord(word),
                null,
                    null,
                    null,
                    "[]",
                    1,
                    word,
                    null,
                    null,
                    null,
                    "WORD",
                    null,
                    null,
                    null,
                    null,
                    null))
            .ToList();

    private async Task<Dictionary<int, SpellCatcherWeakness>> BuildSpellCatcherWeaknessMapAsync(
        int studentId,
        IReadOnlyList<AdaptiveChallengeItemDto> items,
        CancellationToken cancellationToken)
    {
        var vocabularyIds = items
            .Where(i => i.VocabularyItemId.HasValue)
            .Select(i => i.VocabularyItemId!.Value)
            .Distinct()
            .ToList();

        if (vocabularyIds.Count == 0)
            return new Dictionary<int, SpellCatcherWeakness>();

        var mastery = await dbContext.StudentWordMasteries.AsNoTracking()
            .Where(m => m.StudentId == studentId && vocabularyIds.Contains(m.VocabularyItemId))
            .ToListAsync(cancellationToken);

        return mastery.ToDictionary(
            m => m.VocabularyItemId,
            m => new SpellCatcherWeakness(
                m.MasteryScore < 65,
                (m.WeaknessTagsJson ?? string.Empty).Contains("syllable_assembly", StringComparison.OrdinalIgnoreCase),
                m.MasteryScore < 50 || m.TotalAttempts < 3));
    }

    private static SpellCatcherWeakness ResolveSpellCatcherWeakness(
        AdaptiveChallengeItemDto item,
        IReadOnlyDictionary<int, SpellCatcherWeakness>? weaknessMap)
    {
        if (item.VocabularyItemId is int vocabularyId && weaknessMap is not null && weaknessMap.TryGetValue(vocabularyId, out var profile))
            return profile;

        return new SpellCatcherWeakness(true, false, true);
    }

    internal static SpellCatcherSpecDto BuildSpellCatcherSpec(AdaptiveChallengeItemDto item, SpellCatcherWeakness weakness)
    {
        var targetWord = (item.BmText ?? item.Word).Trim().ToLowerInvariant();
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

        var syllables = ParseCorrectSyllables(item, targetWord);
        var meaning = new SpellCatcherMeaningDto(item.EnText ?? item.MeaningText ?? string.Empty, item.ZhText ?? string.Empty);
        var showMeaning = difficulty < 3 || weakness.NeedsMeaningSupport;
        var showFirstLetter = difficulty == 1;
        var showSyllableHint = weakness.NeedsSyllableSupport;
        var playAudio = difficulty == 1 || weakness.NeedsAudioSupport;
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
                    ShowMeaningHint: showMeaning || weakness.NeedsMeaningSupport,
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

    private static string BuildScrambledLetters(IReadOnlyList<string> letters, string targetWord)
    {
        var targetUpper = targetWord.ToUpperInvariant();
        var scrambled = DeterministicShuffle(letters, $"{targetWord}|scramble")
            .Select(letter => letter.ToUpperInvariant())
            .ToList();

        if (string.Equals(string.Concat(scrambled), targetUpper, StringComparison.Ordinal))
            scrambled = RotateLeft(scrambled);

        if (IsTrivialSingleSwap(scrambled, targetUpper))
            scrambled = RotateLeft(scrambled);

        return string.Concat(scrambled);
    }

    private static bool IsTrivialSingleSwap(IReadOnlyList<string> scrambled, string targetUpper)
    {
        if (scrambled.Count != targetUpper.Length)
            return false;

        var mismatch = 0;
        for (var i = 0; i < scrambled.Count; i++)
        {
            if (!string.Equals(scrambled[i], targetUpper[i].ToString(), StringComparison.Ordinal))
                mismatch++;
        }

        return mismatch <= 2;
    }

    private static List<string> GenerateLetterDistractors(
        IReadOnlyList<string> letters,
        string targetWord,
        int targetCount)
    {
        if (targetCount <= 0)
            return new List<string>();

        var existing = new HashSet<string>(letters, StringComparer.OrdinalIgnoreCase);
        var candidates = new List<string>();
        var variants = new[] { "a", "e", "i", "o", "u", "n", "r", "s", "t", "k", "m", "p", "h", "l", "g", "b" };
        foreach (var variant in variants)
        {
            if (!existing.Contains(variant))
                candidates.Add(variant);
        }

        return DeterministicShuffle(candidates, $"{targetWord}|distractors")
            .Take(targetCount)
            .ToList();
    }

    private static string BuildFirstLetterHint(string targetWord)
    {
        if (string.IsNullOrWhiteSpace(targetWord))
            return string.Empty;

        var visible = targetWord[0];
        var masked = new string('_', Math.Max(1, targetWord.Length - 1));
        return $"{visible}{masked}";
    }

    private static void ValidateSpellCatcherSpec(SpellCatcherSpecDto spec)
    {
        if (!spec.UiConfig.PreviewPhase.Enabled)
            throw new InvalidOperationException("Spell Catcher preview phase is required.");
        if (spec.Hints is null || string.IsNullOrWhiteSpace(spec.Hints.Level1) || string.IsNullOrWhiteSpace(spec.Hints.Level2) || string.IsNullOrWhiteSpace(spec.Hints.Level3))
            throw new InvalidOperationException("Spell Catcher requires three hint levels.");
        if (string.Equals(spec.ScrambledLetters, spec.TargetWord, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Spell Catcher scrambled letters must differ from target word.");
        if (spec.UiConfig.ChallengePhase.MaxAttempts <= 0)
            throw new InvalidOperationException("Spell Catcher recall phase must require attempts.");
    }

    internal static SyllableSushiSpecDto BuildSyllableSushiSpec(AdaptiveChallengeItemDto item)
    {
        var targetWord = (item.BmText ?? item.Word).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(targetWord))
            throw new InvalidOperationException("Syllable Sushi requires a valid target word.");

        var correctSyllables = ParseCorrectSyllables(item, targetWord);
        if (correctSyllables.Count == 0)
            throw new InvalidOperationException($"Syllable Sushi requires syllables for word '{targetWord}'.");

        var difficulty = Math.Clamp(item.DifficultyLevel, 1, 3);
        var distractorTarget = difficulty switch
        {
            1 => 1,
            2 => 2,
            _ => 4
        };

        var distractors = GenerateDistractors(correctSyllables, targetWord, distractorTarget);
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
                2000,
                true,
                3,
                SyllableHints));
    }

    private static List<string> ParseCorrectSyllables(AdaptiveChallengeItemDto item, string targetWord)
    {
        var parsed = ParseJsonArray(item.SyllablesJson);
        if (parsed.Count == 0 && !string.IsNullOrWhiteSpace(item.SyllableText))
        {
            parsed = item.SyllableText
                .Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(NormalizeSyllable)
                .Where(s => s.Length > 0)
                .ToList();
        }

        return parsed
            .Select(NormalizeSyllable)
            .Where(s => s.Length > 0)
            .ToList();
    }

    private static List<string> ParseJsonArray(string? raw)
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

    private static List<string> GenerateDistractors(
        IReadOnlyList<string> correctSyllables,
        string targetWord,
        int targetCount)
    {
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
                if (!IsValidDistractor(candidate))
                    continue;
                generated.Add(candidate);
            }
        }

        var ranked = generated
            .OrderByDescending(candidate => SimilarityScore(candidate, correctSyllables))
            .ThenBy(candidate => candidate, StringComparer.Ordinal)
            .Take(Math.Max(targetCount, 1))
            .ToList();

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

        if (normalized.Length > 1)
            yield return normalized[..^1];

        if (index < correctSyllables.Count - 1)
            yield return normalized + NormalizeSyllable(correctSyllables[index + 1]);

        if (targetWord.Length >= 4)
        {
            var mid = targetWord.Length / 2;
            yield return NormalizeSyllable(targetWord[..mid]);
            yield return NormalizeSyllable(targetWord[mid..]);
        }
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
        var swaps = new[] { 'b', 'p', 'm', 'k', 't', 's' };
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

    private static bool IsValidDistractor(string candidate)
        => candidate.Length > 0 && candidate.All(char.IsLetter);

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

    private static List<string> DeterministicShuffle(IReadOnlyList<string> values, string seed)
        => values
            .Select((value, index) => new { value, index, rank = StableHash($"{seed}|{value}|{index}") })
            .OrderBy(x => x.rank)
            .ThenBy(x => x.value, StringComparer.Ordinal)
            .Select(x => x.value)
            .ToList();

    private static List<string> RotateLeft(IReadOnlyList<string> values)
    {
        if (values.Count <= 1)
            return values.ToList();
        return values.Skip(1).Concat(values.Take(1)).ToList();
    }

    private static int StableHash(string value)
    {
        unchecked
        {
            var hash = 17;
            foreach (var ch in value)
                hash = (hash * 31) + ch;
            return hash;
        }
    }

    private static string NormalizeSyllable(string value)
        => value.Trim().ToLowerInvariant().Replace(" ", string.Empty);

    private static IReadOnlyList<string> ExtractLearningTerms(string? text)
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

    internal static string NormalizeTemplateCode(string? preferred, string? objective, string? focus)
    {
        var candidate = preferred?.Trim().ToUpperInvariant();
        if (candidate is "SPELL_CATCHER" or "VOICE_BRIDGE" or "SYLLABLE_SUSHI")
            return candidate;

        var signal = $"{objective} {focus}".ToLowerInvariant();
        if (signal.Contains("syllable")) return "SYLLABLE_SUSHI";
        if (signal.Contains("voice") || signal.Contains("pronunciation") || signal.Contains("oral")) return "VOICE_BRIDGE";
        return "SPELL_CATCHER";
    }

    internal static string ToGameKey(string templateCode) => templateCode switch
    {
        "SPELL_CATCHER" => "spell_catcher",
        "VOICE_BRIDGE" => "voice_bridge",
        "SYLLABLE_SUSHI" => "syllable_sushi",
        "word_bridge" => "word_bridge",
        "word_pair" => "word_pair",
        "magic_backpack" => "magic_backpack",
        _ => templateCode.ToLowerInvariant()
    };

    internal static string ToCategory(string templateCode) => templateCode switch
    {
        "VOICE_BRIDGE" => "SPEAKING",
        "SYLLABLE_SUSHI" => "STRUCTURE",
        "word_pair" => "RECOGNITION",
        _ => "RECALL"
    };

    internal static JsonNode? TryParseJson(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        try { return JsonNode.Parse(raw); }
        catch { return null; }
    }

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    internal readonly record struct SpellCatcherWeakness(
        bool NeedsMeaningSupport,
        bool NeedsSyllableSupport,
        bool NeedsAudioSupport);
}

