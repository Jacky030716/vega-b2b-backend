using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Quiz;
using Mediator;

namespace CleanArc.Application.Features.Games.Commands;

internal sealed class CreateChallengeCommandHandler(IUnitOfWork unitOfWork, IAiAuditService aiAuditService)
    : IRequestHandler<CreateChallengeCommand, OperationResult<CreateChallengeDto>>
{
  public async ValueTask<OperationResult<CreateChallengeDto>> Handle(
      CreateChallengeCommand request,
      CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(request.GameKey))
      return OperationResult<CreateChallengeDto>.FailureResult("Game key is required");

    if (string.IsNullOrWhiteSpace(request.Title))
      return OperationResult<CreateChallengeDto>.FailureResult("Title is required");

    if (request.DifficultyLevel < 1 || request.DifficultyLevel > 5)
      return OperationResult<CreateChallengeDto>.FailureResult("DifficultyLevel must be between 1 and 5");

    var game = await unitOfWork.ChallengeRepository.GetGameByKeyAsync(request.GameKey.Trim());
    if (game is null)
      return OperationResult<CreateChallengeDto>.NotFoundResult($"Game '{request.GameKey}' not found");

    var normalizedContentResult = ChallengeContentNormalizer.NormalizeAndValidate(game.Key, request.ContentData);
    if (!normalizedContentResult.IsSuccess)
      return OperationResult<CreateChallengeDto>.FailureResult(normalizedContentResult.ErrorMessage!);

    var nextOrderIndex = await unitOfWork.ChallengeRepository.GetNextOrderIndexForGameAsync(game.Id);

    var challenge = await unitOfWork.ChallengeRepository.CreateChallengeAsync(new Challenge
    {
      GameId = game.Id,
      Title = request.Title.Trim(),
      Description = request.Description?.Trim() ?? string.Empty,
      DifficultyLevel = request.DifficultyLevel,
      ContentData = normalizedContentResult.Result!,
      OrderIndex = nextOrderIndex,
      MaxStars = 3,
      CreatedById = request.UserId,
      IsAIGenerated = request.IsAIGenerated,
      AiGenerationStatus = request.IsAIGenerated ? AiGenerationStatuses.AiGenerated : AiGenerationStatuses.None,
      AiUseCase = request.IsAIGenerated ? AiUseCases.CustomChallengeExtraction : null,
      AiAuditLogId = request.IsAIGenerated ? request.AiAuditLogId : null,
      ClassroomId = request.ClassroomId
    });

    if (request.IsAIGenerated && request.AiAuditLogId is int auditLogId)
      await aiAuditService.AttachChallengeAsync(auditLogId, challenge.Id, cancellationToken);

    return OperationResult<CreateChallengeDto>.SuccessResult(new CreateChallengeDto(
      challenge.Id,
      game.Id,
      game.Key,
      challenge.Title,
      challenge.DifficultyLevel,
      challenge.OrderIndex,
      challenge.IsAIGenerated,
      challenge.CreatedById,
      challenge.ClassroomId
    ));
  }
}
