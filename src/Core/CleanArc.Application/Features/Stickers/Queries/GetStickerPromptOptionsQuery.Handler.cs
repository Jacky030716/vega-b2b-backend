using CleanArc.Application.Contracts.Infrastructure.Stickers;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Stickers.Queries;

internal class GetStickerPromptOptionsQueryHandler : IRequestHandler<GetStickerPromptOptionsQuery, OperationResult<IReadOnlyList<StickerPromptOptionGroup>>>
{
  private readonly IStickerPromptCatalogService _promptCatalogService;

  public GetStickerPromptOptionsQueryHandler(IStickerPromptCatalogService promptCatalogService)
  {
    _promptCatalogService = promptCatalogService;
  }

  public async ValueTask<OperationResult<IReadOnlyList<StickerPromptOptionGroup>>> Handle(
    GetStickerPromptOptionsQuery request,
    CancellationToken cancellationToken)
  {
    return await _promptCatalogService.GetPromptOptionGroupsAsync(cancellationToken);
  }
}
