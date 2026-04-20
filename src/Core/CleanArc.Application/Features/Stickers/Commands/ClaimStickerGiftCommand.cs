using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Stickers.Commands;

public record ClaimStickerGiftCommand(int UserId, int GiftId)
  : IRequest<OperationResult<ClaimedStickerGiftDto>>;

public record ClaimedStickerGiftDto(
  int GiftId,
  int StickerId,
  string StickerImageUrl,
  DateTime ClaimedAtUtc);
