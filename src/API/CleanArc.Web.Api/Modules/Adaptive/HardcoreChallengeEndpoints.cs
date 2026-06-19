using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Carter;
using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Domain.Entities.Quiz;
using CleanArc.Infrastructure.Persistence;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Web.Api.Endpoints;

public class HardcoreChallengeEndpoints : ICarterModule
{
    private const double Version = 1.1;
    private const string Tag = "Hardcore Challenges";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        // GET pending hardcore challenges for student
        app.MapEndpoint(builder => builder.MapGet(
            "/api/v{version:apiVersion}/student/hardcore-challenges/pending",
            async (
                ClaimsPrincipal user,
                ApplicationDbContext dbContext,
                CancellationToken cancellationToken) =>
            {
                var studentId = int.Parse(user.Identity.GetUserId());
                var now = DateTime.UtcNow;

                // Mark expired drafts
                var expiredDrafts = await dbContext.HardcoreChallengeDrafts
                    .Where(d => d.StudentId == studentId && d.Status == "PENDING" && d.ExpiryAt < now)
                    .ToListAsync(cancellationToken);

                if (expiredDrafts.Count > 0)
                {
                    foreach (var d in expiredDrafts)
                    {
                        d.Status = "EXPIRED";
                    }
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                var pendingDrafts = await dbContext.HardcoreChallengeDrafts
                    .AsNoTracking()
                    .Where(d => d.StudentId == studentId && d.Status == "PENDING" && d.ExpiryAt >= now)
                    .ToListAsync(cancellationToken);

                var pending = pendingDrafts.Select(d => new
                {
                    d.Id,
                    d.Title,
                    d.Description,
                    d.GameType,
                    d.DifficultyLevel,
                    TargetWords = JsonSerializer.Deserialize<List<string>>(d.TargetWordsJson ?? "[]") ?? new List<string>(),
                    d.RewardXp,
                    d.RewardDiamonds,
                    d.MascotEligibility,
                    d.MascotName,
                    d.BadgeCode,
                    d.ExpiryAt,
                    d.DecisionReason
                }).ToList();

                return Results.Ok(pending);
            }), Version, "GetPendingHardcoreChallenges", Tag)
            .RequireAuthorization();

        // POST accept hardcore challenge draft
        app.MapEndpoint(builder => builder.MapPost(
            "/api/v{version:apiVersion}/student/hardcore-challenges/{draftId:int}/accept",
            async (
                int draftId,
                ClaimsPrincipal user,
                ApplicationDbContext dbContext,
                CancellationToken cancellationToken) =>
            {
                var studentId = int.Parse(user.Identity.GetUserId());
                var now = DateTime.UtcNow;

                var draft = await dbContext.HardcoreChallengeDrafts
                    .FirstOrDefaultAsync(d => d.Id == draftId && d.StudentId == studentId, cancellationToken);

                if (draft == null)
                    return Results.NotFound(new { message = "Hardcore challenge draft not found." });

                if (draft.Status != "PENDING")
                    return Results.BadRequest(new { message = $"Draft is already in status: {draft.Status}" });

                if (draft.ExpiryAt < now)
                {
                    draft.Status = "EXPIRED";
                    await dbContext.SaveChangesAsync(cancellationToken);
                    return Results.BadRequest(new { message = "This challenge draft has expired." });
                }

                // Get student's classroom
                var studentClass = await dbContext.ClassroomStudents
                    .AsNoTracking()
                    .FirstOrDefaultAsync(cs => cs.UserId == studentId, cancellationToken);
                int classroomId = studentClass?.ClassroomId ?? 0;

                if (draft.GameType.Equals("SPELLING_TEST", StringComparison.OrdinalIgnoreCase))
                {
                    // Create a Spelling Test
                    var targetWords = JsonSerializer.Deserialize<List<string>>(draft.TargetWordsJson ?? "[]") ?? new List<string>();
                    var vocabIds = await dbContext.VocabularyItems
                        .AsNoTracking()
                        .Where(v => targetWords.Contains(v.Word) && v.IsActive)
                        .Select(v => v.Id)
                        .ToListAsync(cancellationToken);

                    var spellingTest = new SpellingTest
                    {
                        ClassroomId = classroomId,
                        Subject = "English",
                        Title = draft.Title,
                        Description = draft.Description,
                        SourceModuleIdsJson = "[]",
                        WordItemIdsJson = JsonSerializer.Serialize(vocabIds),
                        DueAt = draft.ExpiryAt,
                        Status = SpellingTestStatuses.Active,
                        CreatedByTeacherId = 1, // System default teacher/admin ID
                        ConfigJson = JsonSerializer.Serialize(new
                        {
                            timeLimitSeconds = 300,
                            allowRetries = false,
                            difficulty = 5,
                            gameType = "SPELL_CATCHER"
                        })
                    };

                    dbContext.SpellingTests.Add(spellingTest);
                    await dbContext.SaveChangesAsync(cancellationToken);

                    var attempt = new StudentSpellingTestAttempt
                    {
                        SpellingTestId = spellingTest.Id,
                        StudentId = studentId,
                        Status = StudentSpellingTestAttemptStatuses.NotStarted,
                        RemainingSeconds = 300,
                        ResultJson = "{}"
                    };

                    dbContext.StudentSpellingTestAttempts.Add(attempt);
                    draft.LinkedSpellingTestId = spellingTest.Id;
                }
                else
                {
                    // Create a standard Quiz Challenge
                    var gameKey = draft.GameType.ToLowerInvariant();
                    var game = await dbContext.Games
                        .AsNoTracking()
                        .FirstOrDefaultAsync(g => g.Key == gameKey, cancellationToken);
                    int gameId = game?.Id ?? 1;

                    var challenge = new Challenge
                    {
                        GameId = gameId,
                        Title = draft.Title,
                        Description = draft.Description,
                        DifficultyLevel = 5,
                        ContentData = draft.ContentData,
                        StudentId = studentId,
                        ClassroomId = classroomId > 0 ? classroomId : null,
                        ChallengeMode = "HARDCORE_CHALLENGE",
                        SourceType = "HARDCORE_DRAFT",
                        Status = "assigned",
                        AssignedAt = DateTime.UtcNow,
                        DueAt = draft.ExpiryAt,
                        LifecycleState = ChallengeLifecycleState.Active
                    };

                    dbContext.Challenges.Add(challenge);
                    await dbContext.SaveChangesAsync(cancellationToken);

                    draft.LinkedChallengeId = challenge.Id;
                }

                draft.Status = "ACCEPTED";
                await dbContext.SaveChangesAsync(cancellationToken);

                return Results.Ok(new
                {
                    message = "Hardcore challenge accepted successfully.",
                    draftId = draft.Id,
                    gameType = draft.GameType,
                    challengeId = draft.LinkedChallengeId,
                    spellingTestId = draft.LinkedSpellingTestId
                });
            }), Version, "AcceptHardcoreChallenge", Tag)
            .RequireAuthorization();

        // POST decline hardcore challenge draft
        app.MapEndpoint(builder => builder.MapPost(
            "/api/v{version:apiVersion}/student/hardcore-challenges/{draftId:int}/decline",
            async (
                int draftId,
                ClaimsPrincipal user,
                ApplicationDbContext dbContext,
                CancellationToken cancellationToken) =>
            {
                var studentId = int.Parse(user.Identity.GetUserId());
                var now = DateTime.UtcNow;

                var draft = await dbContext.HardcoreChallengeDrafts
                    .FirstOrDefaultAsync(d => d.Id == draftId && d.StudentId == studentId, cancellationToken);

                if (draft == null)
                    return Results.NotFound(new { message = "Hardcore challenge draft not found." });

                if (draft.Status != "PENDING")
                    return Results.BadRequest(new { message = $"Draft is already in status: {draft.Status}" });

                draft.Status = "DECLINED";
                await dbContext.SaveChangesAsync(cancellationToken);

                return Results.Ok(new { message = "Hardcore challenge draft declined.", draftId = draft.Id });
            }), Version, "DeclineHardcoreChallenge", Tag)
            .RequireAuthorization();
    }
}
