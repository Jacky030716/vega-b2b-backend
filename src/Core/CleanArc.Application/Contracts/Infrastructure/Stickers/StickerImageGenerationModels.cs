using CleanArc.Application.Models.Common;

namespace CleanArc.Application.Contracts.Infrastructure.Stickers;

public record StickerGenerationRequest(string Subject, string Style, string Mood);

public record StickerGenerationResult(byte[] ImageBytes, string ModelName);

public interface IStickerImageGenerationService
{
  Task<OperationResult<StickerGenerationResult>> GenerateAsync(StickerGenerationRequest request, CancellationToken cancellationToken);
}
