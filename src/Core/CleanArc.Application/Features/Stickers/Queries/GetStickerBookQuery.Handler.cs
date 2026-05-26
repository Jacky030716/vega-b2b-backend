using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Sticker;
using Mediator;

namespace CleanArc.Application.Features.Stickers.Queries;

internal class GetStickerBookQueryHandler : IRequestHandler<GetStickerBookQuery, OperationResult<StickerBookResult>>
{
  private readonly IUnitOfWork _unitOfWork;

  public GetStickerBookQueryHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<StickerBookResult>> Handle(GetStickerBookQuery request, CancellationToken cancellationToken)
  {
    var stickers = await _unitOfWork.StickerRepository.GetInventoryByOwnerAsync(request.UserId, cancellationToken);
    var giftTransactions = await _unitOfWork.StickerRepository.GetGiftTransactionsByRecipientAsync(request.UserId, cancellationToken);

    var pendingStickerIds = giftTransactions
      .Where(g => g.Status == StickerGiftStatus.PendingClaim)
      .Select(g => g.RecipientStickerId)
      .ToHashSet();

    var claimedGiftsLookup = giftTransactions
      .Where(g => g.Status == StickerGiftStatus.Claimed)
      .ToDictionary(g => g.RecipientStickerId, g => g.SenderUser);

    var mapped = stickers
      .Where(s => !pendingStickerIds.Contains(s.Id))
      .Select(s => {
        string? gifterName = null;
        if (claimedGiftsLookup.TryGetValue(s.Id, out var senderUser) && senderUser != null)
        {
          gifterName = string.IsNullOrEmpty(senderUser.FamilyName) 
            ? senderUser.Name 
            : $"{senderUser.Name} {senderUser.FamilyName}".Trim();
        }
        return new StickerBookItemDto(
          s.Id,
          s.ImageUrl,
          s.OwnershipSource.ToString(),
          s.SourceStickerId,
          s.GenerationModel,
          s.CreatedTime,
          gifterName);
      })
      .ToList();

    var myCreations = mapped
      .Where(s => string.Equals(s.OwnershipSource, StickerOwnershipSource.Generated.ToString(), StringComparison.OrdinalIgnoreCase))
      .ToList();

    var giftedByFriends = mapped
      .Where(s => string.Equals(s.OwnershipSource, StickerOwnershipSource.GiftClone.ToString(), StringComparison.OrdinalIgnoreCase))
      .ToList();

    return OperationResult<StickerBookResult>.SuccessResult(new StickerBookResult(myCreations, giftedByFriends));
  }
}
