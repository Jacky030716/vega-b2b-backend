using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Commands;

internal class UpdateClassroomCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateClassroomCommand, OperationResult<UpdatedClassroomDto>>
{
  public async ValueTask<OperationResult<UpdatedClassroomDto>> Handle(UpdateClassroomCommand request, CancellationToken cancellationToken)
  {
    var classroom = await unitOfWork.ClassroomRepository.GetClassroomByIdAsync(request.ClassroomId, tracking: true);
    if (classroom is null)
      return OperationResult<UpdatedClassroomDto>.NotFoundResult("Classroom not found");

    if (!request.IsAdmin && classroom.TeacherId != request.RequestingUserId)
      return OperationResult<UpdatedClassroomDto>.ForbiddenResult("You do not manage this classroom");

    var name = request.Name?.Trim() ?? string.Empty;
    if (string.IsNullOrWhiteSpace(name))
      return OperationResult<UpdatedClassroomDto>.FailureResult("Classroom name is required");

    if (name.Length > 200)
      return OperationResult<UpdatedClassroomDto>.FailureResult("Classroom name must be 200 characters or fewer");

    classroom.Name = name;
    classroom.Description = request.Description?.Trim() ?? string.Empty;
    ApplyThumbnail(classroom, request.ThumbnailInfo);

    if (request.IsAdmin && request.TeacherId.HasValue)
    {
      classroom.TeacherId = request.TeacherId.Value;
    }

    await unitOfWork.ClassroomRepository.UpdateClassroomAsync(classroom);

    return OperationResult<UpdatedClassroomDto>.SuccessResult(new UpdatedClassroomDto(
        classroom.Id,
        classroom.Name,
        classroom.Description,
        classroom.ModifiedDate ?? DateTime.UtcNow));
  }

  private static void ApplyThumbnail(Domain.Entities.Classroom.Classroom classroom, ClassroomThumbnailRequest? thumbnailInfo)
  {
    if (thumbnailInfo is null)
      return;

    var type = thumbnailInfo.Type?.Trim().ToUpperInvariant();
    classroom.ThumbnailType = type switch
    {
      "UPLOADED" => "UPLOADED",
      "AI_GENERATED" => "AI_GENERATED",
      _ => "DEFAULT"
    };
    classroom.ThumbnailUrl = thumbnailInfo.Url?.Trim();
    classroom.ThumbnailAssetId = thumbnailInfo.AssetId?.Trim();
    classroom.ThumbnailPrompt = thumbnailInfo.Prompt?.Trim();
    classroom.ThumbnailGeneratedAt =
      string.Equals(classroom.ThumbnailType, "AI_GENERATED", StringComparison.OrdinalIgnoreCase)
        ? DateTime.UtcNow
        : null;
    classroom.Thumbnail = classroom.ThumbnailType == "DEFAULT"
      ? string.Empty
      : (classroom.ThumbnailUrl ?? string.Empty);
  }
}
