using System.Text.Json;
using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Application.Contracts.Infrastructure.Stickers;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Sticker;
using Mediator;
using Microsoft.Extensions.Logging;

namespace CleanArc.Application.Features.Stickers.Commands;

internal class GenerateStickerCommandHandler : IRequestHandler<GenerateStickerCommand, OperationResult<GeneratedStickerDto>>
{
  private const int DailyStudentGenerationLimit = 3;

  private readonly IUnitOfWork _unitOfWork;
  private readonly IStickerImageGenerationService _imageGenerationService;
  private readonly IStickerImageStorageService _imageStorageService;
  private readonly IAiAuditService _aiAuditService;
  private readonly IAiPromptRegistry _promptRegistry;
  private readonly IAiUsageService _aiUsageService;
  private readonly IAiRateLimitService _aiRateLimitService;
  private readonly ILogger<GenerateStickerCommandHandler> _logger;

  public GenerateStickerCommandHandler(
    IUnitOfWork unitOfWork,
    IStickerImageGenerationService imageGenerationService,
    IStickerImageStorageService imageStorageService,
    IAiAuditService aiAuditService,
    IAiPromptRegistry promptRegistry,
    IAiUsageService aiUsageService,
    IAiRateLimitService aiRateLimitService,
    ILogger<GenerateStickerCommandHandler> logger)
  {
    _unitOfWork = unitOfWork;
    _imageGenerationService = imageGenerationService;
    _imageStorageService = imageStorageService;
    _aiAuditService = aiAuditService;
    _promptRegistry = promptRegistry;
    _aiUsageService = aiUsageService;
    _aiRateLimitService = aiRateLimitService;
    _logger = logger;
  }

  public async ValueTask<OperationResult<GeneratedStickerDto>> Handle(GenerateStickerCommand request, CancellationToken cancellationToken)
  {
    var user = await _unitOfWork.StickerRepository.GetUserByIdAsync(request.UserId, cancellationToken);
    if (user is null)
    {
      _logger.LogWarning("Sticker generation rejected for missing user {UserId}.", request.UserId);
      return OperationResult<GeneratedStickerDto>.FailureResult("User not found.");
    }

    if (user.DreamTokensCount <= 0)
    {
      _logger.LogInformation("Sticker generation rejected for user {UserId}: no Dream Tokens.", request.UserId);
      return OperationResult<GeneratedStickerDto>.FailureResult("You need at least 1 Dream Token to generate a sticker.");
    }

    var rateLimit = await _aiRateLimitService.TryAcquireAsync(request.UserId, AiFeatureTypes.StickerGeneration, cancellationToken);
    if (!rateLimit.Allowed)
    {
      _logger.LogInformation("Sticker generation rejected for user {UserId}: AI rate limit.", request.UserId);
      return OperationResult<GeneratedStickerDto>.FailureResult("Too many AI requests. Please try again later.");
    }

    var quota = await _aiUsageService.GetRemainingQuotaAsync(request.UserId, AiFeatureTypes.StickerGeneration, cancellationToken);
    if (quota.Remaining <= 0)
    {
      _logger.LogInformation("Sticker generation rejected for user {UserId}: monthly AI quota exhausted.", request.UserId);
      return OperationResult<GeneratedStickerDto>.FailureResult("Your AI quota is exhausted for this month.");
    }

    var now = DateTime.UtcNow;
    var todayStartUtc = now.Date;
    var tomorrowStartUtc = todayStartUtc.AddDays(1);
    var generatedToday = await _unitOfWork.StickerRepository.CountGeneratedStickersAsync(
      request.UserId,
      todayStartUtc,
      tomorrowStartUtc,
      cancellationToken);

    if (generatedToday >= DailyStudentGenerationLimit)
    {
      _logger.LogInformation("Sticker generation rejected for user {UserId}: daily limit {Limit} reached.", request.UserId, DailyStudentGenerationLimit);
      return OperationResult<GeneratedStickerDto>.FailureResult("You have used all 3 Dream Lab generations for today. Come back tomorrow or save your Dream Tokens for later.");
    }

    var prompt = _promptRegistry.Get(AiUseCases.StickerGeneration);
    var auditLogId = await _aiAuditService.StartAsync(
      new AiAuditStartRequest(
        AiUseCases.StickerGeneration,
        "HUGGING_FACE",
        null,
        prompt.Version,
        JsonSerializer.Serialize(new
        {
          request.Subject,
          request.Style,
          request.Mood
        }),
        request.UserId),
      cancellationToken);

    var generation = await _imageGenerationService.GenerateAsync(
      new StickerGenerationRequest(request.Subject, request.Style, request.Mood),
      cancellationToken);

    if (!generation.IsSuccess)
    {
      _logger.LogWarning(
        "Sticker image generation failed for user {UserId}: {ErrorMessage}",
        request.UserId,
        generation.ErrorMessage ?? "Sticker generation failed.");

      await _aiAuditService.FailAsync(
        auditLogId,
        null,
        new[] { generation.ErrorMessage ?? "Sticker generation failed." },
        cancellationToken);
      return OperationResult<GeneratedStickerDto>.FailureResult(generation.ErrorMessage ?? "Sticker generation failed.");
    }

    var upload = await _imageStorageService.UploadAsync(
      generation.Result.ImageBytes,
      $"{request.Subject}-{request.Style}-{request.Mood}",
      cancellationToken);

    if (!upload.IsSuccess)
    {
      await _aiAuditService.FailAsync(
        auditLogId,
        JsonSerializer.Serialize(new
        {
          generation.Result.ModelName,
          imageBytes = generation.Result.ImageBytes.Length
        }),
        new[] { upload.ErrorMessage ?? "Sticker upload failed." },
        cancellationToken);
      return OperationResult<GeneratedStickerDto>.FailureResult(upload.ErrorMessage ?? "Sticker upload failed.");
    }

    await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
    try
    {
      var generatedAtUtc = DateTime.UtcNow;
      var sticker = new StickerInventoryItem
      {
        OwnerUserId = request.UserId,
        CreatorUserId = request.UserId,
        SourceStickerId = null,
        ImageUrl = upload.Result.ImageRef,
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

      await _aiAuditService.CompleteAsync(
        auditLogId,
        JsonSerializer.Serialize(new
        {
          generation.Result.ModelName,
          imageBytes = generation.Result.ImageBytes.Length
        }),
        JsonSerializer.Serialize(new
        {
          sticker.Id,
          sticker.ImageUrl,
          generation.Result.ModelName
        }),
        AiValidationStatuses.Valid,
        Array.Empty<string>(),
        cancellationToken);

      await _aiUsageService.ConsumeUsageAsync(
        request.UserId,
        AiFeatureTypes.StickerGeneration,
        "POST /api/v1.1/stickers/generate",
        "HUGGING_FACE",
        generation.Result.ModelName,
        1,
        true,
        null,
        "sticker",
        sticker.Id,
        cancellationToken);

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
      await _aiAuditService.FailAsync(auditLogId, null, new[] { ex.Message }, cancellationToken);
      return OperationResult<GeneratedStickerDto>.FailureResult($"Sticker generation failed: {ex.Message}");
    }
  }
}
