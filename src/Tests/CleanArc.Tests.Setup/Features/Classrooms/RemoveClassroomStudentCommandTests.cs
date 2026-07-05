using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Features.Classrooms.Commands;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.User;
using NSubstitute;

namespace CleanArc.Tests.Setup.Features.Classrooms;

public class RemoveClassroomStudentCommandTests
{
  [Fact]
  public async Task RemoveClassroomStudent_DeactivatesExistingCredential()
  {
    var classroomRepository = Substitute.For<IClassroomRepository>();
    var credentialRepository = Substitute.For<IStudentCredentialRepository>();
    var unitOfWork = Substitute.For<IUnitOfWork>();
    unitOfWork.ClassroomRepository.Returns(classroomRepository);
    unitOfWork.StudentCredentialRepository.Returns(credentialRepository);

    classroomRepository.GetClassroomByIdAsync(12, false, false).Returns(new Classroom
    {
      Name = "Language Lab",
      TeacherId = 7,
      IsActive = true
    });
    classroomRepository.GetClassroomStudentAsync(12, 42).Returns(new ClassroomStudent
    {
      ClassroomId = 12,
      UserId = 42,
      JoinedDate = DateTime.UtcNow
    });
    classroomRepository.RemoveClassroomStudentAsync(12, 42).Returns(true);
    credentialRepository.GetByUserIdAsync(42).Returns(new List<StudentCredential>
    {
      new()
      {
        ClassroomId = 12,
        UserId = 42,
        StudentLoginCode = "1234",
        VisualPasswordHash = "DEFAULT",
        IsActive = true
      }
    });

    var handler = new RemoveClassroomStudentCommandHandler(unitOfWork);

    var result = await handler.Handle(new RemoveClassroomStudentCommand(12, 42, 7, false), CancellationToken.None);

    Assert.True(result.IsSuccess);
    await classroomRepository.Received(1).RemoveClassroomStudentAsync(12, 42);
    await credentialRepository.Received(1).UpdateAsync(Arg.Is<StudentCredential>(credential =>
      credential.ClassroomId == 12 &&
      credential.UserId == 42 &&
      credential.IsActive == false));
    await unitOfWork.Received(1).CommitAsync();
  }
}
