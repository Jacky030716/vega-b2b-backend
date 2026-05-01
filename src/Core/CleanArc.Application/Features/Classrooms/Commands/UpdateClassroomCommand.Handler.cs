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

    var subjects = NormalizeSubjects(request.Subjects);
    if (subjects.Count == 0 && !string.IsNullOrWhiteSpace(request.Subject))
      subjects.Add(request.Subject.Trim());

    if (subjects.Count == 0)
      return OperationResult<UpdatedClassroomDto>.FailureResult("At least one subject is required");

    if (subjects.Any(subject => subject.Length > 100))
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
    classroom.Subject = subjects[0];
    classroom.Description = request.Description?.Trim() ?? string.Empty;
    classroom.YearLevel = requestedYearLevel;

    await unitOfWork.ClassroomRepository.UpdateClassroomAsync(classroom);
    await unitOfWork.ClassroomRepository.ReplaceClassroomSubjectsAndModulesAsync(classroom.Id, subjects, classroom.TeacherId);

    return OperationResult<UpdatedClassroomDto>.SuccessResult(new UpdatedClassroomDto(
        classroom.Id,
        classroom.Name,
        classroom.Subject,
        classroom.YearLevel,
        classroom.Description,
        classroom.ModifiedDate ?? DateTime.UtcNow,
        subjects));
  }

  private static List<string> NormalizeSubjects(IEnumerable<string>? subjects)
  {
    return (subjects ?? Array.Empty<string>())
        .Where(subject => !string.IsNullOrWhiteSpace(subject))
        .Select(subject => subject.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();
  }
}
