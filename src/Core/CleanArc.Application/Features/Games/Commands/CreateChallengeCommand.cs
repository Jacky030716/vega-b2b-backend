using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Games.Commands;

/// <summary>
/// Response payload returned after creating a new challenge node.
/// </summary>
public record CreateChallengeDto(
    int ChallengeId,
    int GameId,
    string GameKey,
    string Title,
    int DifficultyLevel,
    int OrderIndex,
    bool IsAIGenerated,
    int? CreatedById
);

/// <summary>
/// Creates a new challenge for a game key with game-specific contentData.
/// </summary>
public record CreateChallengeCommand(
    int UserId,
    string GameKey,
    string Title,
    string Description,
    int DifficultyLevel,
    string ContentData,
    bool IsAIGenerated,
    string? CreationMode,
    string? SourcePrompt,
    string? SourceDocumentName
) : IRequest<OperationResult<CreateChallengeDto>>;
