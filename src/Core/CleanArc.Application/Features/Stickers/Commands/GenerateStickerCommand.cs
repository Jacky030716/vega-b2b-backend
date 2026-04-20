using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Stickers.Commands;

public record GenerateStickerCommand(int UserId, string Subject, string Style, string Mood)
  : IRequest<OperationResult<GeneratedStickerDto>>;

public record GeneratedStickerDto(
  int StickerId,
  string ImageUrl,
  string ModelName,
  int RemainingDreamTokens,
  DateTime LastStickerGeneratedAtUtc);
