using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Stickers.Commands;

public record SendStickerGiftCommand(int SenderUserId, int RecipientUserId, int StickerId)
  : IRequest<OperationResult<StickerGiftSentDto>>;

public record StickerGiftSentDto(
  int GiftId,
  int SourceStickerId,
  int RecipientStickerId,
  int RemainingDiamonds);
