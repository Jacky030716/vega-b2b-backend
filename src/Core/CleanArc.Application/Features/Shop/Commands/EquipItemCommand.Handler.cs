using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Contracts.Achievements;
using CleanArc.Application.Models.Common;
using Mediator;
using System.Text.Json;

namespace CleanArc.Application.Features.Shop.Commands;

internal class EquipItemCommandHandler : IRequestHandler<EquipItemCommand, OperationResult<bool>>
{
  private readonly IUnitOfWork _unitOfWork;
  private readonly IAchievementTrackingService _achievementTrackingService;

  public EquipItemCommandHandler(
    IUnitOfWork unitOfWork,
    IAchievementTrackingService achievementTrackingService)
  {
    _unitOfWork = unitOfWork;
    _achievementTrackingService = achievementTrackingService;
  }

  public async ValueTask<OperationResult<bool>> Handle(EquipItemCommand request, CancellationToken cancellationToken)
  {
    await _unitOfWork.ShopRepository.EquipItemAsync(request.UserId, request.Category, request.ShopItemId);

    if (string.Equals(request.Category, "avatar", StringComparison.OrdinalIgnoreCase))
    {
      await _achievementTrackingService.TrackEventAsync(
        request.UserId,
        "MASCOT_EQUIPPED",
        $"mascot-equipped:{request.UserId}:{request.ShopItemId}:{DateTime.UtcNow:yyyyMMddHHmmssfff}",
        JsonSerializer.Serialize(new
        {
          shopItemId = request.ShopItemId,
          category = request.Category,
        }),
        cancellationToken);
    }

    return OperationResult<bool>.SuccessResult(true);
  }
}
