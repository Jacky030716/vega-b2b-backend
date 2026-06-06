using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Queries;

internal class GetTeacherClassroomsQueryHandler : IRequestHandler<GetTeacherClassroomsQuery, OperationResult<List<ClassroomDto>>>
{
  private readonly IUnitOfWork _unitOfWork;

  public GetTeacherClassroomsQueryHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<List<ClassroomDto>>> Handle(GetTeacherClassroomsQuery request, CancellationToken cancellationToken)
  {
    var classrooms = await _unitOfWork.ClassroomRepository.GetTeacherClassroomsAsync(request.TeacherId, request.IncludeDeleted);

    if (classrooms.Count == 0)
      return OperationResult<List<ClassroomDto>>.SuccessResult([]);

    // Fetch all counts in 3 batch queries instead of 3 queries × N classrooms.
    var classroomIds = classrooms.Select(c => c.Id).ToList();
    var studentCounts = await _unitOfWork.ClassroomRepository.GetStudentCountsAsync(classroomIds);
    var moduleCounts = await _unitOfWork.ClassroomRepository.GetModuleCountsAsync(classroomIds);
    var challengeCounts = await _unitOfWork.ClassroomRepository.GetChallengeCountsAsync(classroomIds);

    var result = classrooms.Select(c =>
    {
      var subjects = c.Subjects.Count > 0
          ? c.Subjects.Select(s => s.Subject).ToList()
          : string.IsNullOrWhiteSpace(c.Subject) ? new List<string>() : new List<string> { c.Subject };

      return new ClassroomDto(
          c.Id,
          c.Name,
          c.Description,
          c.Subject,
          c.YearLevel,
          c.Thumbnail,
          c.JoinCode,
          c.TeacherId,
          c.Teacher?.Name ?? c.Teacher?.UserName ?? "Teacher",
          studentCounts.GetValueOrDefault(c.Id, 0),
          challengeCounts.GetValueOrDefault(c.Id, 0),
          moduleCounts.GetValueOrDefault(c.Id, 0),
          subjects);
    }).ToList();

    return OperationResult<List<ClassroomDto>>.SuccessResult(result);
  }

}
