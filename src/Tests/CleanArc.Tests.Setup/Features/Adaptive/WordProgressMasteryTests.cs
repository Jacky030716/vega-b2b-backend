using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.Quiz;
using CleanArc.Domain.Entities.User;
using CleanArc.Infrastructure.Persistence;
using CleanArc.Infrastructure.Persistence.Services.Adaptive;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CleanArc.Tests.Setup.Features.Adaptive;

public class WordProgressMasteryTests
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
    public void CalculateDelta_CorrectFastAnswer_AwardsSpeedBonus()
    {
        var request = new SubmitAdaptiveItemAttemptRequest(
            StudentChallengeAttemptId: 1,
            ChallengeItemId: 1,
            VocabularyItemId: 1,
            GameTemplateId: 1,
            PresentedAt: DateTime.UtcNow,
            AnsweredAt: DateTime.UtcNow,
            ResponseTimeMs: 4000, // < 5000ms
            WasCorrect: true,
            FirstAttemptCorrect: true,
            RetriesCount: 0,
            HintsUsed: 0,
            AnswerText: "test",
            ExpectedAnswerText: "test",
            SpeechConfidence: null,
            ErrorType: null,
            RawTelemetryJson: null
        );

        var delta = MasteryEngine.CalculateDelta(request, isEarlyPractice: false);
        // Correct (8) + FirstTryCorrect (4) + Speed (2) = 14
        Assert.Equal(14, delta);
    }

    [Fact]
    public void CalculateDelta_CorrectSlowAnswer_DoesNotAwardSpeedBonus()
    {
        var request = new SubmitAdaptiveItemAttemptRequest(
            StudentChallengeAttemptId: 1,
            ChallengeItemId: 1,
            VocabularyItemId: 1,
            GameTemplateId: 1,
            PresentedAt: DateTime.UtcNow,
            AnsweredAt: DateTime.UtcNow,
            ResponseTimeMs: 16000, // > 15000ms (Previously awarded +5)
            WasCorrect: true,
            FirstAttemptCorrect: true,
            RetriesCount: 0,
            HintsUsed: 0,
            AnswerText: "test",
            ExpectedAnswerText: "test",
            SpeechConfidence: null,
            ErrorType: null,
            RawTelemetryJson: null
        );

        var delta = MasteryEngine.CalculateDelta(request, isEarlyPractice: false);
        // Correct (8) + FirstTryCorrect (4) = 12
        Assert.Equal(12, delta);
    }

    [Fact]
    public void CalculateDelta_EarlyPracticeCorrectAnswer_AwardsNoScoreGain()
    {
        var request = new SubmitAdaptiveItemAttemptRequest(
            StudentChallengeAttemptId: 1,
            ChallengeItemId: 1,
            VocabularyItemId: 1,
            GameTemplateId: 1,
            PresentedAt: DateTime.UtcNow,
            AnsweredAt: DateTime.UtcNow,
            ResponseTimeMs: 4000,
            WasCorrect: true,
            FirstAttemptCorrect: true,
            RetriesCount: 0,
            HintsUsed: 0,
            AnswerText: "test",
            ExpectedAnswerText: "test",
            SpeechConfidence: null,
            ErrorType: null,
            RawTelemetryJson: null
        );

        var delta = MasteryEngine.CalculateDelta(request, isEarlyPractice: true);
        Assert.Equal(0, delta);
    }

    [Fact]
    public void CalculateDelta_EarlyPracticeIncorrectAnswer_StillAppliesPenalty()
    {
        var request = new SubmitAdaptiveItemAttemptRequest(
            StudentChallengeAttemptId: 1,
            ChallengeItemId: 1,
            VocabularyItemId: 1,
            GameTemplateId: 1,
            PresentedAt: DateTime.UtcNow,
            AnsweredAt: DateTime.UtcNow,
            ResponseTimeMs: 4000,
            WasCorrect: false,
            FirstAttemptCorrect: false,
            RetriesCount: 0,
            HintsUsed: 0,
            AnswerText: "wrong",
            ExpectedAnswerText: "test",
            SpeechConfidence: null,
            ErrorType: null,
            RawTelemetryJson: null
        );

        var delta = MasteryEngine.CalculateDelta(request, isEarlyPractice: true);
        // Base penalty is -8
        Assert.Equal(-8, delta);
    }

    [Fact]
    public async Task CompleteAsync_CorrectlyRecordsWordProgressCache()
    {
        await using var context = CreateContext();
        
        // Seed users
        var student = new User { UserName = "test-student", Experience = 1 };
        context.Users.Add(student);
        
        var teacher = new User { UserName = "test-teacher", Experience = 1 };
        context.Users.Add(teacher);
        await context.SaveChangesAsync();

        // Seed classrooms and games
        var classroom = new Classroom
        {
            Name = "Class 1",
            JoinCode = "C123",
            Subject = "BM",
            TeacherId = teacher.Id
        };
        context.Classrooms.Add(classroom);

        var game = new Game
        {
            Key = "spell-catcher",
            Name = "Spell Catcher",
            Category = "spelling",
            SkillsTaught = "BM"
        };
        context.Games.Add(game);
        await context.SaveChangesAsync();

        // Seed modules, vocabularies, and challenges
        var module = new SyllabusModule
        {
            ModuleCode = "M100",
            Subject = "BM",
            Language = "ms",
            Title = "Module 1",
            SourceType = "predefined"
        };
        context.SyllabusModules.Add(module);
        await context.SaveChangesAsync();

        var vocabulary = new VocabularyItem
        {
            ModuleId = module.Id,
            Word = "sekolah",
            NormalizedWord = "sekolah",
            BmText = "sekolah",
            Language = "ms",
            Subject = "BM"
        };
        context.VocabularyItems.Add(vocabulary);
        await context.SaveChangesAsync();

        var challenge = new Challenge
        {
            ClassroomId = classroom.Id,
            GameId = game.Id,
            Title = "Challenge 1",
            ContentData = "{}",
            LifecycleState = ChallengeLifecycleState.Active,
            Status = "assigned"
        };
        context.Challenges.Add(challenge);
        await context.SaveChangesAsync();

        var challengeItem = new ChallengeItem
        {
            ChallengeId = challenge.Id,
            VocabularyItemId = vocabulary.Id,
            SequenceNo = 1
        };
        context.ChallengeItems.Add(challengeItem);
        await context.SaveChangesAsync();

        // Add attempt
        var attempt = new StudentChallengeAttempt
        {
            ChallengeId = challenge.Id,
            StudentId = student.Id,
            AttemptNo = 1,
            CompletionStatus = "started",
            StartedAt = DateTime.UtcNow
        };
        context.StudentChallengeAttempts.Add(attempt);
        await context.SaveChangesAsync();

        // Add item attempts
        context.StudentChallengeItemAttempts.Add(new StudentChallengeItemAttempt
        {
            StudentChallengeAttemptId = attempt.Id,
            ChallengeItemId = challengeItem.Id,
            VocabularyItemId = vocabulary.Id,
            WasCorrect = true,
            FirstAttemptCorrect = true,
            ResponseTimeMs = 3000,
            HintsUsed = 0,
            RetriesCount = 0
        });
        await context.SaveChangesAsync();

        var masteryEngine = new MasteryEngine(context);
        var service = new AdaptiveAttemptService(context, masteryEngine);

        // Execute CompleteAsync
        var request = new CompleteAdaptiveAttemptRequest(attempt.Id, 100, "completed");
        await service.CompleteAsync(request, CancellationToken.None);

        // Verify WordProgress was created and cached
        var progress = await context.WordProgresses.FirstOrDefaultAsync(wp => wp.StudentId == student.Id && wp.WordId == vocabulary.Id);
        
        Assert.NotNull(progress);
        Assert.Equal(1, progress.TotalAttempts);
        Assert.Equal(1, progress.TotalCorrect);
        // Under new formula: Mastery = Accuracy*0.5 + Consistency*0.3 + Retention*0.2
        // For 1 correct attempt: Accuracy=100, Consistency=100 (streak 1/1), Retention=100 (just completed)
        // Mastery = 100*0.5 + 100*0.3 + 100*0.2 = 100
        Assert.Equal(100, progress.MasteryScore);
        Assert.NotNull(progress.LastPracticedAt);
        Assert.NotNull(progress.NextReviewDate);
    }

    [Fact]
    public void GetDecayedMasteryScore_AppliesDecayCorrectly()
    {
        var now = DateTime.UtcNow;

        // 0 days elapsed
        Assert.Equal(95, MasteryEngine.GetDecayedMasteryScore(95, now));

        // 7 days elapsed
        Assert.Equal(90, MasteryEngine.GetDecayedMasteryScore(95, now.AddDays(-7)));

        // 14 days elapsed
        Assert.Equal(85, MasteryEngine.GetDecayedMasteryScore(95, now.AddDays(-14)));

        // 30 days elapsed
        Assert.Equal(75, MasteryEngine.GetDecayedMasteryScore(95, now.AddDays(-30)));

        // Clamp at 0
        Assert.Equal(0, MasteryEngine.GetDecayedMasteryScore(15, now.AddDays(-30)));
    }

    [Fact]
    public async Task CompleteAsync_IncorrectAnswer_SetsReviewDateToOneDay()
    {
        await using var context = CreateContext();
        var student = new User { UserName = "student-1", Experience = 1 };
        context.Users.Add(student);
        var teacher = new User { UserName = "teacher-1", Experience = 1 };
        context.Users.Add(teacher);
        await context.SaveChangesAsync();

        var classroom = new Classroom { Name = "Class 1", JoinCode = "C123", Subject = "BM", TeacherId = teacher.Id };
        context.Classrooms.Add(classroom);
        var game = new Game { Key = "spell-catcher", Name = "Spell Catcher", Category = "spelling", SkillsTaught = "BM" };
        context.Games.Add(game);
        var module = new SyllabusModule { ModuleCode = "M100", Subject = "BM", Language = "ms", Title = "Module 1", SourceType = "predefined" };
        context.SyllabusModules.Add(module);
        await context.SaveChangesAsync();

        var vocabulary = new VocabularyItem { ModuleId = module.Id, Word = "sekolah", NormalizedWord = "sekolah", BmText = "sekolah", Language = "ms", Subject = "BM" };
        context.VocabularyItems.Add(vocabulary);
        var challenge = new Challenge { ClassroomId = classroom.Id, GameId = game.Id, Title = "Challenge 1", ContentData = "{}", LifecycleState = ChallengeLifecycleState.Active, Status = "assigned" };
        context.Challenges.Add(challenge);
        await context.SaveChangesAsync();

        var challengeItem = new ChallengeItem { ChallengeId = challenge.Id, VocabularyItemId = vocabulary.Id, SequenceNo = 1 };
        context.ChallengeItems.Add(challengeItem);
        await context.SaveChangesAsync();

        var attempt = new StudentChallengeAttempt { ChallengeId = challenge.Id, StudentId = student.Id, AttemptNo = 1, CompletionStatus = "started", StartedAt = DateTime.UtcNow };
        context.StudentChallengeAttempts.Add(attempt);
        await context.SaveChangesAsync();

        context.StudentChallengeItemAttempts.Add(new StudentChallengeItemAttempt
        {
            StudentChallengeAttemptId = attempt.Id,
            ChallengeItemId = challengeItem.Id,
            VocabularyItemId = vocabulary.Id,
            WasCorrect = false,
            FirstAttemptCorrect = false,
            ResponseTimeMs = 3000,
            HintsUsed = 0,
            RetriesCount = 0
        });
        await context.SaveChangesAsync();

        var masteryEngine = new MasteryEngine(context);
        var service = new AdaptiveAttemptService(context, masteryEngine);

        var request = new CompleteAdaptiveAttemptRequest(attempt.Id, 0, "completed");
        await service.CompleteAsync(request, CancellationToken.None);

        var progress = await context.WordProgresses.FirstOrDefaultAsync(wp => wp.StudentId == student.Id && wp.WordId == vocabulary.Id);
        Assert.NotNull(progress);
        Assert.Equal(0, progress.TotalCorrect);
        // MasteryScore = 0*0.5 + 0*0.3 + 100*0.2 = 20
        Assert.Equal(20, progress.MasteryScore);
        Assert.NotNull(progress.NextReviewDate);
        var diff = progress.NextReviewDate.Value - DateTime.UtcNow;
        Assert.True(diff.TotalHours >= 23 && diff.TotalHours <= 25);
    }

    [Fact]
    public async Task GenerateAsync_PrioritizesWordsCorrectly()
    {
        await using var context = CreateContext();
        var teacher = new User { UserName = "test-teacher", Experience = 1 };
        context.Users.Add(teacher);
        var student = new User { UserName = "test-student", Experience = 1 };
        context.Users.Add(student);
        await context.SaveChangesAsync();

        var classroom = new Classroom { Name = "Class 1", JoinCode = "C123", Subject = "BM", TeacherId = teacher.Id };
        context.Classrooms.Add(classroom);
        var game = new Game { Key = "spell-catcher", Name = "Spell Catcher", Category = "spelling", SkillsTaught = "BM" };
        context.Games.Add(game);
        var module = new SyllabusModule { ModuleCode = "M100", Subject = "BM", Language = "ms", Title = "Module 1", SourceType = "predefined" };
        context.SyllabusModules.Add(module);
        await context.SaveChangesAsync();

        var v1 = new VocabularyItem { ModuleId = module.Id, Word = "word1", NormalizedWord = "word1", Language = "ms", Subject = "BM", DisplayOrder = 1 };
        var v2 = new VocabularyItem { ModuleId = module.Id, Word = "word2", NormalizedWord = "word2", Language = "ms", Subject = "BM", DisplayOrder = 2 };
        var v3 = new VocabularyItem { ModuleId = module.Id, Word = "word3", NormalizedWord = "word3", Language = "ms", Subject = "BM", DisplayOrder = 3 };
        var v4 = new VocabularyItem { ModuleId = module.Id, Word = "word4", NormalizedWord = "word4", Language = "ms", Subject = "BM", DisplayOrder = 4 };
        var v5 = new VocabularyItem { ModuleId = module.Id, Word = "word5", NormalizedWord = "word5", Language = "ms", Subject = "BM", DisplayOrder = 5 };

        context.VocabularyItems.AddRange(v1, v2, v3, v4, v5);
        await context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var wp1 = new WordProgress { StudentId = student.Id, WordId = v1.Id, TotalAttempts = 2, TotalCorrect = 2, MasteryScore = 90, LastPracticedAt = now.AddDays(-8), NextReviewDate = now.AddDays(-1) };
        var wp2 = new WordProgress { StudentId = student.Id, WordId = v2.Id, TotalAttempts = 2, TotalCorrect = 1, MasteryScore = 40, LastPracticedAt = now, NextReviewDate = now.AddDays(1) };
        var wp3 = new WordProgress { StudentId = student.Id, WordId = v3.Id, TotalAttempts = 2, TotalCorrect = 1, MasteryScore = 65, LastPracticedAt = now, NextReviewDate = now.AddDays(3) };
        var wp5 = new WordProgress { StudentId = student.Id, WordId = v5.Id, TotalAttempts = 2, TotalCorrect = 2, MasteryScore = 90, LastPracticedAt = now, NextReviewDate = now.AddDays(7) };

        context.WordProgresses.AddRange(wp1, wp2, wp3, wp5);
        await context.SaveChangesAsync();

        var generator = new ChallengeGenerator(context);
        var request = new GenerateAdaptiveChallengeRequest(
            TargetType: "class",
            StudentId: student.Id,
            ClassId: classroom.Id,
            Objective: "learn_vocabulary",
            SourceType: "predefined_module",
            ModuleId: module.Id,
            PreferredGameTemplateCode: "SPELL_CATCHER",
            LearningFocus: null,
            ManualWords: null,
            AiPrompt: null,
            SourceText: null
        );

        var preview = await generator.GenerateAsync(request, CancellationToken.None);
        Assert.NotNull(preview);
        
        var order = preview.Items.Select(i => i.Word).ToList();
        Assert.Equal(5, order.Count);
        Assert.Equal("word1", order[0]); // Overdue
        Assert.Equal("word2", order[1]); // Weak
        Assert.Equal("word3", order[2]); // Developing
        Assert.Equal("word4", order[3]); // New
        Assert.Equal("word5", order[4]); // Mastered
    }
}
