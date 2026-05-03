using CleanArc.Application.Models.Common;

namespace CleanArc.Application.Contracts.Infrastructure.ClassroomThumbnails;

public record ClassroomThumbnailGenerationRequest(
  int TeacherId,
  string ClassroomName,
  int YearLevel,
  IReadOnlyList<string> Subjects,
  string? Description,
  string? StylePreset);

public record ClassroomThumbnailGenerationResult(
  string AssetId,
  string Url,
  string PromptUsed,
  int RemainingQuota,
  string ModelName,
  string Provider);

public record ClassroomThumbnailUploadResult(string AssetId, string Url);

public interface IClassroomThumbnailImageGenerationService
{
  Task<OperationResult<(byte[] ImageBytes, string ModelName)>> GenerateAsync(
    ClassroomThumbnailGenerationRequest request,
    CancellationToken cancellationToken);
}

public interface IClassroomThumbnailImageStorageService
{
  Task<OperationResult<ClassroomThumbnailUploadResult>> UploadAsync(
    byte[] imageBytes,
    string fileName,
    string contentType,
    CancellationToken cancellationToken);
}
