using CleanArc.Application.Models.Common;

namespace CleanArc.Application.Contracts.Infrastructure.Stickers;

public record StickerUploadResult(string ImageRef);

public interface IStickerImageStorageService
{
  Task<OperationResult<StickerUploadResult>> UploadAsync(byte[] imageBytes, string fileName, CancellationToken cancellationToken);
}
