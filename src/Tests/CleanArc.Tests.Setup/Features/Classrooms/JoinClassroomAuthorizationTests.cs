using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Features.Classrooms.Commands;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.User;
using NSubstitute;

namespace CleanArc.Tests.Setup.Features.Classrooms;

public class JoinClassroomAuthorizationTests
{
  [Fact]
  public async Task JoinClassroom_AllowsAuthenticatedStudentUser()
  {
    var classroomRepository = Substitute.For<IClassroomRepository>();
    var unitOfWork = Substitute.For<IUnitOfWork>();
    var userManager = Substitute.For<IAppUserManager>();
    unitOfWork.ClassroomRepository.Returns(classroomRepository);

    var user = new User
    {
      Id = 101,
      UserName = "student-one",
      Email = "student-one@test.local",
      Name = "Student One"
    };

    userManager.GetUserByIdAsync(user.Id).Returns(user);
    userManager.GetUserRolesAsync(user).Returns(["student"]);
    classroomRepository.GetClassroomByJoinCodeAsync("AB12")
        .Returns(new Classroom
        {
          Name = "Language Lab",
          JoinCode = "AB12",
          TeacherId = 7,
          IsActive = true
        });
    classroomRepository.JoinClassroomAsync(Arg.Any<ClassroomStudent>())
        .Returns(call => call.Arg<ClassroomStudent>());

    var handler = new JoinClassroomCommandHandler(unitOfWork, userManager);

    var result = await handler.Handle(new JoinClassroomCommand(user.Id, "ab12"), CancellationToken.None);

    Assert.True(result.IsSuccess);
    await classroomRepository.Received(1).JoinClassroomAsync(Arg.Is<ClassroomStudent>(student =>
        student.UserId == user.Id));
  }

  [Fact]
  public async Task JoinClassroom_RejectsAuthenticatedNonStudentUser()
  {
    var classroomRepository = Substitute.For<IClassroomRepository>();
    var unitOfWork = Substitute.For<IUnitOfWork>();
    var userManager = Substitute.For<IAppUserManager>();
    unitOfWork.ClassroomRepository.Returns(classroomRepository);

    var user = new User
    {
      Id = 99,
      UserName = "teacher-one",
      Email = "teacher-one@test.local",
      Name = "Teacher One"
    };

    userManager.GetUserByIdAsync(user.Id).Returns(user);
    userManager.GetUserRolesAsync(user).Returns(["teacher"]);
    classroomRepository.GetClassroomByJoinCodeAsync("AB12")
        .Returns(new Classroom
        {
          Name = "Language Lab",
          JoinCode = "AB12",
          TeacherId = 7,
          IsActive = true
        });

    var handler = new JoinClassroomCommandHandler(unitOfWork, userManager);

    var result = await handler.Handle(new JoinClassroomCommand(user.Id, "AB12"), CancellationToken.None);

    Assert.False(result.IsSuccess);
    Assert.True(result.IsForbidden);
    Assert.Equal("Only students can join classrooms using a join code.", result.ErrorMessage);
    await classroomRepository.DidNotReceiveWithAnyArgs().JoinClassroomAsync(default!);
  }
}
