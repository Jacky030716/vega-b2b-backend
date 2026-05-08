using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Features.Users.Commands.UpdateTeacherPreferences;
using CleanArc.Application.Features.Users.Queries.GetTeacherProfile;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.Institution;
using CleanArc.Domain.Entities.Quiz;
using CleanArc.Domain.Entities.User;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace CleanArc.Tests.Setup.Features.Users;

public class TeacherProfileTests
{
  [Fact]
  public async Task GetTeacherProfile_ReturnsB2BSnapshotWithoutRewardStats()
  {
    var teacher = new User
    {
      Id = 7,
      UserName = "teacher01",
      Name = "Ms Vega",
      Email = "teacher@school.test",
      InstitutionId = 3,
      WeeklyAiInsightsEmail = true,
      InactiveStudentAlerts = false
    };
    var classroom = new Classroom { Id = 11, Name = "Year 1", TeacherId = teacher.Id };
    var activeStudent = new User { Id = 21, UserName = "active", Name = "Active", Experience = 80 };
    var lowScoreStudent = new User { Id = 22, UserName = "support", Name = "Support", Experience = 20 };
    var noCompletionStudent = new User { Id = 23, UserName = "new", Name = "New", Experience = 0 };
    var challenge = new Challenge { Id = 31, ClassroomId = classroom.Id, CreatedById = teacher.Id };

    var userManager = Substitute.For<IAppUserManager>();
    userManager.GetUserByIdAsync(teacher.Id).Returns(teacher);

    var unitOfWork = Substitute.For<IUnitOfWork>();
    unitOfWork.ClassroomRepository.GetTeacherClassroomsAsync(teacher.Id).Returns([classroom]);
    unitOfWork.ClassroomRepository.GetClassroomMembersAsync(classroom.Id).Returns([
      new ClassroomStudent { ClassroomId = classroom.Id, UserId = activeStudent.Id, User = activeStudent },
      new ClassroomStudent { ClassroomId = classroom.Id, UserId = lowScoreStudent.Id, User = lowScoreStudent },
      new ClassroomStudent { ClassroomId = classroom.Id, UserId = noCompletionStudent.Id, User = noCompletionStudent },
    ]);
    unitOfWork.ClassroomRepository.GetClassroomChallengesAsync(classroom.Id).Returns([challenge]);
    unitOfWork.ChallengeRepository.GetChallengeLeaderboardAsync(challenge.Id, classroom.Id).Returns([
      new ChallengeProgress
      {
        ClassroomId = classroom.Id,
        ChallengeId = challenge.Id,
        UserId = activeStudent.Id,
        AttemptCount = 1,
        HasCompleted = true,
        BestAccuracy = 92
      },
      new ChallengeProgress
      {
        ClassroomId = classroom.Id,
        ChallengeId = challenge.Id,
        UserId = lowScoreStudent.Id,
        AttemptCount = 1,
        HasCompleted = true,
        BestAccuracy = 35
      },
    ]);
    unitOfWork.InstitutionRepository.GetInstitutionWithStatsAsync(teacher.InstitutionId.Value).Returns(
      new Institution
      {
        Id = teacher.InstitutionId.Value,
        Name = "Vega Primary",
        SubscriptionTier = "Standard",
        SeatsUsed = 18,
        MaxSeats = 50
      });

    var aiUsageService = Substitute.For<IAiUsageService>();
    aiUsageService
      .GetRemainingQuotaAsync(teacher.Id, AiFeatureTypes.CustomChallengeGeneration, Arg.Any<CancellationToken>())
      .Returns(new AiQuotaResult(30, 8, 22));

    var handler = new GetTeacherProfileQueryHandler(userManager, unitOfWork, aiUsageService);

    var result = await handler.Handle(new GetTeacherProfileQuery(teacher.Id), CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.Equal("Ms Vega", result.Result.FullName);
    Assert.Equal("Vega Primary", result.Result.InstitutionName);
    Assert.Equal("Teacher access", result.Result.AccessScope);
    Assert.Equal(1, result.Result.Stats.ActiveClassrooms);
    Assert.Equal(2, result.Result.Stats.ActiveStudents);
    Assert.Equal(2, result.Result.Stats.StudentsNeedingSupport);
    Assert.Equal(22, result.Result.Stats.AiGenerationsRemaining);
    Assert.Equal("Standard", result.Result.Subscription?.PlanTier);
    Assert.Equal("School Admin", result.Result.Subscription?.BillingManagedBy);
    Assert.DoesNotContain(
      result.Result.Stats.GetType().GetProperties().Select(property => property.Name),
      propertyName => propertyName.Contains("Diamond", StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public async Task UpdateTeacherPreferences_PersistsSupportedPreferenceFields()
  {
    var teacher = new User
    {
      Id = 9,
      UserName = "teacher-prefs",
      WeeklyAiInsightsEmail = true,
      InactiveStudentAlerts = false
    };
    var userManager = Substitute.For<IAppUserManager>();
    userManager.GetUserByIdAsync(teacher.Id).Returns(teacher);
    userManager.UpdateUser(teacher).Returns(IdentityResult.Success);
    var handler = new UpdateTeacherPreferencesCommandHandler(userManager);

    var result = await handler.Handle(
      new UpdateTeacherPreferencesCommand(teacher.Id, false, true),
      CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.False(result.Result.WeeklyAiInsightsEmail);
    Assert.True(result.Result.InactiveStudentAlerts);
    Assert.False(teacher.WeeklyAiInsightsEmail);
    Assert.True(teacher.InactiveStudentAlerts);
    await userManager.Received(1).UpdateUser(teacher);
  }
}
