using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Stickers.Queries;

public record GetUnclaimedStickerGiftsQuery(int UserId) : IRequest<OperationResult<List<UnclaimedStickerGiftDto>>>;

public record UnclaimedStickerGiftDto(
  int GiftId,
  int SenderUserId,
  int SourceStickerId,
  int RecipientStickerId,
  string StickerImageUrl,
  DateTime SentAtUtc);
