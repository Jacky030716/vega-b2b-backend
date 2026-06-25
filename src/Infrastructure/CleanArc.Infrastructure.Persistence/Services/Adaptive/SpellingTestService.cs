using System.Text.Json;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Domain.Entities.Classroom;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanArc.Infrastructure.Persistence.Services.Adaptive;

public class SpellingTestService(
    ApplicationDbContext dbContext,
    ILogger<SpellingTestService> logger,
    CleanArc.Application.Contracts.AdaptiveLearning.IAdaptiveLearningAgent adaptiveLearningAgent) : ISpellingTestService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan CompletedVisibilityWindow = TimeSpan.FromHours(24);
    private const int DefaultTimeLimitSeconds = 180;

    public async Task<SpellingTestSummaryDto> CreateAsync(
        int classroomId,
        CreateSpellingTestRequest request,
        int teacherId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Creating spelling test for classroom {ClassroomId} by teacher {TeacherId}. Subject={Subject}; Title={Title}; ModuleIds={ModuleIds}; DueAt={DueAt}; WordCount={WordCount}; GameType={GameType}",
            classroomId,
            teacherId,
            request.Subject,
            request.Title,
            string.Join(",", request.ModuleIds ?? Array.Empty<int>()),
            request.DueAt,
            request.Config?.WordCount,
            request.Config?.GameType);

        var classroom = await GetManagedClassroomAsync(classroomId, teacherId, isAdmin, cancellationToken);
        var title = request.Title?.Trim() ?? string.Empty;
        var subject = request.Subject?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title))
            throw CreateSpellingTestFailure("Spelling test title is required.", classroomId, teacherId, request, null);
        if (string.IsNullOrWhiteSpace(subject))
            throw CreateSpellingTestFailure("Subject is required.", classroomId, teacherId, request, null);
        if (request.DueAt <= DateTime.UtcNow)
            throw CreateSpellingTestFailure("Due date must be in the future.", classroomId, teacherId, request, null);

        var moduleIds = request.ModuleIds?.Where(id => id > 0).Distinct().ToList() ?? new List<int>();
        if (moduleIds.Count == 0)
            throw CreateSpellingTestFailure("Select at least one module.", classroomId, teacherId, request, moduleIds);

        await EnsureClassroomModuleLinksAsync(classroom, cancellationToken);

        await EnsureModulesAttachedAsync(classroomId, moduleIds, cancellationToken);
        var modules = await dbContext.SyllabusModules.AsNoTracking()
            .Where(module => moduleIds.Contains(module.Id) && module.IsActive)
            .ToListAsync(cancellationToken);
        if (modules.Count != moduleIds.Count)
            throw CreateSpellingTestFailure("One or more selected modules were not found.", classroomId, teacherId, request, moduleIds);
        if (modules.Any(module => !string.Equals(module.Subject, subject, StringComparison.OrdinalIgnoreCase)))
            throw CreateSpellingTestFailure(
                "Selected modules must match the selected subject.",
                classroomId,
                teacherId,
                request,
                moduleIds,
                modules.Select(module => $"{module.Id}:{module.Subject}:Y{module.YearLevel}").ToList());
        if (modules.Any(module => module.YearLevel != classroom.YearLevel))
            throw CreateSpellingTestFailure(
                "Selected modules must match the classroom year level.",
                classroomId,
                teacherId,
                request,
                moduleIds,
                modules.Select(module => $"{module.Id}:{module.Subject}:Y{module.YearLevel}").ToList());

        var config = NormalizeConfig(request.Config);
        var words = await SelectWordsAsync(classroomId, moduleIds, config, title, cancellationToken);
        if (words.Count == 0)
            throw CreateSpellingTestFailure("Selected modules do not contain vocabulary for a spelling test.", classroomId, teacherId, request, moduleIds);

        var test = new SpellingTest
        {
            ClassroomId = classroomId,
            Subject = subject,
            Title = title,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            SourceModuleIdsJson = Serialize(moduleIds),
            WordItemIdsJson = Serialize(words.Select(word => word.Id).ToList()),
            DueAt = DateTime.SpecifyKind(request.DueAt, DateTimeKind.Utc),
            Status = SpellingTestStatuses.Active,
            CreatedByTeacherId = teacherId,
            ConfigJson = Serialize(config)
        };

        var studentIds = await dbContext.ClassroomStudents.AsNoTracking()
            .Where(student => student.ClassroomId == classroomId)
            .Select(student => student.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var studentId in studentIds)
        {
            test.StudentAttempts.Add(new StudentSpellingTestAttempt
            {
                StudentId = studentId,
                Status = StudentSpellingTestAttemptStatuses.NotStarted
            });
        }

        dbContext.SpellingTests.Add(test);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Created spelling test {SpellingTestId} for classroom {ClassroomId}. WordCount={WordCount}; AssignedCount={AssignedCount}",
            test.Id,
            classroomId,
            words.Count,
            studentIds.Count);
        return await BuildTeacherSummaryAsync(test.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<SpellingTestSummaryDto>> GetForTeacherAsync(
        int classroomId,
        int teacherId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        await GetManagedClassroomAsync(classroomId, teacherId, isAdmin, cancellationToken);
        var ids = await dbContext.SpellingTests.AsNoTracking()
            .Where(test => test.ClassroomId == classroomId && test.Status != SpellingTestStatuses.Archived)
            .OrderByDescending(test => test.CreatedTime)
            .Select(test => test.Id)
            .ToListAsync(cancellationToken);

        var result = new List<SpellingTestSummaryDto>();
        foreach (var id in ids)
            result.Add(await BuildTeacherSummaryAsync(id, cancellationToken));
        return result;
    }

    public async Task<SpellingTestSummaryDto> GetTeacherDetailAsync(int testId, int teacherId, bool isAdmin, CancellationToken cancellationToken)
    {
        var test = await GetTeacherTestAsync(testId, teacherId, isAdmin, cancellationToken);
        return await BuildTeacherSummaryAsync(test.Id, cancellationToken);
    }

    public async Task<SpellingTestResultsDto> GetTeacherResultsAsync(int testId, int teacherId, bool isAdmin, CancellationToken cancellationToken)
    {
        var test = await GetTeacherTestAsync(testId, teacherId, isAdmin, cancellationToken);
        var now = DateTime.UtcNow;
        var attempts = await dbContext.StudentSpellingTestAttempts.AsNoTracking()
            .Where(attempt => attempt.SpellingTestId == testId)
            .ToListAsync(cancellationToken);
        var studentIds = attempts.Select(attempt => attempt.StudentId).Distinct().ToList();
        var students = await dbContext.Users.AsNoTracking()
            .Where(user => studentIds.Contains(user.Id))
            .Select(user => new { user.Id, user.Name, user.FamilyName, user.UserName })
            .ToListAsync(cancellationToken);
        var studentNames = students.ToDictionary(
            user => user.Id,
            user =>
            {
                var fullName = string.Join(" ", new[] { user.Name, user.FamilyName }
                    .Where(part => !string.IsNullOrWhiteSpace(part))).Trim();
                return string.IsNullOrWhiteSpace(fullName) ? user.UserName ?? $"Student {user.Id}" : fullName;
            });

        var rows = attempts
            .Select(attempt =>
            {
                studentNames.TryGetValue(attempt.StudentId, out var studentName);
                return new SpellingTestStudentResultRowDto(
                    attempt.StudentId,
                    string.IsNullOrWhiteSpace(studentName) ? $"Student {attempt.StudentId}" : studentName,
                    ResolveStudentStatus(attempt, test.DueAt, now),
                    attempt.Score,
                    attempt.Stars,
                    attempt.StartedAt,
                    attempt.CompletedAt);
            })
            .OrderBy(row => row.StudentName)
            .ToList();

        return new SpellingTestResultsDto(
            test.Id,
            test.Title,
            test.DueAt,
            rows.Count,
            rows.Count(row => row.Status == StudentSpellingTestAttemptStatuses.Completed),
            rows.Count(row => row.Status == StudentSpellingTestAttemptStatuses.Overdue),
            rows);
    }

    public async Task<SpellingTestSummaryDto> UpdateAsync(
        int testId,
        UpdateSpellingTestRequest request,
        int teacherId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        var test = await GetTeacherTestAsync(testId, teacherId, isAdmin, cancellationToken, tracking: true);
        if (!string.IsNullOrWhiteSpace(request.Title))
            test.Title = request.Title.Trim();
        if (request.Description is not null)
            test.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        if (request.DueAt.HasValue)
        {
            if (request.DueAt.Value <= DateTime.UtcNow)
                throw new InvalidOperationException("Due date must be in the future.");
            test.DueAt = DateTime.SpecifyKind(request.DueAt.Value, DateTimeKind.Utc);
        }
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim().ToUpperInvariant();
            if (status is not (SpellingTestStatuses.Draft or SpellingTestStatuses.Active or SpellingTestStatuses.Completed or SpellingTestStatuses.Archived))
                throw new InvalidOperationException("Invalid spelling test status.");
            test.Status = status;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildTeacherSummaryAsync(test.Id, cancellationToken);
    }

    public async Task<bool> ArchiveAsync(int testId, int teacherId, bool isAdmin, CancellationToken cancellationToken)
    {
        var test = await GetTeacherTestAsync(testId, teacherId, isAdmin, cancellationToken, tracking: true);
        test.Status = SpellingTestStatuses.Archived;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<StudentSpellingTestSummaryDto>> GetActiveForStudentAsync(int studentId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var attempts = await dbContext.StudentSpellingTestAttempts.AsNoTracking()
            .Include(attempt => attempt.SpellingTest)
            .Where(attempt => attempt.StudentId == studentId && attempt.SpellingTest.Status == SpellingTestStatuses.Active)
            .OrderBy(attempt => attempt.SpellingTest.DueAt)
            .ToListAsync(cancellationToken);

        var visible = attempts.Where(attempt =>
        {
            var status = ResolveStudentStatus(attempt, attempt.SpellingTest.DueAt, now);
            if (status is StudentSpellingTestAttemptStatuses.NotStarted or StudentSpellingTestAttemptStatuses.InProgress or StudentSpellingTestAttemptStatuses.Overdue)
                return true;
            return attempt.CompletedAt.HasValue && now - attempt.CompletedAt.Value <= CompletedVisibilityWindow;
        }).ToList();

        var result = new List<StudentSpellingTestSummaryDto>();
        foreach (var attempt in visible)
            result.Add(await BuildStudentSummaryAsync(attempt.SpellingTestId, studentId, cancellationToken));
        return result;
    }

    public async Task<StudentSpellingTestDetailDto> GetStudentDetailAsync(int testId, int studentId, CancellationToken cancellationToken)
    {
        await GetStudentAttemptAsync(testId, studentId, cancellationToken);
        return await BuildStudentDetailAsync(testId, studentId, cancellationToken);
    }

    public async Task<StudentSpellingTestDetailDto> StartAsync(int testId, int studentId, CancellationToken cancellationToken)
    {
        var attempt = await GetStudentAttemptAsync(testId, studentId, cancellationToken, tracking: true);
        var now = DateTime.UtcNow;
        if (attempt.SpellingTest.Status == SpellingTestStatuses.Archived)
            throw new InvalidOperationException("This spelling test is no longer available.");
        if (attempt.CompletedAt.HasValue)
            return await BuildStudentDetailAsync(testId, studentId, cancellationToken);
        if (attempt.ConfirmedAt.HasValue)
            return await ResumeAsync(testId, studentId, cancellationToken);
        if (attempt.SpellingTest.DueAt < now)
        {
            attempt.Status = StudentSpellingTestAttemptStatuses.Overdue;
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("This spelling test is overdue.");
        }

        attempt.Status = StudentSpellingTestAttemptStatuses.InProgress;
        attempt.StartedAt ??= now;
        attempt.ConfirmedAt ??= now;
        attempt.RemainingSeconds ??= GetTimeLimitSeconds(attempt.SpellingTest);
        attempt.LastResumedAt = now;
        attempt.ModalSeenAt ??= now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildStudentDetailAsync(testId, studentId, cancellationToken);
    }

    public async Task<StudentSpellingTestDetailDto> ResumeAsync(int testId, int studentId, CancellationToken cancellationToken)
    {
        var attempt = await GetStudentAttemptAsync(testId, studentId, cancellationToken, tracking: true);
        var now = DateTime.UtcNow;
        if (attempt.CompletedAt.HasValue)
            return await BuildStudentDetailAsync(testId, studentId, cancellationToken);
        if (!attempt.ConfirmedAt.HasValue)
            return await BuildStudentDetailAsync(testId, studentId, cancellationToken);
        if (attempt.SpellingTest.DueAt < now)
        {
            attempt.Status = StudentSpellingTestAttemptStatuses.Overdue;
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("This spelling test is overdue.");
        }

        UpdateRemainingFromRunningClock(attempt, now);
        if ((attempt.RemainingSeconds ?? 0) <= 0)
        {
            await FinalizeAttemptAsync(attempt, GetSavedAnswers(attempt.ResultJson), now, cancellationToken);
            return await BuildStudentDetailAsync(testId, studentId, cancellationToken);
        }

        attempt.Status = StudentSpellingTestAttemptStatuses.InProgress;
        attempt.LastResumedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildStudentDetailAsync(testId, studentId, cancellationToken);
    }

    public async Task<StudentSpellingTestDetailDto> PauseAsync(int testId, int studentId, PauseSpellingTestRequest request, CancellationToken cancellationToken)
    {
        var attempt = await GetStudentAttemptAsync(testId, studentId, cancellationToken, tracking: true);
        var now = DateTime.UtcNow;
        if (attempt.CompletedAt.HasValue)
            return await BuildStudentDetailAsync(testId, studentId, cancellationToken);
        if (!attempt.ConfirmedAt.HasValue)
            return await BuildStudentDetailAsync(testId, studentId, cancellationToken);

        var answers = request.Answers ?? Array.Empty<SpellingTestAnswerDto>();
        UpdateRemainingFromRunningClock(attempt, now);
        if ((attempt.RemainingSeconds ?? 0) <= 0)
        {
            await FinalizeAttemptAsync(attempt, answers, now, cancellationToken);
            return await BuildStudentDetailAsync(testId, studentId, cancellationToken);
        }

        attempt.ResultJson = Serialize(new AttemptResultEnvelope(SavedAnswers: answers.ToList()));
        attempt.LastResumedAt = null;
        attempt.Status = StudentSpellingTestAttemptStatuses.InProgress;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildStudentDetailAsync(testId, studentId, cancellationToken);
    }

    public async Task<SubmitSpellingTestResultDto> SubmitAsync(int testId, int studentId, SubmitSpellingTestRequest request, CancellationToken cancellationToken)
    {
        var attempt = await GetStudentAttemptAsync(testId, studentId, cancellationToken, tracking: true);
        if (attempt.CompletedAt.HasValue)
        {
            return new SubmitSpellingTestResultDto(
                testId,
                attempt.Status,
                attempt.Score ?? 0,
                attempt.Stars ?? 0,
                GetStoredCorrectCount(attempt.ResultJson),
                GetStoredTotalCount(attempt.ResultJson),
                attempt.CompletedAt.Value);
        }
        if (attempt.SpellingTest.DueAt < DateTime.UtcNow)
        {
            attempt.Status = StudentSpellingTestAttemptStatuses.Overdue;
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("This spelling test is overdue.");
        }

        var answers = request.Answers ?? Array.Empty<SpellingTestAnswerDto>();
        var completedAt = DateTime.UtcNow;
        UpdateRemainingFromRunningClock(attempt, completedAt);
        await FinalizeAttemptAsync(attempt, answers, completedAt, cancellationToken);

        return new SubmitSpellingTestResultDto(
            testId,
            attempt.Status,
            attempt.Score ?? 0,
            attempt.Stars ?? 0,
            GetStoredCorrectCount(attempt.ResultJson),
            GetStoredTotalCount(attempt.ResultJson),
            attempt.CompletedAt ?? completedAt);
    }

    private async Task FinalizeAttemptAsync(
        StudentSpellingTestAttempt attempt,
        IReadOnlyList<SpellingTestAnswerDto> answers,
        DateTime completedAt,
        CancellationToken cancellationToken)
    {
        var wordIds = ParseIntList(attempt.SpellingTest.WordItemIdsJson);
        var questions = await dbContext.VocabularyItems.AsNoTracking()
            .Include(item => item.Translations)
            .Where(item => wordIds.Contains(item.Id))
            .ToListAsync(cancellationToken);
        var questionsById = questions.ToDictionary(item => item.Id);

        var correct = 0;
        var results = new List<SpellingTestAnswerResultDto>();
        foreach (var wordId in wordIds)
        {
            if (!questionsById.TryGetValue(wordId, out var question))
                continue;
            var answer = answers.LastOrDefault(candidate => candidate.VocabularyItemId == wordId);
            var answerText = answer?.AnswerText ?? string.Empty;
            var wasCorrect = NormalizeAnswer(answerText) == NormalizeAnswer(question.Word);
            if (wasCorrect) correct++;
            results.Add(new SpellingTestAnswerResultDto(
                wordId,
                question.Word,
                answerText,
                wasCorrect,
                answer?.ResponseTimeMs,
                answer?.RetriesCount,
                answer?.HintsUsed));
        }

        var total = Math.Max(questions.Count, 1);
        var score = (int)Math.Round((double)correct / total * 100);
        var stars = score >= 90 ? 3 : score >= 60 ? 2 : score > 0 ? 1 : 0;

        attempt.Status = StudentSpellingTestAttemptStatuses.Completed;
        attempt.Score = score;
        attempt.Stars = stars;
        attempt.CompletedAt = completedAt;
        attempt.StartedAt ??= completedAt;
        attempt.LastResumedAt = null;
        attempt.RemainingSeconds = Math.Max(0, attempt.RemainingSeconds ?? 0);
        attempt.ResultJson = Serialize(new AttemptResultEnvelope(
            SavedAnswers: answers.ToList(),
            Results: results,
            Correct: correct,
            Total: total,
            Score: score,
            Stars: stars));
        await UpdateWordProgressAfterSpellingTestAsync(attempt, results, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Trigger adaptive evaluation after the spelling attempt is safely persisted.
        try
        {
            await adaptiveLearningAgent.EvaluateAndTriggerDraftAsync(
                attempt.StudentId,
                attempt.Id,
                isSpellingTest: true,
                cancellationToken);
        }
        catch
        {
            // Non-fatal; the spelling test submission remains authoritative.
        }

        // Check if this spelling test is linked to an accepted hardcore draft
        try
        {
            var hardcoreDraft = await dbContext.HardcoreChallengeDrafts
                .FirstOrDefaultAsync(d => d.LinkedSpellingTestId == attempt.SpellingTestId && d.StudentId == attempt.StudentId && d.Status == "ACCEPTED", cancellationToken);

            if (hardcoreDraft != null)
            {
                hardcoreDraft.Status = "COMPLETED";
                hardcoreDraft.CompletedAt = DateTime.UtcNow;

                // Award configurable rewards
                var user = await dbContext.Users
                    .FirstOrDefaultAsync(u => u.Id == attempt.StudentId, cancellationToken);
                if (user != null)
                {
                    user.Experience += hardcoreDraft.RewardXp;
                    user.Diamonds += hardcoreDraft.RewardDiamonds;
                }

                var userProgress = await dbContext.UserProgresses
                    .FirstOrDefaultAsync(p => p.UserId == attempt.StudentId, cancellationToken);
                if (userProgress != null)
                {
                    userProgress.TotalXP += hardcoreDraft.RewardXp;
                    userProgress.ModifiedDate = DateTime.UtcNow;

                    var nextLevel = await dbContext.Levels.AsNoTracking()
                        .Where(l => l.LevelNumber > userProgress.CurrentLevel && l.RequiredXP <= userProgress.TotalXP)
                        .OrderByDescending(l => l.LevelNumber)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (nextLevel is not null)
                    {
                        userProgress.CurrentLevel = nextLevel.LevelNumber;
                        if (user is not null)
                        {
                            user.Level = nextLevel.LevelNumber;
                        }
                    }
                }

                // Unlock limited edition mascot
                if (hardcoreDraft.MascotEligibility && !string.IsNullOrWhiteSpace(hardcoreDraft.MascotName))
                {
                    var mascotItem = await dbContext.ShopItems
                        .AsNoTracking()
                        .FirstOrDefaultAsync(item => item.Name == hardcoreDraft.MascotName && item.Category == "avatar", cancellationToken);

                    if (mascotItem != null)
                    {
                        var alreadyOwned = await dbContext.UserInventoryItems
                            .AnyAsync(ii => ii.UserId == attempt.StudentId && ii.ShopItemId == mascotItem.Id, cancellationToken);
                        if (!alreadyOwned)
                        {
                            var invItem = new CleanArc.Domain.Entities.Shop.UserInventoryItem
                            {
                                UserId = attempt.StudentId,
                                ShopItemId = mascotItem.Id,
                                AcquiredAt = DateTime.UtcNow
                            };
                            dbContext.UserInventoryItems.Add(invItem);
                        }
                    }
                }

                // Grant exclusive badge progression
                if (!string.IsNullOrWhiteSpace(hardcoreDraft.BadgeCode))
                {
                    var badge = await dbContext.Badges
                        .AsNoTracking()
                        .FirstOrDefaultAsync(b => b.Code == hardcoreDraft.BadgeCode, cancellationToken);

                    if (badge != null)
                    {
                        var progress = await dbContext.UserBadgeProgresses
                            .FirstOrDefaultAsync(bp => bp.UserId == attempt.StudentId && bp.BadgeId == badge.Id, cancellationToken);

                        if (progress == null)
                        {
                            progress = new CleanArc.Domain.Entities.Achievement.UserBadgeProgress
                            {
                                UserId = attempt.StudentId,
                                BadgeId = badge.Id,
                                ProgressValue = 1,
                                LastEvaluatedAt = DateTime.UtcNow
                            };
                            dbContext.UserBadgeProgresses.Add(progress);
                        }
                        else
                        {
                            progress.ProgressValue += 1;
                            progress.LastEvaluatedAt = DateTime.UtcNow;
                        }
                    }
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        catch
        {
            // Safeguard
        }
    }

    public async Task<StudentSpellingTestSummaryDto> DismissModalAsync(int testId, int studentId, CancellationToken cancellationToken)
    {
        var attempt = await GetStudentAttemptAsync(testId, studentId, cancellationToken, tracking: true);
        var now = DateTime.UtcNow;
        attempt.ModalSeenAt ??= now;
        attempt.DismissedAt ??= now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildStudentSummaryAsync(testId, studentId, cancellationToken);
    }

    private async Task<Classroom> GetManagedClassroomAsync(int classroomId, int teacherId, bool isAdmin, CancellationToken cancellationToken)
    {
        var classroom = await dbContext.Classrooms.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == classroomId && item.IsActive && !item.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Classroom not found.");
        if (!isAdmin && classroom.TeacherId != teacherId)
            throw new UnauthorizedAccessException("You do not manage this classroom.");
        return classroom;
    }

    private async Task<SpellingTest> GetTeacherTestAsync(int testId, int teacherId, bool isAdmin, CancellationToken cancellationToken, bool tracking = false)
    {
        var query = tracking ? dbContext.SpellingTests : dbContext.SpellingTests.AsNoTracking();
        var test = await query.Include(item => item.Classroom)
            .FirstOrDefaultAsync(item => item.Id == testId, cancellationToken)
            ?? throw new InvalidOperationException("Spelling test not found.");
        if (!isAdmin && test.Classroom.TeacherId != teacherId)
            throw new UnauthorizedAccessException("You do not manage this spelling test.");
        return test;
    }

    private async Task<StudentSpellingTestAttempt> GetStudentAttemptAsync(int testId, int studentId, CancellationToken cancellationToken, bool tracking = false)
    {
        var query = tracking ? dbContext.StudentSpellingTestAttempts : dbContext.StudentSpellingTestAttempts.AsNoTracking();
        var attempt = await query.Include(attempt => attempt.SpellingTest)
            .FirstOrDefaultAsync(attempt => attempt.SpellingTestId == testId && attempt.StudentId == studentId, cancellationToken);

        if (attempt != null)
        {
            return attempt;
        }

        var test = await dbContext.SpellingTests.AsNoTracking()
            .FirstOrDefaultAsync(st => st.Id == testId, cancellationToken);
        if (test == null)
            throw new InvalidOperationException("Spelling test not found.");

        var isMember = await dbContext.ClassroomStudents.AsNoTracking()
            .AnyAsync(cs => cs.ClassroomId == test.ClassroomId && cs.UserId == studentId, cancellationToken);
        if (!isMember)
            throw new InvalidOperationException("Spelling test not found.");

        var newAttempt = new StudentSpellingTestAttempt
        {
            SpellingTestId = testId,
            StudentId = studentId,
            Status = StudentSpellingTestAttemptStatuses.NotStarted,
            ResultJson = "{}"
        };

        dbContext.StudentSpellingTestAttempts.Add(newAttempt);
        await dbContext.SaveChangesAsync(cancellationToken);

        var finalQuery = tracking ? dbContext.StudentSpellingTestAttempts : dbContext.StudentSpellingTestAttempts.AsNoTracking();
        return await finalQuery.Include(a => a.SpellingTest)
            .FirstAsync(a => a.SpellingTestId == testId && a.StudentId == studentId, cancellationToken);
    }

    private async Task EnsureModulesAttachedAsync(int classroomId, IReadOnlyList<int> moduleIds, CancellationToken cancellationToken)
    {
        var attached = await dbContext.ClassroomModules.AsNoTracking()
            .Where(link => link.ClassroomId == classroomId && moduleIds.Contains(link.ModuleId))
            .Select(link => link.ModuleId)
            .ToListAsync(cancellationToken);
        if (attached.Distinct().Count() != moduleIds.Count)
        {
            logger.LogWarning(
                "Spelling test create rejected because selected modules are not all attached to classroom {ClassroomId}. RequestedModuleIds={RequestedModuleIds}; AttachedModuleIds={AttachedModuleIds}",
                classroomId,
                string.Join(",", moduleIds),
                string.Join(",", attached));
            throw new InvalidOperationException("Selected modules must belong to this classroom.");
        }
    }

    private InvalidOperationException CreateSpellingTestFailure(
        string message,
        int classroomId,
        int teacherId,
        CreateSpellingTestRequest request,
        IReadOnlyList<int>? normalizedModuleIds,
        IReadOnlyList<string>? moduleDetails = null)
    {
        logger.LogWarning(
            "Spelling test create rejected: {Reason}. ClassroomId={ClassroomId}; TeacherId={TeacherId}; Subject={Subject}; Title={Title}; RequestModuleIds={RequestModuleIds}; NormalizedModuleIds={NormalizedModuleIds}; DueAt={DueAt}; WordCount={WordCount}; Difficulty={Difficulty}; GameType={GameType}; ModuleDetails={ModuleDetails}",
            message,
            classroomId,
            teacherId,
            request.Subject,
            request.Title,
            string.Join(",", request.ModuleIds ?? Array.Empty<int>()),
            normalizedModuleIds is null ? null : string.Join(",", normalizedModuleIds),
            request.DueAt,
            request.Config?.WordCount,
            request.Config?.Difficulty,
            request.Config?.GameType,
            moduleDetails is null ? null : string.Join(" | ", moduleDetails));

        return new InvalidOperationException(message);
    }

    private async Task EnsureClassroomModuleLinksAsync(Classroom classroom, CancellationToken cancellationToken)
    {
        var subjects = await dbContext.ClassroomSubjects.AsNoTracking()
            .Where(subject => subject.ClassroomId == classroom.Id)
            .Select(subject => subject.Subject)
            .ToListAsync(cancellationToken);

        if (subjects.Count == 0 && !string.IsNullOrWhiteSpace(classroom.Subject))
        {
            subjects.Add(classroom.Subject.Trim());
        }

        subjects = subjects
            .Where(subject => !string.IsNullOrWhiteSpace(subject))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (subjects.Count == 0)
        {
            return;
        }

        var existingSubjects = (await dbContext.ClassroomSubjects.AsNoTracking()
                .Where(subject => subject.ClassroomId == classroom.Id)
                .Select(subject => subject.Subject)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var subject in subjects.Where(subject => !existingSubjects.Contains(subject)))
        {
            dbContext.ClassroomSubjects.Add(new ClassroomSubject
            {
                ClassroomId = classroom.Id,
                Subject = subject
            });
        }

        var matchingModuleIds = await dbContext.SyllabusModules.AsNoTracking()
            .Where(module => module.IsActive
                             && module.ModuleType == SyllabusModule.PredefinedModuleType
                             && module.YearLevel == classroom.YearLevel
                             && subjects.Contains(module.Subject)
                             && dbContext.VocabularyItems.Any(vocabulary => vocabulary.ModuleId == module.Id && vocabulary.IsActive))
            .Select(module => module.Id)
            .ToListAsync(cancellationToken);

        if (matchingModuleIds.Count == 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var existingModuleIds = await dbContext.ClassroomModules.AsNoTracking()
            .Where(link => link.ClassroomId == classroom.Id && matchingModuleIds.Contains(link.ModuleId))
            .Select(link => link.ModuleId)
            .ToListAsync(cancellationToken);
        var existingModules = existingModuleIds.ToHashSet();

        foreach (var moduleId in matchingModuleIds.Where(moduleId => !existingModules.Contains(moduleId)))
        {
            dbContext.ClassroomModules.Add(new ClassroomModule
            {
                ClassroomId = classroom.Id,
                ModuleId = moduleId
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<VocabularyItem>> SelectWordsAsync(
        int classroomId,
        IReadOnlyList<int> moduleIds,
        SpellingTestConfigDto config,
        string title,
        CancellationToken cancellationToken)
    {
        var allWords = await dbContext.VocabularyItems.AsNoTracking()
            .Where(item => moduleIds.Contains(item.ModuleId) && item.IsActive)
            .OrderBy(item => item.ModuleId)
            .ThenBy(item => item.DisplayOrder)
            .ThenBy(item => item.Word)
            .ToListAsync(cancellationToken);

        var words = allWords;
        if (config.Difficulty is int difficulty)
        {
            var filtered = allWords.Where(item => item.DifficultyLevel == difficulty).ToList();
            if (filtered.Count > 0)
            {
                words = filtered;
            }
            else
            {
                logger.LogInformation(
                    "Spelling test words selection: No words found with difficulty {Difficulty} in modules {ModuleIds}. Falling back to all difficulties.",
                    difficulty,
                    string.Join(",", moduleIds));
            }
        }

        if (config.IncludeUnmasteredOnly)
        {
            var studentIds = await dbContext.ClassroomStudents.AsNoTracking()
                .Where(student => student.ClassroomId == classroomId)
                .Select(student => student.UserId)
                .ToListAsync(cancellationToken);
            var progresses = await dbContext.WordProgresses.AsNoTracking()
                .Where(wp => studentIds.Contains(wp.StudentId)
                             && wp.WordId > 0)
                .ToListAsync(cancellationToken);

            var weakIds = progresses
                .Select(wp => new
                {
                    wp.WordId,
                    DecayedScore = MasteryEngine.GetDecayedMasteryScore(wp.MasteryScore, wp.LastPracticedAt)
                })
                .Where(x => x.DecayedScore < 80)
                .GroupBy(x => x.WordId)
                .OrderBy(group => group.Average(item => item.DecayedScore))
                .Select(group => group.Key)
                .ToList();

            var weakSet = weakIds.ToHashSet();
            var weakWords = words.Where(word => weakSet.Contains(word.Id)).ToList();
            if (weakWords.Count > 0)
                words = weakWords;
        }

        if (config.RandomizeOrder)
        {
            var seed = StableHash($"{classroomId}|{title}|{DateTime.UtcNow:yyyyMMddHH}");
            words = words.OrderBy(word => StableHash($"{seed}|{word.Id}")).ToList();
        }

        if (config.WordCount is int wordCount && wordCount > 0)
            words = words.Take(wordCount).ToList();

        return words;
    }

    private async Task<SpellingTestSummaryDto> BuildTeacherSummaryAsync(int testId, CancellationToken cancellationToken)
    {
        var test = await dbContext.SpellingTests.AsNoTracking()
            .Include(item => item.StudentAttempts)
            .FirstAsync(item => item.Id == testId, cancellationToken);
        var moduleIds = ParseIntList(test.SourceModuleIdsJson);
        var moduleTitles = await GetModuleTitlesAsync(moduleIds, cancellationToken);
        var wordIds = ParseIntList(test.WordItemIdsJson);
        var now = DateTime.UtcNow;
        var overdueCount = test.StudentAttempts.Count(attempt => ResolveStudentStatus(attempt, test.DueAt, now) == StudentSpellingTestAttemptStatuses.Overdue);
        return new SpellingTestSummaryDto(
            test.Id,
            test.ClassroomId,
            test.Subject,
            test.Title,
            test.Description,
            moduleIds,
            moduleTitles,
            test.DueAt,
            test.Status,
            wordIds.Count,
            test.StudentAttempts.Count,
            test.StudentAttempts.Count(attempt => attempt.Status == StudentSpellingTestAttemptStatuses.Completed),
            overdueCount,
            ParseConfig(test.ConfigJson),
            test.CreatedTime,
            test.ModifiedDate);
    }

    private async Task<StudentSpellingTestSummaryDto> BuildStudentSummaryAsync(int testId, int studentId, CancellationToken cancellationToken)
    {
        var attempt = await dbContext.StudentSpellingTestAttempts.AsNoTracking()
            .Include(item => item.SpellingTest)
            .FirstAsync(item => item.SpellingTestId == testId && item.StudentId == studentId, cancellationToken);
        var test = attempt.SpellingTest;
        var moduleTitles = await GetModuleTitlesAsync(ParseIntList(test.SourceModuleIdsJson), cancellationToken);
        var now = DateTime.UtcNow;
        var status = ResolveStudentStatus(attempt, test.DueAt, now);
        return new StudentSpellingTestSummaryDto(
            test.Id,
            test.ClassroomId,
            test.Subject,
            test.Title,
            test.Description,
            moduleTitles,
            test.DueAt,
            test.Status,
            status,
            ParseIntList(test.WordItemIdsJson).Count,
            attempt.Score,
            attempt.Stars,
            attempt.StartedAt,
            attempt.CompletedAt,
            attempt.ConfirmedAt,
            GetVisibleRemainingSeconds(attempt, test.DueAt, now),
            attempt.ModalSeenAt,
            attempt.DismissedAt,
            status is StudentSpellingTestAttemptStatuses.NotStarted && !attempt.ModalSeenAt.HasValue && !attempt.DismissedAt.HasValue,
            ParseConfig(test.ConfigJson));
    }

    private async Task<StudentSpellingTestDetailDto> BuildStudentDetailAsync(int testId, int studentId, CancellationToken cancellationToken)
    {
        var summary = await BuildStudentSummaryAsync(testId, studentId, cancellationToken);
        var test = await dbContext.SpellingTests.AsNoTracking().FirstAsync(item => item.Id == testId, cancellationToken);
        var attempt = await dbContext.StudentSpellingTestAttempts.AsNoTracking()
            .FirstAsync(item => item.SpellingTestId == testId && item.StudentId == studentId, cancellationToken);
        var wordIds = ParseIntList(test.WordItemIdsJson);
        var words = await dbContext.VocabularyItems.AsNoTracking()
            .Include(item => item.Translations)
            .Include(item => item.SyllableInfo)
            .Where(item => wordIds.Contains(item.Id))
            .ToListAsync(cancellationToken);
        var byId = words.ToDictionary(item => item.Id);
        var questions = wordIds
            .Where(byId.ContainsKey)
            .Select(id =>
            {
                var item = byId[id];
                return new SpellingTestQuestionDto(
                    item.Id,
                    item.Word,
                    item.MeaningText,
                    item.ExampleSentence,
                    ChallengeGenerator.GetTranslation(item, "ms"),
                    ChallengeGenerator.GetTranslation(item, "en"),
                    ChallengeGenerator.GetTranslation(item, "zh"),
                    item.SyllableInfo?.SyllableText,
                    item.DifficultyLevel,
                    GenerateWordDistractors(item.Word));
            })
            .ToList();
        return new StudentSpellingTestDetailDto(
            summary,
            questions,
            summary.RemainingSeconds,
            GetSavedAnswers(attempt.ResultJson));
    }

    private async Task<IReadOnlyList<string>> GetModuleTitlesAsync(IReadOnlyList<int> moduleIds, CancellationToken cancellationToken)
    {
        var modules = await dbContext.SyllabusModules.AsNoTracking()
            .Where(module => moduleIds.Contains(module.Id))
            .OrderBy(module => module.UnitNumber ?? int.MaxValue)
            .ThenBy(module => module.Title)
            .Select(module => string.IsNullOrWhiteSpace(module.UnitTitle) ? module.Title : module.UnitTitle)
            .ToListAsync(cancellationToken);
        return modules;
    }

    private static string ResolveStudentStatus(StudentSpellingTestAttempt attempt, DateTime dueAt, DateTime now)
    {
        if (attempt.Status == StudentSpellingTestAttemptStatuses.Completed)
            return StudentSpellingTestAttemptStatuses.Completed;
        if (dueAt < now)
            return StudentSpellingTestAttemptStatuses.Overdue;
        return attempt.Status;
    }

    private static SpellingTestConfigDto NormalizeConfig(SpellingTestConfigDto? config)
    {
        var wordCount = config?.WordCount is > 0 ? Math.Min(config.WordCount.Value, 60) : 20;
        var difficulty = config?.Difficulty is >= 1 and <= 5 ? config.Difficulty : null;
        var timeLimitSeconds = config?.TimeLimitSeconds is > 0
            ? Math.Clamp(config.TimeLimitSeconds.Value, 30, 3600)
            : DefaultTimeLimitSeconds;
        var gameType = config?.GameType?.Trim();
        if (string.IsNullOrWhiteSpace(gameType))
        {
            gameType = "MIXED";
        }
        return new SpellingTestConfigDto(
            wordCount,
            difficulty,
            config?.IncludeUnmasteredOnly ?? false,
            config?.RandomizeOrder ?? true,
            config?.AllowRetries ?? false,
            timeLimitSeconds,
            gameType);
    }

    private static SpellingTestConfigDto ParseConfig(string raw)
    {
        try
        {
            return NormalizeConfig(JsonSerializer.Deserialize<SpellingTestConfigDto>(raw, JsonOptions));
        }
        catch
        {
            return NormalizeConfig(null);
        }
    }

    private static List<int> ParseIntList(string raw)
    {
        try
        {
            return JsonSerializer.Deserialize<List<int>>(raw, JsonOptions) ?? new List<int>();
        }
        catch
        {
            return new List<int>();
        }
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

    private static int GetTimeLimitSeconds(SpellingTest test) => ParseConfig(test.ConfigJson).TimeLimitSeconds ?? DefaultTimeLimitSeconds;

    private static int? GetVisibleRemainingSeconds(StudentSpellingTestAttempt attempt, DateTime dueAt, DateTime now)
    {
        if (attempt.Status == StudentSpellingTestAttemptStatuses.Completed || dueAt < now)
            return attempt.RemainingSeconds;
        if (!attempt.ConfirmedAt.HasValue)
            return GetTimeLimitSeconds(attempt.SpellingTest);
        var remaining = attempt.RemainingSeconds ?? GetTimeLimitSeconds(attempt.SpellingTest);
        if (!attempt.LastResumedAt.HasValue)
            return remaining;
        var elapsed = (int)Math.Floor((now - attempt.LastResumedAt.Value).TotalSeconds);
        return Math.Max(0, remaining - Math.Max(0, elapsed));
    }

    private static void UpdateRemainingFromRunningClock(StudentSpellingTestAttempt attempt, DateTime now)
    {
        var remaining = attempt.RemainingSeconds ?? GetTimeLimitSeconds(attempt.SpellingTest);
        if (attempt.LastResumedAt.HasValue)
        {
            var elapsed = (int)Math.Floor((now - attempt.LastResumedAt.Value).TotalSeconds);
            remaining = Math.Max(0, remaining - Math.Max(0, elapsed));
        }

        attempt.RemainingSeconds = remaining;
        attempt.LastResumedAt = null;
    }

    private static IReadOnlyList<SpellingTestAnswerDto> GetSavedAnswers(string raw)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<AttemptResultEnvelope>(raw, JsonOptions);
            return envelope?.SavedAnswers ?? Array.Empty<SpellingTestAnswerDto>();
        }
        catch
        {
            return Array.Empty<SpellingTestAnswerDto>();
        }
    }

    private static int GetStoredCorrectCount(string raw)
    {
        try
        {
            return JsonSerializer.Deserialize<AttemptResultEnvelope>(raw, JsonOptions)?.Correct ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private static int GetStoredTotalCount(string raw)
    {
        try
        {
            return JsonSerializer.Deserialize<AttemptResultEnvelope>(raw, JsonOptions)?.Total ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private static string NormalizeAnswer(string value)
        => new(value.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    private static List<string> GenerateWordDistractors(string word)
    {
        var list = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var len = word.Length;

        if (len > 3)
        {
            for (int i = 1; i < len - 1; i++)
            {
                var chars = word.ToCharArray();
                var tmp = chars[i];
                chars[i] = chars[i + 1];
                chars[i + 1] = tmp;
                var w = new string(chars);
                if (w != word) list.Add(w);
                if (list.Count >= 3) break;
            }
        }

        var vowels = new HashSet<char> { 'a', 'e', 'i', 'o', 'u' };
        for (int i = 0; i < len; i++)
        {
            if (vowels.Contains(char.ToLower(word[i])))
            {
                foreach (var v in vowels)
                {
                    if (char.ToLower(v) != char.ToLower(word[i]))
                    {
                        var w = word.Substring(0, i) + v + word.Substring(i + 1);
                        list.Add(w);
                        if (list.Count >= 3) break;
                    }
                }
            }
            if (list.Count >= 3) break;
        }

        if (len > 2)
        {
            for (int i = 1; i < len; i++)
            {
                var w = word.Substring(0, i) + word.Substring(i + 1);
                list.Add(w);
                if (list.Count >= 3) break;
            }
        }

        int count = 1;
        while (list.Count < 3)
        {
            list.Add(word + count++);
        }

        return list.Take(3).ToList();
    }

    private static int StableHash(string value)
    {
        var hash = 17;
        foreach (var character in value)
            hash = unchecked(hash * 31 + character);
        return Math.Abs(hash);
    }

    private async Task UpdateWordProgressAfterSpellingTestAsync(
        StudentSpellingTestAttempt attempt,
        List<SpellingTestAnswerResultDto> results,
        CancellationToken cancellationToken)
    {
        if (results == null || results.Count == 0) return;

        var vocabularyItemIds = results.Select(r => r.VocabularyItemId).Distinct().ToList();

        var existingProgresses = await dbContext.WordProgresses
            .Where(wp => wp.StudentId == attempt.StudentId && vocabularyItemIds.Contains(wp.WordId))
            .ToListAsync(cancellationToken);

        var progressMap = existingProgresses.ToDictionary(wp => wp.WordId);

        foreach (var result in results)
        {
            var wordId = result.VocabularyItemId;

            if (!progressMap.TryGetValue(wordId, out var wp))
            {
                wp = new WordProgress
                {
                    StudentId = attempt.StudentId,
                    WordId = wordId,
                    TotalAttempts = 0,
                    TotalCorrect = 0,
                    MasteryScore = 0,
                    LastPracticedAt = null,
                    NextReviewDate = null
                };
                dbContext.WordProgresses.Add(wp);
                progressMap[wordId] = wp;
            }

            wp.TotalAttempts += 1;
            if (result.WasCorrect)
            {
                wp.TotalCorrect += 1;
            }
            wp.LastPracticedAt = DateTime.UtcNow;

            // Fetch recent attempts from other challenge types for consistency calculations
            var recentAttempts = await dbContext.StudentChallengeItemAttempts
                .Where(i => i.StudentChallengeAttempt.StudentId == attempt.StudentId && i.VocabularyItemId == wordId)
                .OrderByDescending(i => i.AnsweredAt)
                .Take(10)
                .Select(i => i.WasCorrect)
                .ToListAsync(cancellationToken);
            recentAttempts.Reverse();

            recentAttempts.Add(result.WasCorrect);
            if (recentAttempts.Count > 10)
            {
                recentAttempts.RemoveAt(0);
            }

            // Calculate accuracy, consistency, and mastery score
            double accuracy = AdaptiveAttemptService.CalculateAccuracy(wp.TotalCorrect, wp.TotalAttempts);
            double consistency = AdaptiveAttemptService.CalculateConsistency(wp.TotalCorrect, wp.TotalAttempts, recentAttempts);
            double retention = 100.0; // Reset retention to 100% after practice/test completion

            wp.MasteryScore = AdaptiveAttemptService.CalculateMasteryScore(accuracy, consistency, retention);
            wp.NextReviewDate = AdaptiveAttemptService.CalculateNextReviewDate(wp.MasteryScore, result.WasCorrect);
        }
    }

    private record AttemptResultEnvelope(
        IReadOnlyList<SpellingTestAnswerDto>? SavedAnswers = null,
        IReadOnlyList<SpellingTestAnswerResultDto>? Results = null,
        int Correct = 0,
        int Total = 0,
        int Score = 0,
        int Stars = 0);

    private record SpellingTestAnswerResultDto(
        int VocabularyItemId,
        string Expected,
        string AnswerText,
        bool WasCorrect,
        int? ResponseTimeMs,
        int? RetriesCount,
        int? HintsUsed);
}
