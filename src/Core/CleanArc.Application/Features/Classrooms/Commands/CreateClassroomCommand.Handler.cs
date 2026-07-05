using System.Collections.Generic;
using System.Security.Cryptography;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.User;
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

    var joinCode = GenerateJoinCode();

    var classroom = new Classroom
    {
      Name = request.Name.Trim(),
      Description = request.Description,
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

    if (request.StudentIds is { Count: > 0 })
    {
      var existingCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      foreach (var studentId in request.StudentIds)
      {
        await _unitOfWork.ClassroomRepository.JoinClassroomAsync(new ClassroomStudent
        {
          ClassroomId = created.Id,
          UserId = studentId,
          JoinedDate = DateTime.UtcNow
        });

        string loginCode;
        while (true)
        {
          var candidate = RandomNumberGenerator.GetInt32(1000, 10000).ToString();
          if (existingCodes.Contains(candidate))
            continue;

          var existing = await _unitOfWork.StudentCredentialRepository.GetByLoginCodeAsync(candidate);
          if (existing != null)
            continue;

          loginCode = candidate;
          existingCodes.Add(loginCode);
          break;
        }

        await _unitOfWork.StudentCredentialRepository.CreateAsync(new StudentCredential
        {
          UserId = studentId,
          ClassroomId = created.Id,
          StudentLoginCode = loginCode,
          VisualPasswordHash = "DEFAULT",
          IsActive = true,
          FailedAttempts = 0
        });
      }

      await _unitOfWork.CommitAsync();
    }

    return OperationResult<int>.SuccessResult(created.Id);
  }

  private static string GenerateJoinCode()
  {
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    var random = new Random();
    return new string(Enumerable.Range(0, 4).Select(_ => chars[random.Next(chars.Length)]).ToArray());
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
