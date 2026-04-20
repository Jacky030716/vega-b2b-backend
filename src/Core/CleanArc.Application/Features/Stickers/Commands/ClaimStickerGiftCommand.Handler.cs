using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Sticker;
using Mediator;

namespace CleanArc.Application.Features.Stickers.Commands;

internal class ClaimStickerGiftCommandHandler : IRequestHandler<ClaimStickerGiftCommand, OperationResult<ClaimedStickerGiftDto>>
{
  private readonly IUnitOfWork _unitOfWork;

  public ClaimStickerGiftCommandHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<ClaimedStickerGiftDto>> Handle(ClaimStickerGiftCommand request, CancellationToken cancellationToken)
  {
    var gift = await _unitOfWork.StickerRepository.GetGiftByIdAsync(request.GiftId, cancellationToken);
    if (gift is null)
      return OperationResult<ClaimedStickerGiftDto>.FailureResult("Gift not found.");

    if (gift.RecipientUserId != request.UserId)
      return OperationResult<ClaimedStickerGiftDto>.UnauthorizedResult("You are not allowed to claim this gift.");

    if (gift.Status != StickerGiftStatus.PendingClaim)
      return OperationResult<ClaimedStickerGiftDto>.FailureResult("Gift has already been claimed or is no longer available.");

    gift.Status = StickerGiftStatus.Claimed;
    gift.ClaimedAtUtc = DateTime.UtcNow;

    await _unitOfWork.CommitAsync();

    return OperationResult<ClaimedStickerGiftDto>.SuccessResult(new ClaimedStickerGiftDto(
      gift.Id,
      gift.RecipientStickerId,
      gift.RecipientSticker.ImageUrl,
      gift.ClaimedAtUtc.Value));
  }
}
