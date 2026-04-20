using CleanArc.Application.Models.Common;

namespace CleanArc.Application.Contracts.Infrastructure.Stickers;

public record StickerPromptOption(string Value, string Label);

public record StickerPromptOptionGroup(string Key, string Label, IReadOnlyList<StickerPromptOption> Options);

public interface IStickerPromptCatalogService
{
  Task<OperationResult<IReadOnlyList<StickerPromptOptionGroup>>> GetPromptOptionGroupsAsync(CancellationToken cancellationToken);
}
