using CleanArc.Application.Contracts.Infrastructure.Stickers;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Stickers.Queries;

public record GetStickerPromptOptionsQuery() : IRequest<OperationResult<IReadOnlyList<StickerPromptOptionGroup>>>;
