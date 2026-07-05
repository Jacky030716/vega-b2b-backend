using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Features.Classrooms.Queries;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.Institution;
using CleanArc.Domain.Entities.User;
using NSubstitute;

namespace CleanArc.Tests.Setup.Features.Classrooms;

public class GetClassroomAssignableStudentsQueryTests
{
  [Fact]
  public async Task GetClassroomAssignableStudents_ExcludesExistingMembers()
  {
    var classroomRepository = Substitute.For<IClassroomRepository>();
    var institutionUserReportRepository = Substitute.For<IInstitutionUserReportRepository>();
    var unitOfWork = Substitute.For<IUnitOfWork>();
    unitOfWork.ClassroomRepository.Returns(classroomRepository);

    classroomRepository.GetClassroomByIdAsync(12, false, false).Returns(new Classroom
    {
      Name = "Language Lab",
      TeacherId = 7,
      Teacher = new User
      {
        UserName = "teacher-one",
        Name = "Teacher One",
        InstitutionId = 99
      },
      IsActive = true
    });

    classroomRepository.GetClassroomMembersAsync(12).Returns(new List<ClassroomStudent>
    {
      new()
      {
        UserId = 42,
        User = new User
        {
          UserName = "student-alpha",
          Name = "Alpha Student"
        }
      }
    });

    institutionUserReportRepository.GetUsersAsync(
      Arg.Is<InstitutionUserReportFilter>(filter =>
        filter.InstitutionId == 99 &&
        filter.Role == "student" &&
        filter.Tab == "all"),
      Arg.Any<CancellationToken>()).Returns(new List<InstitutionUserReportRow>
    {
      new(
        42,
        "Alpha",
        "Student",
        "student-alpha",
        "alpha@test.local",
        "student",
        true,
        null,
        "Year 1",
        true,
        false,
        "1234"),
      new(
        55,
        "Bravo",
        "Student",
        "student-bravo",
        "bravo@test.local",
        "student",
        true,
        null,
        "Year 1",
        true,
        false,
        "5678")
    });

    var handler = new GetClassroomAssignableStudentsQueryHandler(unitOfWork, institutionUserReportRepository);

    var result = await handler.Handle(new GetClassroomAssignableStudentsQuery(12, 7, false), CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.Single(result.Result);
    Assert.Equal(55, result.Result[0].Id);
    Assert.Equal("Bravo Student", result.Result[0].DisplayName);
  }
}
