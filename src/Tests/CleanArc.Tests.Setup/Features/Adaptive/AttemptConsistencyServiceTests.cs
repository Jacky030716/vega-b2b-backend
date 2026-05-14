using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.Quiz;
using CleanArc.Domain.Entities.User;
using CleanArc.Infrastructure.Persistence;
using CleanArc.Infrastructure.Persistence.Services.Adaptive;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Tests.Setup.Features.Adaptive;

public class AttemptConsistencyServiceTests
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
  public async Task CheckClassroomAsync_ReportsMissingProgressForCompletedLegacyAttempt()
  {
    await using var context = CreateContext();
    var teacher = await AddUserAsync(context, "teacher-consistency");
    var student = await AddUserAsync(context, "student-consistency");
    var classroom = await AddClassroomAsync(context, teacher.Id);
    await AddClassroomStudentAsync(context, classroom.Id, student.Id);
    var game = await AddGameAsync(context);
    var challenge = await AddChallengeAsync(context, game.Id, classroom.Id);

    context.Attempts.Add(new Attempt
    {
      UserId = student.Id,
      ChallengeId = challenge.Id,
      Score = 88,
      StarsEarned = 3,
      IsCompleted = true,
      CompletedAt = DateTime.UtcNow
    });
    await context.SaveChangesAsync();

    var service = new AttemptConsistencyService(context);

    var report = await service.CheckClassroomAsync(
      classroom.Id,
      teacher.Id,
      false,
      null,
      null,
      null,
      CancellationToken.None);

    Assert.Contains(report.Issues, issue => issue.Code == "missing_challenge_progress");
  }

  [Fact]
  public async Task CheckClassroomAsync_ReportsMissingMasteryForAdaptiveTelemetry()
  {
    await using var context = CreateContext();
    var teacher = await AddUserAsync(context, "teacher-mastery");
    var student = await AddUserAsync(context, "student-mastery");
    var classroom = await AddClassroomAsync(context, teacher.Id);
    await AddClassroomStudentAsync(context, classroom.Id, student.Id);
    var game = await AddGameAsync(context);
    var module = await AddModuleAsync(context);
    var vocabulary = await AddVocabularyAsync(context, module.Id);
    var challenge = await AddChallengeAsync(context, game.Id, classroom.Id, module.Id);
    var challengeItem = await AddChallengeItemAsync(context, challenge.Id, vocabulary.Id);
    var adaptiveAttempt = await AddAdaptiveAttemptAsync(context, challenge.Id, student.Id);

    context.StudentChallengeItemAttempts.Add(new StudentChallengeItemAttempt
    {
      StudentChallengeAttemptId = adaptiveAttempt.Id,
      ChallengeItemId = challengeItem.Id,
      VocabularyItemId = vocabulary.Id,
      WasCorrect = false,
      FirstAttemptCorrect = false,
      RetriesCount = 1,
      HintsUsed = 0
    });
    await context.SaveChangesAsync();

    var service = new AttemptConsistencyService(context);

    var report = await service.CheckClassroomAsync(
      classroom.Id,
      teacher.Id,
      false,
      module.Id,
      student.Id,
      challenge.Id,
      CancellationToken.None);

    Assert.Contains(report.Issues, issue => issue.Code == "missing_word_mastery");
  }

  [Fact]
  public async Task CheckHealthAsync_SummarizesAttemptAndMasteryDivergence()
  {
    await using var context = CreateContext();
    var teacher = await AddUserAsync(context, "teacher-health");
    var student = await AddUserAsync(context, "student-health");
    var classroom = await AddClassroomAsync(context, teacher.Id);
    await AddClassroomStudentAsync(context, classroom.Id, student.Id);
    var game = await AddGameAsync(context);
    var module = await AddModuleAsync(context);
    var vocabulary = await AddVocabularyAsync(context, module.Id);
    var legacyOnlyChallenge = await AddChallengeAsync(context, game.Id, classroom.Id, module.Id);
    var adaptiveOnlyChallenge = await AddChallengeAsync(context, game.Id, classroom.Id, module.Id);
    var challengeItem = await AddChallengeItemAsync(context, adaptiveOnlyChallenge.Id, vocabulary.Id);
    var adaptiveAttempt = await AddAdaptiveAttemptAsync(context, adaptiveOnlyChallenge.Id, student.Id);

    context.Attempts.Add(new Attempt
    {
      UserId = student.Id,
      ChallengeId = legacyOnlyChallenge.Id,
      Score = 92,
      StarsEarned = 3,
      IsCompleted = true,
      CompletedAt = DateTime.UtcNow
    });
    context.StudentChallengeItemAttempts.Add(new StudentChallengeItemAttempt
    {
      StudentChallengeAttemptId = adaptiveAttempt.Id,
      ChallengeItemId = challengeItem.Id,
      VocabularyItemId = vocabulary.Id,
      WasCorrect = true,
      FirstAttemptCorrect = true,
      RetriesCount = 0,
      HintsUsed = 0
    });
    await context.SaveChangesAsync();

    var service = new AttemptConsistencyService(context);

    var health = await service.CheckHealthAsync(CancellationToken.None);

    Assert.Equal(1, health.MissingAdaptiveAttempts);
    Assert.Equal(1, health.MissingLegacyAttempts);
    Assert.Equal(0, health.MissingItemTelemetry);
    Assert.Equal(1, health.MissingWordMasteryUpdates);
    Assert.Equal("critical", health.Severity);
    Assert.Contains(student.Id, health.AffectedStudentIds);
    Assert.Contains(legacyOnlyChallenge.Id, health.AffectedChallengeIds);
    Assert.Contains(adaptiveOnlyChallenge.Id, health.AffectedChallengeIds);
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

  private static async Task<Classroom> AddClassroomAsync(ApplicationDbContext context, int teacherId)
  {
    var classroom = new Classroom
    {
      Name = "Consistency Class",
      Description = "Memory practice",
      Subject = "BM",
      YearLevel = 1,
      TeacherId = teacherId,
      JoinCode = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant(),
      IsActive = true
    };
    context.Classrooms.Add(classroom);
    await context.SaveChangesAsync();
    return classroom;
  }

  private static async Task AddClassroomStudentAsync(ApplicationDbContext context, int classroomId, int studentId)
  {
    context.ClassroomStudents.Add(new ClassroomStudent
    {
      ClassroomId = classroomId,
      UserId = studentId
    });
    await context.SaveChangesAsync();
  }

  private static async Task<Game> AddGameAsync(ApplicationDbContext context)
  {
    var game = new Game
    {
      Key = $"spell-catcher-{Guid.NewGuid():N}",
      Name = "Spell Catcher",
      Description = "Spell from memory",
      ImageUrl = "https://example.com/game.png",
      Category = "adaptive",
      SkillsTaught = "memory"
    };
    context.Games.Add(game);
    await context.SaveChangesAsync();
    return game;
  }

  private static async Task<SyllabusModule> AddModuleAsync(ApplicationDbContext context)
  {
    var module = new SyllabusModule
    {
      ModuleCode = Guid.NewGuid().ToString("N"),
      Subject = "BM",
      Language = "ms",
      YearLevel = 1,
      Term = "1",
      Title = "Words",
      Description = "Words",
      SourceType = "test",
      IsActive = true
    };
    context.SyllabusModules.Add(module);
    await context.SaveChangesAsync();
    return module;
  }

  private static async Task<VocabularyItem> AddVocabularyAsync(ApplicationDbContext context, int moduleId)
  {
    var item = new VocabularyItem
    {
      ModuleId = moduleId,
      Word = "sekolah",
      NormalizedWord = "sekolah",
      BmText = "sekolah",
      Language = "ms",
      Subject = "BM",
      YearLevel = 1,
      SyllablesJson = "[]",
      ItemType = "word",
      DisplayOrder = 1,
      DifficultyLevel = 1,
      IsActive = true
    };
    context.VocabularyItems.Add(item);
    await context.SaveChangesAsync();
    return item;
  }

  private static async Task<Challenge> AddChallengeAsync(
    ApplicationDbContext context,
    int gameId,
    int classroomId,
    int? moduleId = null)
  {
    var challenge = new Challenge
    {
      GameId = gameId,
      Title = "Spell school",
      Description = "Spell from memory",
      DifficultyLevel = 1,
      ContentData = "{}",
      OrderIndex = 1,
      MaxStars = 3,
      ClassroomId = classroomId,
      ModuleId = moduleId,
      Status = "assigned",
      LifecycleState = ChallengeLifecycleState.Active
    };
    context.Challenges.Add(challenge);
    await context.SaveChangesAsync();
    return challenge;
  }

  private static async Task<ChallengeItem> AddChallengeItemAsync(ApplicationDbContext context, int challengeId, int vocabularyItemId)
  {
    var item = new ChallengeItem
    {
      ChallengeId = challengeId,
      VocabularyItemId = vocabularyItemId,
      SequenceNo = 1,
      SettingsJson = "{}"
    };
    context.ChallengeItems.Add(item);
    await context.SaveChangesAsync();
    return item;
  }

  private static async Task<StudentChallengeAttempt> AddAdaptiveAttemptAsync(ApplicationDbContext context, int challengeId, int studentId)
  {
    var attempt = new StudentChallengeAttempt
    {
      ChallengeId = challengeId,
      StudentId = studentId,
      AttemptNo = 1,
      CompletionStatus = "completed",
      CompletedAt = DateTime.UtcNow
    };
    context.StudentChallengeAttempts.Add(attempt);
    await context.SaveChangesAsync();
    return attempt;
  }
}
