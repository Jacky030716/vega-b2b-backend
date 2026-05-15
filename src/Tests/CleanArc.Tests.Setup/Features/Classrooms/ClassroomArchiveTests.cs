using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Features.Classrooms.Commands;
using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.User;
using CleanArc.Infrastructure.Persistence;
using CleanArc.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace CleanArc.Tests.Setup.Features.Classrooms;

public class ClassroomArchiveTests
{
  private static ApplicationDbContext CreateContext()
  {
    var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = ":memory:" }.ToString());
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlite(connection)
        .Options;

    var context = new ApplicationDbContext(options);
    context.Database.OpenConnection();
    context.Database.EnsureCreated();
    return context;
  }

  [Fact]
  public async Task UpdateClassroom_AllowsOwnerToEditDetails()
  {
    await using var context = CreateContext();
    var repo = new ClassroomRepository(context);
    var unitOfWork = Substitute.For<IUnitOfWork>();
    unitOfWork.ClassroomRepository.Returns(repo);
    var teacher = await AddUserAsync(context, "teacher-edit");
    var classroom = await AddClassroomAsync(context, teacher.Id, "Old Name");
    var handler = new UpdateClassroomCommandHandler(unitOfWork);

    var result = await handler.Handle(new UpdateClassroomCommand(
        classroom.Id,
        teacher.Id,
        false,
        "New classroom name",
        "Bahasa Melayu",
        null,
        1,
        "Updated description",
        null), CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.Equal("New classroom name", result.Result.Name);
    Assert.Equal("Bahasa Melayu", result.Result.Subject);
    Assert.Equal("Updated description", result.Result.Description);
  }

  [Fact]
  public async Task UpdateClassroom_RejectsEmptyName()
  {
    await using var context = CreateContext();
    var repo = new ClassroomRepository(context);
    var unitOfWork = Substitute.For<IUnitOfWork>();
    unitOfWork.ClassroomRepository.Returns(repo);
    var teacher = await AddUserAsync(context, "teacher-empty");
    var classroom = await AddClassroomAsync(context, teacher.Id, "Class Name");
    var handler = new UpdateClassroomCommandHandler(unitOfWork);

    var result = await handler.Handle(new UpdateClassroomCommand(
        classroom.Id,
        teacher.Id,
        false,
        " ",
        "Science",
        null,
        1,
        null,
        null), CancellationToken.None);

    Assert.False(result.IsSuccess);
    Assert.Contains("required", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public async Task UpdateClassroom_RejectsNonOwner()
  {
    await using var context = CreateContext();
    var repo = new ClassroomRepository(context);
    var unitOfWork = Substitute.For<IUnitOfWork>();
    unitOfWork.ClassroomRepository.Returns(repo);
    var owner = await AddUserAsync(context, "teacher-owner");
    var otherTeacher = await AddUserAsync(context, "teacher-other");
    var classroom = await AddClassroomAsync(context, owner.Id, "Class Name");
    var handler = new UpdateClassroomCommandHandler(unitOfWork);

    var result = await handler.Handle(new UpdateClassroomCommand(
        classroom.Id,
        otherTeacher.Id,
        false,
        "New Name",
        "Science",
        null,
        1,
        null,
        null), CancellationToken.None);

    Assert.False(result.IsSuccess);
    Assert.True(result.IsForbidden);
  }

  [Fact]
  public async Task UpdateClassroom_RejectsYearLevelChangeAfterCustomModuleExists()
  {
    await using var context = CreateContext();
    var repo = new ClassroomRepository(context);
    var unitOfWork = Substitute.For<IUnitOfWork>();
    unitOfWork.ClassroomRepository.Returns(repo);
    var teacher = await AddUserAsync(context, "teacher-module");
    var classroom = await AddClassroomAsync(context, teacher.Id, "Class Name");
    context.ClassroomModules.Add(new ClassroomModule
    {
      ClassroomId = classroom.Id,
      Module = new SyllabusModule
      {
        ModuleCode = $"CUSTOM-{classroom.Id}-{Guid.NewGuid():N}",
        Subject = classroom.Subject,
        Language = "ms",
        YearLevel = 1,
        Term = string.Empty,
        UnitTitle = "Custom Module",
        Title = "Custom Module",
        Description = "Teacher-created learning module.",
        ModuleType = SyllabusModule.CustomModuleType,
        SourceType = "teacher_created",
        CreatedByTeacherId = teacher.Id
      }
    });
    await context.SaveChangesAsync();
    var handler = new UpdateClassroomCommandHandler(unitOfWork);

    var result = await handler.Handle(new UpdateClassroomCommand(
        classroom.Id,
        teacher.Id,
        false,
        "Class Name",
        "Science",
        null,
        2,
        null,
        null), CancellationToken.None);

    Assert.False(result.IsSuccess);
    Assert.Equal("Year level cannot be changed after modules or challenges have been created.", result.ErrorMessage);
  }

  [Fact]
  public async Task ArchiveClassroom_SoftDeletesAndPreservesMembers()
  {
    await using var context = CreateContext();
    var repo = new ClassroomRepository(context);
    var unitOfWork = Substitute.For<IUnitOfWork>();
    unitOfWork.ClassroomRepository.Returns(repo);
    var teacher = await AddUserAsync(context, "teacher-archive");
    var student = await AddUserAsync(context, "student-archive");
    var classroom = await AddClassroomAsync(context, teacher.Id, "Archive Me");
    context.ClassroomStudents.Add(new ClassroomStudent
    {
      ClassroomId = classroom.Id,
      UserId = student.Id,
      JoinedDate = DateTime.UtcNow
    });
    await context.SaveChangesAsync();
    var handler = new ArchiveClassroomCommandHandler(unitOfWork);

    var result = await handler.Handle(new ArchiveClassroomCommand(classroom.Id, teacher.Id, false), CancellationToken.None);

    Assert.True(result.IsSuccess);
    var archived = await repo.GetClassroomByIdAsync(classroom.Id, includeDeleted: true);
    Assert.NotNull(archived);
    Assert.True(archived.IsDeleted);
    Assert.False(archived.IsActive);
    Assert.Equal(teacher.Id, archived.DeletedBy);
    Assert.NotNull(archived.DeletedAt);
    Assert.Null(await repo.GetClassroomByIdAsync(classroom.Id));
    Assert.Empty(await repo.GetTeacherClassroomsAsync(teacher.Id));
    Assert.Empty(await repo.GetStudentClassroomsAsync(student.Id));
    Assert.Equal(1, await context.ClassroomStudents.CountAsync(cs => cs.ClassroomId == classroom.Id));
  }

  private static async Task<User> AddUserAsync(ApplicationDbContext context, string userName)
  {
    var user = new User
    {
      UserName = userName,
      Email = $"{userName}@example.com",
      Name = userName,
      Experience = 1
    };
    context.Users.Add(user);
    await context.SaveChangesAsync();
    return user;
  }

  private static async Task<Classroom> AddClassroomAsync(ApplicationDbContext context, int teacherId, string name)
  {
    var classroom = new Classroom
    {
      Name = name,
      Description = "Description",
      Subject = "Science",
      YearLevel = 1,
      TeacherId = teacherId,
      JoinCode = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant(),
      IsActive = true
    };
    context.Classrooms.Add(classroom);
    await context.SaveChangesAsync();
    return classroom;
  }
}
