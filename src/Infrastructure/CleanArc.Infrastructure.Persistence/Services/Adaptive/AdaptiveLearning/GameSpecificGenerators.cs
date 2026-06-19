using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Application.Contracts.AdaptiveLearning;

namespace CleanArc.Infrastructure.Persistence.Services.Adaptive.AdaptiveLearning;

public class VoiceBridgeChallengeGenerator : CleanArc.Application.Contracts.AdaptiveLearning.IChallengeGenerator
{
    private readonly CleanArc.Application.Contracts.Adaptive.IChallengeGenerator _baseGenerator;

    public VoiceBridgeChallengeGenerator(CleanArc.Application.Contracts.Adaptive.IChallengeGenerator baseGenerator)
    {
        _baseGenerator = baseGenerator;
    }

    public string GameType => "VOICE_BRIDGE";

    public async Task<string> GenerateContentJsonAsync(IReadOnlyList<string> words, int difficulty, CancellationToken cancellationToken)
    {
        var request = new GenerateAdaptiveChallengeRequest(
            TargetType: "class",
            StudentId: null,
            ClassId: null,
            Objective: "hardcore_practice",
            SourceType: "manual_input",
            ModuleId: null,
            PreferredGameTemplateCode: "VOICE_BRIDGE",
            LearningFocus: "hardcore speaking practice",
            ManualWords: words,
            AiPrompt: null,
            SourceText: null
        );

        var preview = await _baseGenerator.GenerateAsync(request, cancellationToken);
        return preview.ContentData;
    }
}

public class SpellCatcherChallengeGenerator : CleanArc.Application.Contracts.AdaptiveLearning.IChallengeGenerator
{
    private readonly CleanArc.Application.Contracts.Adaptive.IChallengeGenerator _baseGenerator;

    public SpellCatcherChallengeGenerator(CleanArc.Application.Contracts.Adaptive.IChallengeGenerator baseGenerator)
    {
        _baseGenerator = baseGenerator;
    }

    public string GameType => "SPELL_CATCHER";

    public async Task<string> GenerateContentJsonAsync(IReadOnlyList<string> words, int difficulty, CancellationToken cancellationToken)
    {
        var request = new GenerateAdaptiveChallengeRequest(
            TargetType: "class",
            StudentId: null,
            ClassId: null,
            Objective: "hardcore_practice",
            SourceType: "manual_input",
            ModuleId: null,
            PreferredGameTemplateCode: "SPELL_CATCHER",
            LearningFocus: "hardcore spelling recall",
            ManualWords: words,
            AiPrompt: null,
            SourceText: null
        );

        var preview = await _baseGenerator.GenerateAsync(request, cancellationToken);
        return preview.ContentData;
    }
}

public class SyllableSushiChallengeGenerator : CleanArc.Application.Contracts.AdaptiveLearning.IChallengeGenerator
{
    private readonly CleanArc.Application.Contracts.Adaptive.IChallengeGenerator _baseGenerator;

    public SyllableSushiChallengeGenerator(CleanArc.Application.Contracts.Adaptive.IChallengeGenerator baseGenerator)
    {
        _baseGenerator = baseGenerator;
    }

    public string GameType => "SYLLABLE_SUSHI";

    public async Task<string> GenerateContentJsonAsync(IReadOnlyList<string> words, int difficulty, CancellationToken cancellationToken)
    {
        var request = new GenerateAdaptiveChallengeRequest(
            TargetType: "class",
            StudentId: null,
            ClassId: null,
            Objective: "hardcore_practice",
            SourceType: "manual_input",
            ModuleId: null,
            PreferredGameTemplateCode: "SYLLABLE_SUSHI",
            LearningFocus: "hardcore syllable structure",
            ManualWords: words,
            AiPrompt: null,
            SourceText: null
        );

        var preview = await _baseGenerator.GenerateAsync(request, cancellationToken);
        return preview.ContentData;
    }
}

public class SpellingTestChallengeGenerator : CleanArc.Application.Contracts.AdaptiveLearning.IChallengeGenerator
{
    public string GameType => "SPELLING_TEST";

    public Task<string> GenerateContentJsonAsync(IReadOnlyList<string> words, int difficulty, CancellationToken cancellationToken)
    {
        // Spelling tests don't have strategy-specific configs, they just list the target words.
        var config = new
        {
            words = words,
            difficulty = difficulty,
            timeLimitSeconds = 300
        };
        return Task.FromResult(JsonSerializer.Serialize(config));
    }
}
