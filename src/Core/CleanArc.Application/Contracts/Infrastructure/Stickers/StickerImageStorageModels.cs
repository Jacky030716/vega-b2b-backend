using CleanArc.Application.Models.Common;

namespace CleanArc.Application.Contracts.Infrastructure.Stickers;

public record StickerUploadResult(string PublicId, string SecureUrl);

public interface IStickerImageStorageService
{
  Task<OperationResult<StickerUploadResult>> UploadPngAsync(byte[] imageBytes, string fileName, CancellationToken cancellationToken);
}
