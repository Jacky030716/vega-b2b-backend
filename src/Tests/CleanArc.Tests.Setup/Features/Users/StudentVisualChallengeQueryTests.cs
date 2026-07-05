using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Features.Users.Queries.StudentVisualChallenge;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.User;
using NSubstitute;

namespace CleanArc.Tests.Setup.Features.Users;

public class StudentVisualChallengeQueryTests
{
  [Fact]
  public async Task StudentVisualChallenge_ReactivatesExistingInactiveCredential()
  {
    var classroomRepository = Substitute.For<IClassroomRepository>();
    var credentialRepository = Substitute.For<IStudentCredentialRepository>();
    var unitOfWork = Substitute.For<IUnitOfWork>();
    unitOfWork.ClassroomRepository.Returns(classroomRepository);
    unitOfWork.StudentCredentialRepository.Returns(credentialRepository);

    classroomRepository.GetClassroomByJoinCodeAsync("E5A1").Returns(SetId(new Classroom
    {
      Name = "Language Lab",
      JoinCode = "E5A1",
      TeacherId = 7,
      IsActive = true
    }, 12));

    classroomRepository.GetClassroomMembersAsync(12).Returns(new List<ClassroomStudent>
    {
      new()
      {
        ClassroomId = 12,
        UserId = 42,
        User = new User
        {
          Id = 42,
          Name = "Alpha Student",
          UserName = "student-alpha",
          AvatarId = "0",
          AvatarUrl = ""
        }
      }
    });

    credentialRepository.GetByClassroomIdAsync(12).Returns(new List<StudentCredential>());
    credentialRepository.GetByUserIdAsync(42).Returns(new List<StudentCredential>
    {
      new()
      {
        ClassroomId = 12,
        UserId = 42,
        StudentLoginCode = "1234",
        VisualPasswordHash = "DEFAULT",
        IsActive = false,
        FailedAttempts = 2,
        LastFailedAt = DateTime.UtcNow.AddMinutes(-5),
        LastSuccessfulLoginAt = DateTime.UtcNow.AddDays(-1)
      }
    });

    var handler = new StudentVisualChallengeQueryHandler(unitOfWork);

    var result = await handler.Handle(new StudentVisualChallengeQuery("e5a1"), CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.Single(result.Result.Students);
    Assert.Equal("1234", result.Result.Students[0].StudentLoginCode);
    await credentialRepository.DidNotReceive().CreateAsync(Arg.Any<StudentCredential>());
    await credentialRepository.Received(1).UpdateAsync(Arg.Is<StudentCredential>(credential =>
      credential.ClassroomId == 12 &&
      credential.UserId == 42 &&
      credential.StudentLoginCode == "1234" &&
      credential.IsActive &&
      credential.FailedAttempts == 0 &&
      credential.LastFailedAt == null));
    await unitOfWork.Received(1).CommitAsync();
  }

  private static T SetId<T>(T entity, int id)
  {
    typeof(T).GetProperty("Id")!.SetValue(entity, id);
    return entity;
  }
}
