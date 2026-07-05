using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Features.Classrooms.Commands;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.User;
using NSubstitute;

namespace CleanArc.Tests.Setup.Features.Classrooms;

public class AddClassroomStudentCommandTests
{
  [Fact]
  public async Task AddClassroomStudent_AssignsStudentAndCreatesCredential()
  {
    var classroomRepository = Substitute.For<IClassroomRepository>();
    var credentialRepository = Substitute.For<IStudentCredentialRepository>();
    var unitOfWork = Substitute.For<IUnitOfWork>();
    var userManager = Substitute.For<IAppUserManager>();
    unitOfWork.ClassroomRepository.Returns(classroomRepository);
    unitOfWork.StudentCredentialRepository.Returns(credentialRepository);

    classroomRepository.GetClassroomByIdAsync(12, false, false).Returns(new Classroom
    {
      Name = "Language Lab",
      TeacherId = 7,
      IsActive = true
    });
    classroomRepository.GetClassroomStudentAsync(12, 42).Returns((ClassroomStudent?)null);
    classroomRepository.JoinClassroomAsync(Arg.Any<ClassroomStudent>()).Returns(call => call.Arg<ClassroomStudent>());
    credentialRepository.GetByLoginCodeAsync(Arg.Any<string>()).Returns((StudentCredential?)null);

    var student = new User
    {
      UserName = "student-alpha",
      Email = "alpha@test.local",
      Name = "Alpha Student"
    };

    userManager.GetUserByIdAsync(42).Returns(student);
    userManager.GetUserRolesAsync(student).Returns(["student"]);

    var handler = new AddClassroomStudentCommandHandler(unitOfWork, userManager);

    var result = await handler.Handle(new AddClassroomStudentCommand(12, 42, 7, false), CancellationToken.None);

    Assert.True(result.IsSuccess);
    await classroomRepository.Received(1).JoinClassroomAsync(Arg.Is<ClassroomStudent>(studentMembership =>
      studentMembership.ClassroomId == 12 &&
      studentMembership.UserId == 42));
    await credentialRepository.Received(1).CreateAsync(Arg.Is<StudentCredential>(credential =>
      credential.ClassroomId == 12 &&
      credential.UserId == 42 &&
      credential.IsActive));
    await unitOfWork.Received(1).CommitAsync();
  }
}
