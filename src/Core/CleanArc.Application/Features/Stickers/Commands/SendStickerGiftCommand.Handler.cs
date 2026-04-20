using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Sticker;
using Mediator;

namespace CleanArc.Application.Features.Stickers.Commands;

internal class SendStickerGiftCommandHandler : IRequestHandler<SendStickerGiftCommand, OperationResult<StickerGiftSentDto>>
{
  private const int GiftDiamondCost = 5;

  private readonly IUnitOfWork _unitOfWork;

  public SendStickerGiftCommandHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<StickerGiftSentDto>> Handle(SendStickerGiftCommand request, CancellationToken cancellationToken)
  {
    if (request.SenderUserId == request.RecipientUserId)
      return OperationResult<StickerGiftSentDto>.FailureResult("You cannot gift a sticker to yourself.");

    var sender = await _unitOfWork.StickerRepository.GetUserByIdAsync(request.SenderUserId, cancellationToken);
    if (sender is null)
      return OperationResult<StickerGiftSentDto>.FailureResult("Sender not found.");

    var recipient = await _unitOfWork.StickerRepository.GetUserByIdAsync(request.RecipientUserId, cancellationToken);
    if (recipient is null)
      return OperationResult<StickerGiftSentDto>.FailureResult("Recipient not found.");

    if (sender.Diamonds < GiftDiamondCost)
      return OperationResult<StickerGiftSentDto>.FailureResult("Not enough diamonds to send this gift.");

    var sourceSticker = await _unitOfWork.StickerRepository.GetStickerForOwnerAsync(request.StickerId, request.SenderUserId, cancellationToken);
    if (sourceSticker is null)
      return OperationResult<StickerGiftSentDto>.FailureResult("Sticker not found in sender inventory.");

    await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
    try
    {
      var recipientSticker = new StickerInventoryItem
      {
        OwnerUserId = request.RecipientUserId,
        CreatorUserId = sourceSticker.CreatorUserId,
        SourceStickerId = sourceSticker.Id,
        ImageUrl = sourceSticker.ImageUrl,
        OwnershipSource = StickerOwnershipSource.GiftClone,
        PromptChoicesJson = sourceSticker.PromptChoicesJson,
        GenerationModel = sourceSticker.GenerationModel,
        GeneratedAtUtc = sourceSticker.GeneratedAtUtc,
      };

      await _unitOfWork.StickerRepository.AddStickerAsync(recipientSticker, cancellationToken);
      await _unitOfWork.CommitAsync();

      var gift = new StickerGiftTransaction
      {
        SenderUserId = request.SenderUserId,
        RecipientUserId = request.RecipientUserId,
        SourceStickerId = sourceSticker.Id,
        RecipientStickerId = recipientSticker.Id,
        DiamondCost = GiftDiamondCost,
        Status = StickerGiftStatus.PendingClaim,
      };

      await _unitOfWork.StickerRepository.AddGiftAsync(gift, cancellationToken);
      sender.Diamonds -= GiftDiamondCost;

      await _unitOfWork.CommitAsync();
      await transaction.CommitAsync(cancellationToken);

      return OperationResult<StickerGiftSentDto>.SuccessResult(new StickerGiftSentDto(
        gift.Id,
        sourceSticker.Id,
        recipientSticker.Id,
        sender.Diamonds));
    }
    catch (Exception ex)
    {
      await transaction.RollbackAsync(cancellationToken);
      return OperationResult<StickerGiftSentDto>.FailureResult($"Failed to send sticker gift: {ex.Message}");
    }
  }
}
