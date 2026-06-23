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
    var teacher = SetId(new User
    {
      UserName = "teacher01",
      Name = "Ms Vega",
      Email = "teacher@school.test",
      InstitutionId = 3,
      WeeklyAiInsightsEmail = true,
      InactiveStudentAlerts = false
    }, 7);
    var classroom = SetId(new Classroom { Name = "Year 1", TeacherId = teacher.Id }, 11);
    var activeStudent = SetId(new User { UserName = "active", Name = "Active", Experience = 80 }, 21);
    var lowScoreStudent = SetId(new User { UserName = "support", Name = "Support", Experience = 20 }, 22);
    var noCompletionStudent = SetId(new User { UserName = "new", Name = "New", Experience = 0 }, 23);
    var challenge = SetId(new Challenge { ClassroomId = classroom.Id, CreatedById = teacher.Id }, 31);

    var userManager = Substitute.For<IAppUserManager>();
    userManager.GetUserByIdAsync(teacher.Id).Returns(teacher);
    userManager.GetUserRolesAsync(teacher).Returns(["teacher"]);

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
    var institution = new Institution
    {
      Id = teacher.InstitutionId.Value,
      Name = "Vega Primary",
      SubscriptionTier = "Standard",
      SeatsUsed = 18,
      MaxSeats = 50
    };
    unitOfWork.InstitutionRepository
      .GetPrimaryInstitutionForUserAsync(teacher.Id, Arg.Any<CancellationToken>())
      .Returns(new InstitutionUser
      {
        InstitutionId = institution.Id,
        Institution = institution,
        UserId = teacher.Id,
        AccessScope = "Teacher access",
        IsActive = true,
        IsPrimary = true
      });

    var aiUsageService = Substitute.For<IAiUsageService>();
    aiUsageService
      .GetRemainingQuotaAsync(teacher.Id, AiFeatureTypes.CustomChallengeGeneration, Arg.Any<CancellationToken>())
      .Returns(new AiQuotaResult(30, 8, 22));

    var handler = new GetTeacherProfileQueryHandler(userManager, unitOfWork, aiUsageService);

    var result = await handler.Handle(new GetTeacherProfileQuery(teacher.Id), CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.Equal("Ms Vega", result.Result.FullName);
    Assert.Equal("Teacher", result.Result.RoleLabel);
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
  public async Task GetTeacherProfile_ReturnsAdministratorLabelForAdminRole()
  {
    var admin = SetId(new User
    {
      UserName = "admin01",
      Name = "Vega Admin",
      Email = "admin@school.test"
    }, 8);
    var userManager = Substitute.For<IAppUserManager>();
    userManager.GetUserByIdAsync(admin.Id).Returns(admin);
    userManager.GetUserRolesAsync(admin).Returns(["admin", "teacher"]);

    var unitOfWork = Substitute.For<IUnitOfWork>();
    unitOfWork.ClassroomRepository.GetTeacherClassroomsAsync(admin.Id).Returns([]);
    unitOfWork.InstitutionRepository
      .GetPrimaryInstitutionForUserAsync(admin.Id, Arg.Any<CancellationToken>())
      .Returns((InstitutionUser)null);

    var aiUsageService = Substitute.For<IAiUsageService>();
    aiUsageService
      .GetRemainingQuotaAsync(admin.Id, AiFeatureTypes.CustomChallengeGeneration, Arg.Any<CancellationToken>())
      .Returns(new AiQuotaResult(30, 0, 30));

    var handler = new GetTeacherProfileQueryHandler(userManager, unitOfWork, aiUsageService);

    var result = await handler.Handle(new GetTeacherProfileQuery(admin.Id), CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.Equal("Administrator", result.Result.RoleLabel);
  }

  [Fact]
  public async Task UpdateTeacherPreferences_PersistsSupportedPreferenceFields()
  {
    var teacher = SetId(new User
    {
      UserName = "teacher-prefs",
      WeeklyAiInsightsEmail = true,
      InactiveStudentAlerts = false
    }, 9);
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

  private static T SetId<T>(T entity, int id)
  {
    entity.GetType().GetProperty("Id")!.SetValue(entity, id);
    return entity;
  }
}
