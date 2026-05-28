using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Classroom;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Commands;

internal class CreateClassroomCommandHandler : IRequestHandler<CreateClassroomCommand, OperationResult<int>>
{
  private readonly IUnitOfWork _unitOfWork;

  public CreateClassroomCommandHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<int>> Handle(CreateClassroomCommand request, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(request.Name))
    {
      return OperationResult<int>.FailureResult("Classroom name is required");
    }

    var membership = await _unitOfWork.InstitutionRepository.GetPrimaryInstitutionForUserAsync(request.TeacherId, cancellationToken);
    if (membership is null)
    {
      return OperationResult<int>.FailureResult("Teacher does not belong to any active institution.");
    }

    var institution = membership.Institution;
    if (institution is null)
    {
      institution = await _unitOfWork.InstitutionRepository.GetInstitutionWithStatsAsync(membership.InstitutionId);
    }

    if (institution is not null && !string.Equals(institution.SubscriptionTier, "Premium", StringComparison.OrdinalIgnoreCase))
    {
      var currentClassroomsCount = await _unitOfWork.ClassroomRepository.GetInstitutionClassroomsCountAsync(institution.Id, cancellationToken);
      if (currentClassroomsCount >= 10)
      {
        return OperationResult<int>.FailureResult("Your institution has reached the maximum limit of 10 classrooms allowed on the Standard plan. Please upgrade your subscription to create more.");
      }
    }

    if (request.YearLevel is < 1 or > 6)
    {
      return OperationResult<int>.FailureResult("Year level must be between 1 and 6");
    }

    var subjects = NormalizeSubjects(request.Subjects);
    if (subjects.Count == 0 && !string.IsNullOrWhiteSpace(request.Subject))
    {
      subjects.Add(request.Subject.Trim());
    }

    if (subjects.Count == 0)
    {
      return OperationResult<int>.FailureResult("At least one subject is required");
    }

    var joinCode = GenerateJoinCode();

    var classroom = new Classroom
    {
      Name = request.Name.Trim(),
      Description = request.Description,
      Subject = subjects[0],
      YearLevel = request.YearLevel,
      Thumbnail = ResolveThumbnailUrl(request.ThumbnailInfo, request.Thumbnail),
      ThumbnailType = ResolveThumbnailType(request.ThumbnailInfo, request.Thumbnail),
      ThumbnailUrl = ResolveThumbnailUrl(request.ThumbnailInfo, request.Thumbnail),
      ThumbnailAssetId = request.ThumbnailInfo?.AssetId,
      ThumbnailPrompt = request.ThumbnailInfo?.Prompt,
      ThumbnailGeneratedAt = string.Equals(request.ThumbnailInfo?.Type, "AI_GENERATED", StringComparison.OrdinalIgnoreCase)
        ? DateTime.UtcNow
        : null,
      JoinCode = joinCode,
      TeacherId = request.TeacherId,
      IsActive = true
    };

    var created = await _unitOfWork.ClassroomRepository.CreateClassroomAsync(classroom);
    await _unitOfWork.ClassroomRepository.ProvisionClassroomModulesAsync(created.Id, subjects, request.TeacherId);
    return OperationResult<int>.SuccessResult(created.Id);
  }

  private static string GenerateJoinCode()
  {
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    var random = new Random();
    return new string(Enumerable.Range(0, 4).Select(_ => chars[random.Next(chars.Length)]).ToArray());
  }

  private static List<string> NormalizeSubjects(IEnumerable<string>? subjects)
  {
    return (subjects ?? Array.Empty<string>())
        .Where(subject => !string.IsNullOrWhiteSpace(subject))
        .Select(subject => subject.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();
  }

  private static string ResolveThumbnailType(ClassroomThumbnailRequest? thumbnailInfo, string? thumbnail)
  {
    if (thumbnailInfo is not null)
    {
      return thumbnailInfo.Type?.Trim().ToUpperInvariant() switch
      {
        "UPLOADED" => "UPLOADED",
        "AI_GENERATED" => "AI_GENERATED",
        _ => "DEFAULT"
      };
    }

    return string.IsNullOrWhiteSpace(thumbnail) ? "DEFAULT" : "UPLOADED";
  }

  private static string ResolveThumbnailUrl(ClassroomThumbnailRequest? thumbnailInfo, string? thumbnail)
  {
    if (thumbnailInfo?.Url is { Length: > 0 } url)
      return url;

    return thumbnail ?? string.Empty;
  }
}
