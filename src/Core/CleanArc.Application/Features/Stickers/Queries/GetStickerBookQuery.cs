#nullable enable

using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Stickers.Queries;

public record GetStickerBookQuery(int UserId) : IRequest<OperationResult<StickerBookResult>>;

public record StickerBookResult(List<StickerBookItemDto> MyCreations, List<StickerBookItemDto> GiftedByFriends);

public record StickerBookItemDto(
  int StickerId,
  string ImageUrl,
  string OwnershipSource,
  int? SourceStickerId,
  string? GenerationModel,
  DateTime CreatedAtUtc);
