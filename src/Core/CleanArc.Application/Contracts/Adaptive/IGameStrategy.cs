using CleanArc.Application.Contracts.Adaptive;

namespace CleanArc.Application.Contracts.Adaptive;

public interface IGameStrategy
{
    string GameKey { get; }
    string GameTemplateCode { get; }
    string Category { get; }

    object GeneratePlayableContent(
        IReadOnlyList<AdaptiveChallengeItemDto> items,
        int difficultyLevel,
        string? configJson);
}
