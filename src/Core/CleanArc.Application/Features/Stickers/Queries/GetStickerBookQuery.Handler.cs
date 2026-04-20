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

    var mapped = stickers
      .Select(MapToDto)
      .ToList();

    var myCreations = mapped
      .Where(s => string.Equals(s.OwnershipSource, StickerOwnershipSource.Generated.ToString(), StringComparison.OrdinalIgnoreCase))
      .ToList();

    var giftedByFriends = mapped
      .Where(s => string.Equals(s.OwnershipSource, StickerOwnershipSource.GiftClone.ToString(), StringComparison.OrdinalIgnoreCase))
      .ToList();

    return OperationResult<StickerBookResult>.SuccessResult(new StickerBookResult(myCreations, giftedByFriends));
  }

  private static StickerBookItemDto MapToDto(StickerInventoryItem sticker)
  {
    return new StickerBookItemDto(
      sticker.Id,
      sticker.ImageUrl,
      sticker.OwnershipSource.ToString(),
      sticker.SourceStickerId,
      sticker.GenerationModel,
      sticker.CreatedTime);
  }
}
