using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.User;
using CleanArc.Infrastructure.Persistence;
using CleanArc.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Test.Application.Features.Classrooms;

public class ClassroomJoinAndMembersTest
{
  private ApplicationDbContext GetTestDbContext()
  {
    var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = ":memory:" };
    var connection = new SqliteConnection(connectionStringBuilder.ToString());

    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlite(connection)
        .Options;

    var context = new ApplicationDbContext(options);
    context.Database.OpenConnection();
    context.Database.EnsureCreated();
    return context;
  }

  [Fact]
  public async Task JoinClassroom_WithValidCode_AddsStudentToClassroom()
  {
    // Arrange
    var context = GetTestDbContext();
    var repo = new ClassroomRepository(context);

    var teacher = new User { UserName = "teacher1", Email = "teacher@test.com", Name = "Teacher", Experience = 100 };
    var student = new User { UserName = "student1", Email = "student@test.com", Name = "Student", Experience = 50 };

    context.Users.Add(teacher);
    context.Users.Add(student);

    var classroom = new Classroom
    {
      Name = "Math 101",
      Description = "Basic Math",
      Subject = "Mathematics",
      TeacherId = teacher.Id,
      JoinCode = "ABC12345",
      IsActive = true
    };

    context.Classrooms.Add(classroom);
    await context.SaveChangesAsync();

    var classroomStudent = new ClassroomStudent
    {
      ClassroomId = classroom.Id,
      UserId = student.Id,
      JoinedDate = DateTime.UtcNow
    };

    // Act
    var result = await repo.JoinClassroomAsync(classroomStudent);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(student.Id, result.UserId);
    Assert.Equal(classroom.Id, result.ClassroomId);

    var storedStudent = await repo.GetClassroomStudentAsync(classroom.Id, student.Id);
    Assert.NotNull(storedStudent);
  }

  [Fact]
  public async Task GetClassroomMembers_ReturnsMembersOrderedByXPDescending()
  {
    // Arrange
    var context = GetTestDbContext();
    var repo = new ClassroomRepository(context);

    var teacher = new User { UserName = "teacher2", Email = "teacher2@test.com", Name = "Teacher", Experience = 200 };
    var student1 = new User { UserName = "student3", Email = "student3@test.com", Name = "Alice", Experience = 150 };
    var student2 = new User { UserName = "student4", Email = "student4@test.com", Name = "Bob", Experience = 100 };
    var student3 = new User { UserName = "student5", Email = "student5@test.com", Name = "Charlie", Experience = 50 };

    context.Users.AddRange(teacher, student1, student2, student3);

    var classroom = new Classroom
    {
      Name = "Physics 101",
      Description = "Basic Physics",
      Subject = "Physics",
      TeacherId = teacher.Id,
      JoinCode = "PHY12345",
      IsActive = true
    };

    context.Classrooms.Add(classroom);
    await context.SaveChangesAsync();

    // Add students to classroom in non-sorted order (Bob, Charlie, Alice)
    var cs1 = new ClassroomStudent { ClassroomId = classroom.Id, UserId = student2.Id, JoinedDate = DateTime.UtcNow };
    var cs2 = new ClassroomStudent { ClassroomId = classroom.Id, UserId = student3.Id, JoinedDate = DateTime.UtcNow };
    var cs3 = new ClassroomStudent { ClassroomId = classroom.Id, UserId = student1.Id, JoinedDate = DateTime.UtcNow };

    context.ClassroomStudents.AddRange(cs1, cs2, cs3);
    await context.SaveChangesAsync();

    // Act
    var members = await repo.GetClassroomMembersAsync(classroom.Id);

    // Assert
    Assert.NotNull(members);
    Assert.Equal(3, members.Count);

    // Verify order by XP descending: Alice (150) → Bob (100) → Charlie (50)
    Assert.Equal(student1.Id, members[0].User.Id);
    Assert.Equal("Alice", members[0].User.Name);
    Assert.Equal(150, members[0].User.Experience);

    Assert.Equal(student2.Id, members[1].User.Id);
    Assert.Equal("Bob", members[1].User.Name);
    Assert.Equal(100, members[1].User.Experience);

    Assert.Equal(student3.Id, members[2].User.Id);
    Assert.Equal("Charlie", members[2].User.Name);
    Assert.Equal(50, members[2].User.Experience);
  }

  [Fact]
  public async Task GetClassroomMembers_EmptyClassroom_ReturnsEmptyList()
  {
    // Arrange
    var context = GetTestDbContext();
    var repo = new ClassroomRepository(context);

    var teacher = new User { UserName = "teacher3", Email = "teacher3@test.com", Name = "Teacher" };
    context.Users.Add(teacher);

    var classroom = new Classroom
    {
      Name = "Empty Class",
      Description = "No students",
      Subject = "Testing",
      TeacherId = teacher.Id,
      JoinCode = "EMPTY01",
      IsActive = true
    };

    context.Classrooms.Add(classroom);
    await context.SaveChangesAsync();

    // Act
    var members = await repo.GetClassroomMembersAsync(classroom.Id);

    // Assert
    Assert.NotNull(members);
    Assert.Empty(members);
  }

  [Fact]
  public async Task Student_CanJoinClassroom_Then_AppearsInMembersList()
  {
    // Arrange - Full integration test
    var context = GetTestDbContext();
    var repo = new ClassroomRepository(context);

    var teacher = new User { UserName = "teacher4", Email = "teacher4@test.com", Name = "Teacher", Experience = 100 };
    var student = new User { UserName = "student6", Email = "student6@test.com", Name = "Test Student", Experience = 75 };

    context.Users.AddRange(teacher, student);

    var classroom = new Classroom
    {
      Name = "Integration Test Class",
      Description = "Testing",
      Subject = "Testing",
      TeacherId = teacher.Id,
      JoinCode = "INT12345",
      IsActive = true
    };

    context.Classrooms.Add(classroom);
    await context.SaveChangesAsync();

    // Act 1 - Join classroom
    var classroomStudent = new ClassroomStudent
    {
      ClassroomId = classroom.Id,
      UserId = student.Id,
      JoinedDate = DateTime.UtcNow
    };

    var joinResult = await repo.JoinClassroomAsync(classroomStudent);
    Assert.NotNull(joinResult);

    // Act 2 - Get classroom members
    var members = await repo.GetClassroomMembersAsync(classroom.Id);

    // Assert
    Assert.NotNull(members);
    Assert.Single(members);

    var member = members.First();
    Assert.Equal(student.Id, member.User.Id);
    Assert.Equal("Test Student", member.User.Name);
    Assert.Equal(75, member.User.Experience);
  }
}

