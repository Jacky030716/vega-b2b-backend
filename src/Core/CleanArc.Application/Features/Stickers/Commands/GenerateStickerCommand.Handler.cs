using System.Text.Json;
using CleanArc.Application.Contracts.Infrastructure.Stickers;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Sticker;
using Mediator;

namespace CleanArc.Application.Features.Stickers.Commands;

internal class GenerateStickerCommandHandler : IRequestHandler<GenerateStickerCommand, OperationResult<GeneratedStickerDto>>
{
  private readonly IUnitOfWork _unitOfWork;
  private readonly IStickerImageGenerationService _imageGenerationService;
  private readonly IStickerImageStorageService _imageStorageService;

  public GenerateStickerCommandHandler(
    IUnitOfWork unitOfWork,
    IStickerImageGenerationService imageGenerationService,
    IStickerImageStorageService imageStorageService)
  {
    _unitOfWork = unitOfWork;
    _imageGenerationService = imageGenerationService;
    _imageStorageService = imageStorageService;
  }

  public async ValueTask<OperationResult<GeneratedStickerDto>> Handle(GenerateStickerCommand request, CancellationToken cancellationToken)
  {
    var user = await _unitOfWork.StickerRepository.GetUserByIdAsync(request.UserId, cancellationToken);
    if (user is null)
      return OperationResult<GeneratedStickerDto>.FailureResult("User not found.");

    if (user.DreamTokensCount <= 0)
      return OperationResult<GeneratedStickerDto>.FailureResult("You need at least 1 Dream Token to generate a sticker.");

    var now = DateTime.UtcNow;
    if (user.LastStickerGeneratedAtUtc.HasValue)
    {
      var nextAllowed = user.LastStickerGeneratedAtUtc.Value.AddHours(24);
      if (nextAllowed > now)
      {
        var remaining = nextAllowed - now;
        return OperationResult<GeneratedStickerDto>.FailureResult($"Sticker generation is on cooldown. Try again in {remaining.Hours}h {remaining.Minutes}m.");
      }
    }

    var generation = await _imageGenerationService.GenerateAsync(
      new StickerGenerationRequest(request.Subject, request.Style, request.Mood),
      cancellationToken);

    if (!generation.IsSuccess)
      return OperationResult<GeneratedStickerDto>.FailureResult(generation.ErrorMessage ?? "Sticker generation failed.");

    var upload = await _imageStorageService.UploadPngAsync(
      generation.Result.ImageBytes,
      $"{request.Subject}-{request.Style}-{request.Mood}",
      cancellationToken);

    if (!upload.IsSuccess)
      return OperationResult<GeneratedStickerDto>.FailureResult(upload.ErrorMessage ?? "Sticker upload failed.");

    await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
    try
    {
      var generatedAtUtc = DateTime.UtcNow;
      var sticker = new StickerInventoryItem
      {
        OwnerUserId = request.UserId,
        CreatorUserId = request.UserId,
        SourceStickerId = null,
        ImageUrl = upload.Result.SecureUrl,
        OwnershipSource = StickerOwnershipSource.Generated,
        PromptChoicesJson = JsonSerializer.Serialize(new
        {
          request.Subject,
          request.Style,
          request.Mood,
        }),
        GenerationModel = generation.Result.ModelName,
        GeneratedAtUtc = generatedAtUtc,
      };

      await _unitOfWork.StickerRepository.AddStickerAsync(sticker, cancellationToken);

      user.DreamTokensCount -= 1;
      user.LastStickerGeneratedAtUtc = generatedAtUtc;

      await _unitOfWork.CommitAsync();
      await transaction.CommitAsync(cancellationToken);

      return OperationResult<GeneratedStickerDto>.SuccessResult(new GeneratedStickerDto(
        sticker.Id,
        sticker.ImageUrl,
        generation.Result.ModelName,
        user.DreamTokensCount,
        generatedAtUtc));
    }
    catch (Exception ex)
    {
      await transaction.RollbackAsync(cancellationToken);
      return OperationResult<GeneratedStickerDto>.FailureResult($"Sticker generation failed: {ex.Message}");
    }
  }
}
