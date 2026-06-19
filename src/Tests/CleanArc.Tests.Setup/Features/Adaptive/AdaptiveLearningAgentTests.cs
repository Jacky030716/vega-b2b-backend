using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CleanArc.Application.Contracts.AdaptiveLearning;
using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Domain.Entities.Quiz;
using CleanArc.Domain.Entities.User;
using CleanArc.Infrastructure.Persistence;
using CleanArc.Infrastructure.Persistence.Services.Adaptive.AdaptiveLearning;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CleanArc.Tests.Setup.Features.Adaptive;

public class AdaptiveLearningAgentTests
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

    private class MockChallengeGenerator : IChallengeGenerator
    {
        public MockChallengeGenerator(string gameType)
        {
            GameType = gameType;
        }

        public string GameType { get; }

        public Task<string> GenerateContentJsonAsync(IReadOnlyList<string> words, int difficulty, CancellationToken cancellationToken)
        {
            return Task.FromResult(JsonSerializer.Serialize(new { gameType = GameType, words = words, difficulty = difficulty }));
        }
    }

    private static async Task<Game> AddGameAsync(ApplicationDbContext context)
    {
        var game = new Game
        {
            Key = "spell-catcher",
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

    [Fact]
    public async Task EvaluateAndTriggerDraftAsync_NotEnoughAttempts_FailsEligibility()
    {
        // Arrange
        using var dbContext = CreateContext();
        var logger = NullLogger<AdaptiveLearningAgent>.Instance;
        var generators = new List<IChallengeGenerator> { new MockChallengeGenerator("VOICE_BRIDGE") };
        var agent = new AdaptiveLearningAgent(dbContext, generators, logger);

        // Seed student
        var student = new User { Name = "Normal Student", UserName = "normalstudent" };
        dbContext.Users.Add(student);
        await dbContext.SaveChangesAsync();

        int studentId = student.Id;

        // Seed game & challenge
        var game = await AddGameAsync(dbContext);
        var challenge = new Challenge
        {
            GameId = game.Id,
            Title = "Base Challenge",
            ContentData = JsonSerializer.Serialize(new { items = new[] { new { word = "apple" } } })
        };
        dbContext.Challenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        // Seed only 1 attempt (minimum 3 needed)
        var attempt = new Attempt
        {
            UserId = studentId,
            ChallengeId = challenge.Id,
            IsCompleted = true,
            Score = 100,
            CompletedAt = DateTime.UtcNow.AddMinutes(-10),
            AttemptData = JsonSerializer.Serialize(new { durationSeconds = 5.0, results = new[] { new { hintsUsed = 0, retriesCount = 0 } } })
        };
        dbContext.Attempts.Add(attempt);
        await dbContext.SaveChangesAsync();

        // Act
        await agent.EvaluateAndTriggerDraftAsync(studentId, attempt.Id, isSpellingTest: false, CancellationToken.None);

        // Assert
        var decision = await dbContext.AdaptiveAgentDecisions.FirstOrDefaultAsync(d => d.StudentId == studentId);
        Assert.NotNull(decision);
        Assert.False(decision.IsEligible);
        Assert.Contains("minimum 3 required", decision.DecisionReason);
    }

    [Fact]
    public async Task EvaluateAndTriggerDraftAsync_ConsistentHighPerformance_CreatesHardcoreChallengeDraft()
    {
        // Arrange
        using var dbContext = CreateContext();
        var logger = NullLogger<AdaptiveLearningAgent>.Instance;
        var generators = new List<IChallengeGenerator>
        {
            new MockChallengeGenerator("VOICE_BRIDGE"),
            new MockChallengeGenerator("SYLLABLE_SUSHI"),
            new MockChallengeGenerator("SPELL_CATCHER"),
            new MockChallengeGenerator("SPELLING_TEST")
        };
        var agent = new AdaptiveLearningAgent(dbContext, generators, logger);

        // Seed student user
        var student = new User { Name = "Excellent Student", UserName = "excellent" };
        dbContext.Users.Add(student);
        await dbContext.SaveChangesAsync();

        int studentId = student.Id;

        // Seed game & challenge
        var game = await AddGameAsync(dbContext);
        var challenge = new Challenge
        {
            GameId = game.Id,
            Title = "Base Challenge",
            ContentData = JsonSerializer.Serialize(new { items = new[] { new { word = "apple" }, new { word = "banana" } } })
        };
        dbContext.Challenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        // Seed 3 perfect and fast attempts
        Attempt triggeringAttempt = null;
        for (int i = 1; i <= 3; i++)
        {
            var attempt = new Attempt
            {
                UserId = studentId,
                ChallengeId = challenge.Id,
                IsCompleted = true,
                Score = 95,
                CompletedAt = DateTime.UtcNow.AddMinutes(-i),
                // 2 words, duration is 4s (faster than 2 * 6.0 = 12s)
                AttemptData = JsonSerializer.Serialize(new
                {
                    durationSeconds = 4.0,
                    results = new[]
                    {
                        new { hintsUsed = 0, retriesCount = 0 },
                        new { hintsUsed = 0, retriesCount = 0 }
                    }
                })
            };
            dbContext.Attempts.Add(attempt);
            if (i == 1)
            {
                triggeringAttempt = attempt;
            }
        }
        await dbContext.SaveChangesAsync();

        // Act
        await agent.EvaluateAndTriggerDraftAsync(studentId, triggeringAttempt.Id, isSpellingTest: false, CancellationToken.None);

        // Assert
        var decision = await dbContext.AdaptiveAgentDecisions.FirstOrDefaultAsync(d => d.StudentId == studentId && d.IsEligible);
        Assert.NotNull(decision);
        Assert.True(decision.IsEligible);
        Assert.NotNull(decision.GeneratedDraftId);

        var draft = await dbContext.HardcoreChallengeDrafts.FirstOrDefaultAsync(d => d.Id == decision.GeneratedDraftId);
        Assert.NotNull(draft);
        Assert.Equal("PENDING", draft.Status);
        Assert.Equal(5, draft.DifficultyLevel);
        Assert.Contains("apple", draft.TargetWordsJson);
        Assert.Contains("banana", draft.TargetWordsJson);
    }
}
