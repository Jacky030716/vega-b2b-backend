using CleanArc.Application.Contracts.Infrastructure.Stickers;
using CleanArc.Application.Models.Common;

namespace CleanArc.Infrastructure.Persistence.Services.Stickers;

public class StickerPromptCatalogService : IStickerPromptCatalogService
{
  private static readonly IReadOnlyList<StickerPromptOptionGroup> Groups =
  [
    new StickerPromptOptionGroup(
      "subject",
      "Who",
      [
        new StickerPromptOption("fox", "Fox"),
        new StickerPromptOption("cat", "Cat"),
        new StickerPromptOption("panda", "Panda"),
        new StickerPromptOption("dragon", "Dragon"),
      ]),
    new StickerPromptOptionGroup(
      "style",
      "Style",
      [
        new StickerPromptOption("chibi", "Chibi"),
        new StickerPromptOption("pixel", "Pixel"),
        new StickerPromptOption("paper-cut", "Paper Cut"),
        new StickerPromptOption("cartoon", "Cartoon"),
      ]),
    new StickerPromptOptionGroup(
      "mood",
      "Mood",
      [
        new StickerPromptOption("happy", "Happy"),
        new StickerPromptOption("brave", "Brave"),
        new StickerPromptOption("sleepy", "Sleepy"),
        new StickerPromptOption("curious", "Curious"),
      ]),
  ];

  public Task<OperationResult<IReadOnlyList<StickerPromptOptionGroup>>> GetPromptOptionGroupsAsync(CancellationToken cancellationToken)
  {
    return Task.FromResult(OperationResult<IReadOnlyList<StickerPromptOptionGroup>>.SuccessResult(Groups));
  }
}
