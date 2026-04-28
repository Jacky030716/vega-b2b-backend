using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Application.Models.Common;
using CleanArc.Infrastructure.Persistence.Services.AI;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CleanArc.Tests.Setup.Features.Adaptive;

public class ChallengeAiPipelineServiceTests
{
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
        var service = CreateService($$"""
        {
          "gameType": "{{gameType}}",
          "config": {}
        }
        """);

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
    }

    private static ChallengeAiPipelineService CreateService(string rawResponse)
    {
        var ai = Substitute.For<IAiGenerationService>();
        ai.GenerateJsonAsync(Arg.Any<ChallengeGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(OperationResult<ChallengeGenerationResult>.SuccessResult(new ChallengeGenerationResult(rawResponse)));

        return new ChallengeAiPipelineService(ai, Substitute.For<ILogger<ChallengeAiPipelineService>>());
    }

    private static ModuleChallengePlanRequest CreatePlanRequest() =>
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
                    "Saya baca buku.")
            },
            Array.Empty<string>(),
            null);

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
