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

    var subject = request.Subject?.Trim() ?? string.Empty;
    if (string.IsNullOrWhiteSpace(subject))
      return OperationResult<UpdatedClassroomDto>.FailureResult("Subject is required");

    if (subject.Length > 100)
      return OperationResult<UpdatedClassroomDto>.FailureResult("Subject must be 100 characters or fewer");

    var requestedYearLevel = request.YearLevel ?? classroom.YearLevel;
    if (requestedYearLevel is < 1 or > 6)
      return OperationResult<UpdatedClassroomDto>.FailureResult("Year level must be between 1 and 6");

    if (requestedYearLevel != classroom.YearLevel)
    {
      var hasModulesOrChallenges = await unitOfWork.ClassroomRepository.HasModulesOrChallengesAsync(classroom.Id);
      if (hasModulesOrChallenges)
        return OperationResult<UpdatedClassroomDto>.FailureResult("Year level cannot be changed after modules or challenges have been created.");
    }

    classroom.Name = name;
    classroom.Subject = subject;
    classroom.Description = request.Description?.Trim() ?? string.Empty;
    classroom.YearLevel = requestedYearLevel;

    await unitOfWork.ClassroomRepository.UpdateClassroomAsync(classroom);

    return OperationResult<UpdatedClassroomDto>.SuccessResult(new UpdatedClassroomDto(
        classroom.Id,
        classroom.Name,
        classroom.Subject,
        classroom.YearLevel,
        classroom.Description,
        classroom.ModifiedDate ?? DateTime.UtcNow));
  }
}
