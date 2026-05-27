using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Stickers.Queries;

internal class GetUnclaimedStickerGiftsQueryHandler : IRequestHandler<GetUnclaimedStickerGiftsQuery, OperationResult<List<UnclaimedStickerGiftDto>>>
{
  private readonly IUnitOfWork _unitOfWork;

  public GetUnclaimedStickerGiftsQueryHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<List<UnclaimedStickerGiftDto>>> Handle(
    GetUnclaimedStickerGiftsQuery request,
    CancellationToken cancellationToken)
  {
    var gifts = await _unitOfWork.StickerRepository.GetUnclaimedGiftsByRecipientAsync(request.UserId, cancellationToken);

    var result = gifts
      .Select(g => {
        var senderName = g.SenderUser == null 
          ? "Friend" 
          : (string.IsNullOrEmpty(g.SenderUser.FamilyName) 
            ? g.SenderUser.Name 
            : $"{g.SenderUser.Name} {g.SenderUser.FamilyName}".Trim());

        return new UnclaimedStickerGiftDto(
          g.Id,
          g.SenderUserId,
          senderName,
          g.SourceStickerId,
          g.RecipientStickerId,
          g.SourceSticker.ImageUrl,
          g.CreatedTime);
      })
      .ToList();

    return OperationResult<List<UnclaimedStickerGiftDto>>.SuccessResult(result);
  }
}
