using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Quiz;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Queries;

internal class GetStudentClassroomsQueryHandler : IRequestHandler<GetStudentClassroomsQuery, OperationResult<List<ClassroomDto>>>
{
  private static readonly HashSet<ChallengeLifecycleState> StudentVisibleStates = new()
  {
    ChallengeLifecycleState.Active,
    ChallengeLifecycleState.Scheduled,
    ChallengeLifecycleState.Completed
  };

  private static readonly HashSet<string> StudentVisibleStatuses = new(StringComparer.OrdinalIgnoreCase)
  {
    "assigned",
    "active",
    "scheduled",
    "completed"
  };

  private readonly IUnitOfWork _unitOfWork;

  public GetStudentClassroomsQueryHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<List<ClassroomDto>>> Handle(GetStudentClassroomsQuery request, CancellationToken cancellationToken)
  {
    var classrooms = await _unitOfWork.ClassroomRepository.GetStudentClassroomsAsync(request.UserId);

    var result = new List<ClassroomDto>();
    foreach (var c in classrooms)
    {
      var studentCount = await _unitOfWork.ClassroomRepository.GetStudentCountAsync(c.Id);
      var modulesCount = await _unitOfWork.ClassroomRepository.GetModuleCountAsync(c.Id);
      var challenges = await _unitOfWork.ClassroomRepository.GetClassroomChallengesAsync(c.Id);
      var visibleChallengeCount = challenges.Count(challenge =>
          StudentVisibleStates.Contains(challenge.LifecycleState) ||
          StudentVisibleStatuses.Contains(challenge.Status));
      var subjects = c.Subjects.Count > 0
          ? c.Subjects.Select(s => s.Subject).ToList()
          : string.IsNullOrWhiteSpace(c.Subject) ? new List<string>() : new List<string> { c.Subject };
      result.Add(new ClassroomDto(c.Id, c.Name, c.Description, c.Subject, c.YearLevel, c.Thumbnail,
          c.JoinCode, c.TeacherId, c.Teacher?.UserName ?? "", studentCount, visibleChallengeCount, modulesCount, subjects));
    }

    return OperationResult<List<ClassroomDto>>.SuccessResult(result);
  }
}
