using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.Quiz;
using CleanArc.Domain.Entities.User;
using CleanArc.Infrastructure.Persistence;
using CleanArc.Infrastructure.Persistence.Services.Adaptive;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CleanArc.Tests.Setup.Features.Adaptive;

public class ClassroomModuleManagementServiceTests
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
  public async Task DeleteChallenge_AllowsArchivedChallengeOwnedThroughClassroom()
  {
    await using var context = CreateContext();
    var teacher = await AddUserAsync(context, "teacher-delete");
    var classroom = await AddClassroomAsync(context, teacher.Id, "Archive Ready");
    var game = await AddGameAsync(context);
    var challenge = await AddChallengeAsync(context, game.Id, classroom.Id, null, ChallengeLifecycleState.Archived);

    var service = CreateService(context);

    var result = await service.DeleteChallengeAsync(challenge.Id, teacher.Id, CancellationToken.None);

    Assert.True(result);
    Assert.Null(await context.Challenges.FirstOrDefaultAsync(c => c.Id == challenge.Id));
  }

  [Fact]
  public async Task DeleteChallenge_RejectsNonArchivedChallenge()
  {
    await using var context = CreateContext();
    var teacher = await AddUserAsync(context, "teacher-active");
    var classroom = await AddClassroomAsync(context, teacher.Id, "Active Challenge");
    var game = await AddGameAsync(context);
    var challenge = await AddChallengeAsync(context, game.Id, classroom.Id, teacher.Id, ChallengeLifecycleState.Active);

    var service = CreateService(context);

    var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
      service.DeleteChallengeAsync(challenge.Id, teacher.Id, CancellationToken.None));

    Assert.Contains("archived", exception.Message, StringComparison.OrdinalIgnoreCase);
    Assert.NotNull(await context.Challenges.FirstOrDefaultAsync(c => c.Id == challenge.Id));
  }

  private static ClassroomModuleManagementService CreateService(ApplicationDbContext context)
  {
    return new ClassroomModuleManagementService(
      context,
      Substitute.For<IChallengeOrchestrator>(),
      Substitute.For<IChallengeAiPipelineService>(),
      Substitute.For<IAiAuditService>(),
      Substitute.For<ILogger<ClassroomModuleManagementService>>());
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

  private static async Task<Game> AddGameAsync(ApplicationDbContext context)
  {
    var game = new Game
    {
      Key = $"game-{Guid.NewGuid():N}",
      Name = "Test Game",
      Description = "Test Description",
      ImageUrl = "https://example.com/game.png",
      Category = "TEST",
      SkillsTaught = "Testing"
    };

    context.Games.Add(game);
    await context.SaveChangesAsync();
    return game;
  }

  private static async Task<Challenge> AddChallengeAsync(
    ApplicationDbContext context,
    int gameId,
    int classroomId,
    int? createdById,
    ChallengeLifecycleState lifecycleState)
  {
    var challenge = new Challenge
    {
      GameId = gameId,
      Title = "Archived Challenge",
      Description = "Test challenge",
      DifficultyLevel = 1,
      ContentData = "{}",
      OrderIndex = 1,
      MaxStars = 3,
      CreatedById = createdById,
      ClassroomId = classroomId,
      Status = "assigned",
      LifecycleState = lifecycleState
    };

    context.Challenges.Add(challenge);
    await context.SaveChangesAsync();
    return challenge;
  }
}
