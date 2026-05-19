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
    var assignedAt = request.ClassroomId.HasValue ? DateTime.UtcNow : (DateTime?)null;

    var moduleId = request.ModuleId;
    if (request.ClassroomId.HasValue && !moduleId.HasValue)
    {
      moduleId = await unitOfWork.ClassroomRepository.ResolveChallengeModuleIdAsync(request.ClassroomId.Value);
      if (!moduleId.HasValue)
        return OperationResult<CreateChallengeDto>.FailureResult("Select a classroom module before creating this challenge");
    }

    if (moduleId.HasValue)
    {
      if (!request.ClassroomId.HasValue)
        return OperationResult<CreateChallengeDto>.FailureResult("ClassroomId is required when assigning a challenge to a module");

      var isAttached = await unitOfWork.ClassroomRepository.IsModuleAttachedToClassroomAsync(
        request.ClassroomId.Value,
        moduleId.Value);
      if (!isAttached)
        return OperationResult<CreateChallengeDto>.FailureResult("Module is not attached to this classroom");

      var existingCount = await unitOfWork.ChallengeRepository.CountActiveModuleChallengesAsync(
        request.ClassroomId.Value,
        moduleId.Value);
      if (existingCount >= 3)
        return OperationResult<CreateChallengeDto>.FailureResult("Each module can have up to 3 game challenges");
    }

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
      ClassroomId = request.ClassroomId,
      ModuleId = moduleId,
      Status = request.ClassroomId.HasValue ? "assigned" : "draft",
      AssignedAt = assignedAt,
      LifecycleState = request.ClassroomId.HasValue
        ? ChallengeLifecycleState.Active
        : ChallengeLifecycleState.Draft,
      LastActivityAt = assignedAt
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
      challenge.ClassroomId,
      challenge.ModuleId
    ));
  }
}
