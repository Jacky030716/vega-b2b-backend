using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Users.Queries.GetTeacherProfile;

internal class GetTeacherProfileQueryHandler(
    IAppUserManager userManager,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GetTeacherProfileQuery, OperationResult<TeacherProfileDto>>
{
  public async ValueTask<OperationResult<TeacherProfileDto>> Handle(
      GetTeacherProfileQuery request,
      CancellationToken cancellationToken)
  {
    var teacher = await userManager.GetUserByIdAsync(request.TeacherId);
    if (teacher is null)
      return OperationResult<TeacherProfileDto>.NotFoundResult("Teacher not found");

    var classrooms = await unitOfWork.ClassroomRepository.GetTeacherClassroomsAsync(request.TeacherId);
    var uniqueStudents = new Dictionary<int, (int Diamonds, string UserName)>();
    var challengesLaunched = 0;

    foreach (var classroom in classrooms)
    {
      var members = await unitOfWork.ClassroomRepository.GetClassroomMembersAsync(classroom.Id);
      foreach (var member in members)
      {
        if (!uniqueStudents.ContainsKey(member.UserId))
        {
          uniqueStudents[member.UserId] = (member.User.Diamonds, member.User.UserName);
        }
      }

      challengesLaunched += await unitOfWork.ClassroomRepository.GetQuizCountAsync(classroom.Id);
    }

    var avatarUrl = teacher.AvatarUrl;
    if (string.IsNullOrWhiteSpace(avatarUrl) &&
        int.TryParse(teacher.AvatarId, out var avatarItemId) &&
        avatarItemId > 0)
    {
      var avatarItem = await unitOfWork.ShopRepository.GetShopItemByIdAsync(avatarItemId);
      if (avatarItem is not null &&
          string.Equals(avatarItem.Category, "avatar", StringComparison.OrdinalIgnoreCase))
      {
        avatarUrl = avatarItem.ImageUrl;
      }
    }

    var fullName = string.IsNullOrWhiteSpace(teacher.Name)
        ? teacher.UserName
        : teacher.Name;

    var result = new TeacherProfileDto(
        fullName,
        teacher.Email ?? string.Empty,
        "Teacher",
        avatarUrl,
        "professor",
        new TeacherProfileStatsDto(
            uniqueStudents.Count,
            challengesLaunched,
            uniqueStudents.Values.Sum(student => student.Diamonds)),
        new TeacherPreferencesDto(
            teacher.WeeklyAiInsightsEmail,
            teacher.InactiveStudentAlerts));

    return OperationResult<TeacherProfileDto>.SuccessResult(result);
  }
}
