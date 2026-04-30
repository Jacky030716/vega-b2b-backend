using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Application.Models.Common;
using CleanArc.Infrastructure.Persistence.Services.AI;
using CleanArc.Infrastructure.Persistence.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

#nullable enable

namespace CleanArc.Tests.Setup.Features.Adaptive;

public class ChallengeAiPipelineServiceTests
{
    [Fact]
    public async Task GenerateStructuredVocabularyFromInputAsync_KeepsCustomDraftShape()
    {
        var service = CreateService("""
        {
          "title": "Word Pair: Animals",
          "description": "Match animal words.",
          "content": {
            "pairs": [
              { "key": "kucing", "value": "cat" },
              { "key": "anjing", "value": "dog" },
              { "key": "burung", "value": "bird" }
            ],
            "isBilingual": true
          }
        }
        """);

        var result = await service.GenerateStructuredVocabularyFromInputAsync(
            new CustomVocabularyGenerationRequest("word_pair", "animals", "animals context", 7, 9),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal("word_pair", result.Result.DraftSchema);
        Assert.Contains("kucing", result.Result.DraftPayload);
        Assert.Contains("cat", result.Result.PlayableContentData);
        Assert.Equal(42, result.Result.AiAuditLogId);
    }

    [Fact]
    public async Task GenerateStructuredVocabularyFromInputAsync_RejectsInvalidCustomDraft()
    {
        var audit = Substitute.For<IAiAuditService>();
        var service = CreateService("""
        {
          "title": "Word Pair: Animals",
          "description": "Match animal words.",
          "content": {
            "pairs": [
              { "key": "kucing", "value": "cat" }
            ]
          }
        }
        """, audit);

        var result = await service.GenerateStructuredVocabularyFromInputAsync(
            new CustomVocabularyGenerationRequest("word_pair", "animals", "animals context", 7, 9),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("at least 3 pairs", result.ErrorMessage);
        await audit.Received(1).CompleteAsync(
            42,
            Arg.Any<string>(),
            "{}",
            AiValidationStatuses.Invalid,
            Arg.Is<IReadOnlyList<string>>(errors => errors.Any(error => error.Contains("at least 3 pairs"))),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateModuleChallengePlanAsync_ReturnsAuditLogIdOnValidPlan()
    {
        var service = CreateService("""
        {
          "selectedWords": ["buku"],
          "recommendedGameType": "SPELL_CATCHER",
          "difficultyLevel": 1,
          "reason": "Weak spelling practice.",
          "focusType": "WEAKNESS"
        }
        """);

        var result = await service.GenerateModuleChallengePlanAsync(CreatePlanRequest(new[] { "buku" }), CancellationToken.None);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(42, result.Result.AiAuditLogId);
    }

    [Fact]
    public async Task GenerateModuleChallengePlanAsync_RejectsHallucinatedWords()
    {
        var service = CreateService("""
        {
          "selectedWords": ["buku", "not-in-module"],
          "recommendedGameType": "SPELL_CATCHER",
          "difficultyLevel": 1,
          "reason": "test",
          "focusType": "PRACTICE"
        }
        """);

        var result = await service.GenerateModuleChallengePlanAsync(CreatePlanRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("outside the module", result.ErrorMessage);
    }

    [Fact]
    public async Task GenerateModuleChallengePlanAsync_RejectsMissingRequiredFields()
    {
        var service = CreateService("""
        {
          "selectedWords": ["buku"],
          "recommendedGameType": "SPELL_CATCHER",
          "difficultyLevel": 1
        }
        """);

        var result = await service.GenerateModuleChallengePlanAsync(CreatePlanRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("reason", result.ErrorMessage);
        Assert.Equal(42, result.Result.AiAuditLogId);
    }

    [Fact]
    public async Task GenerateModuleChallengePlanAsync_RejectsPlanThatIgnoresWeakWords()
    {
        var service = CreateService("""
        {
          "selectedWords": ["meja"],
          "recommendedGameType": "SPELL_CATCHER",
          "difficultyLevel": 1,
          "reason": "test",
          "focusType": "WEAKNESS"
        }
        """);

        var result = await service.GenerateModuleChallengePlanAsync(CreatePlanRequest(new[] { "buku" }), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("weak words", result.ErrorMessage);
    }

    [Fact]
    public async Task GenerateModuleChallengePlanAsync_RejectsInvalidGameType()
    {
        var service = CreateService("""
        {
          "selectedWords": ["buku"],
          "recommendedGameType": "WORD_TWINS",
          "difficultyLevel": 1,
          "reason": "test",
          "focusType": "PRACTICE"
        }
        """);

        var result = await service.GenerateModuleChallengePlanAsync(CreatePlanRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("invalid game type", result.ErrorMessage);
    }

    [Theory]
    [InlineData("SPELL_CATCHER")]
    [InlineData("SYLLABLE_SUSHI")]
    [InlineData("VOICE_BRIDGE")]
    public async Task GenerateGameConfigAsync_AcceptsValidAdaptiveGameEnvelope(string gameType)
    {
        var audit = Substitute.For<IAiAuditService>();
        var service = CreateService($$"""
        {
          "gameType": "{{gameType}}",
          "config": {}
        }
        """, audit);

        var result = await service.GenerateGameConfigAsync(
            new GameConfigGenerationRequest(
                10,
                "Unit 1",
                "Bahasa Melayu",
                99,
                "MODULE_PRACTICE",
                "PREDEFINED_MODULE",
                gameType,
                2,
                new[] { CreateAdaptiveItem("buku") }),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(gameType, result.Result.GameTemplateCode);
        Assert.Equal("PREDEFINED_MODULE", result.Result.SourceType);
        await audit.Received(1).StartAsync(
            Arg.Is<AiAuditStartRequest>(request =>
                request.Provider == "RULE_BASED"
                && request.ModelName == null
                && request.RelatedClassroomId == 99
                && request.RelatedModuleId == 10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void AiPromptRegistry_ReturnsStickerGenerationPrompt()
    {
        var prompt = new AiPromptRegistry().Get(AiUseCases.StickerGeneration);

        Assert.Equal(AiUseCases.StickerGeneration, prompt.UseCase);
        Assert.Equal("v1", prompt.Version);
        Assert.Equal("sticker_generation_request", prompt.OutputSchemaName);
    }

    private static ChallengeAiPipelineService CreateService(string rawResponse, IAiAuditService? audit = null)
    {
        var ai = Substitute.For<IAiGenerationService>();
        ai.GenerateJsonAsync(Arg.Any<ChallengeGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(OperationResult<ChallengeGenerationResult>.SuccessResult(new ChallengeGenerationResult(rawResponse)));

        audit ??= Substitute.For<IAiAuditService>();
        audit.StartAsync(Arg.Any<AiAuditStartRequest>(), Arg.Any<CancellationToken>())
            .Returns(42);

        return new ChallengeAiPipelineService(
            ai,
            new AiPromptRegistry(),
            audit,
            Options.Create(new GoogleAiOptions { ModelId = "gemini-test" }),
            Substitute.For<ILogger<ChallengeAiPipelineService>>());
    }

    private static ModuleChallengePlanRequest CreatePlanRequest(IReadOnlyList<string>? weakWords = null) =>
        new(
            10,
            "Unit 1",
            "Bahasa Melayu",
            1,
            "SPELL_CATCHER",
            "MODULE_PRACTICE",
            new[]
            {
                new ModuleChallengeAiItem(
                    1,
                    "buku",
                    "buku",
                    "book",
                    "shu",
                    "[\"bu\",\"ku\"]",
                    "bu/ku",
                    "WORD",
                    1,
                    "book",
                    "Saya baca buku."),
                new ModuleChallengeAiItem(
                    2,
                    "meja",
                    "meja",
                    "table",
                    "zhuo",
                    "[\"me\",\"ja\"]",
                    "me/ja",
                    "WORD",
                    1,
                    "table",
                    "Meja itu besar.")
            },
            weakWords ?? Array.Empty<string>(),
            null,
            7,
            9);

    private static AdaptiveChallengeItemDto CreateAdaptiveItem(string word) =>
        new(
            null,
            1,
            word,
            word,
            "book",
            "book",
            "Saya baca buku.",
            "[\"bu\",\"ku\"]",
            2,
            word,
            "shu",
            "book",
            "bu/ku",
            "WORD",
            1,
            null,
            null,
            null,
            null);
}
