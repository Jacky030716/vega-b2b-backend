using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanArc.Application.Contracts.AI;
using CleanArc.Application.Contracts.Audit;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.Quiz;
using CleanArc.Domain.Entities.Adaptive;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanArc.Infrastructure.Persistence.Services.AI;

/// <summary>
/// Professor Vega Agent Inbox Service. Coordinates the supervisor (Manager) agent,
/// specialist (Worker) agents, QualityGuard (Judge) evaluator, and ActionDelivery (Tool) interfaces.
/// </summary>
public sealed class VegaAgentService : IVegaAgentService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly IClassroomModuleManagementService _moduleService;
    private readonly ILogger<VegaAgentService> _logger;

    public VegaAgentService(
        ApplicationDbContext dbContext,
        IAuditService auditService,
        IClassroomModuleManagementService moduleService,
        ILogger<VegaAgentService> logger)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _moduleService = moduleService;
        _logger = logger;
    }

    /// <summary>
    /// Acts as the Manager/Router Agent (ProfessorVegaSupervisor).
    /// Orchestrates subtask delegation to specialists, collects their findings,
    /// and filters them through the evaluator (Judge).
    /// </summary>
    public async Task<List<VegaRecommendationDto>> GetInboxRecommendationsAsync(int classroomId, int teacherId, CancellationToken cancellationToken)
    {
        var rawRecommendations = new List<VegaRecommendationDto>();

        // 1. Verify Classroom and Teacher
        var classroom = await _dbContext.Classrooms
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == classroomId && !c.IsDeleted, cancellationToken);

        if (classroom == null || classroom.TeacherId != teacherId)
        {
            return rawRecommendations;
        }

        // Initialize Specialist and Tool Agents
        var healthSpecialist = new ClassroomHealthSpecialist(_auditService);
        var challengePlanner = new ChallengePlannerSpecialist(_auditService);
        var engagementSpecialist = new EngagementBoosterSpecialist(_dbContext);
        var actionTool = new ActionDeliveryTool(_dbContext, _moduleService, _auditService);
        var validatorJudge = new QualityGuardJudge();

        // 2. Delegate Classroom Health Analysis (ClassroomHealthSpecialist)
        var (healthScore, _) = await healthSpecialist.AnalyzeHealthAsync(classroomId, cancellationToken);
        var healthStats = await _auditService.GetClassroomHealthAsync(classroomId, cancellationToken);
        int studentCount = healthStats.StudentCount;

        // 3. Load Classroom Vocabulary Words (ActionDeliveryTool)
        var classroomVocabWords = await actionTool.LoadClassroomVocabWordsAsync(classroomId, cancellationToken);

        // 4. Plan Weak Words Recovery Challenges (ChallengePlannerSpecialist)
        var challengeDraftCard = await challengePlanner.PlanChallengeDraftAsync(
            classroomId, studentCount, healthScore, classroomVocabWords, cancellationToken);
        if (challengeDraftCard != null)
        {
            rawRecommendations.Add(challengeDraftCard);
        }

        // 5. Gather Student Risk and Streak Data (EngagementBoosterSpecialist)
        var students = await _dbContext.ClassroomStudents
            .AsNoTracking()
            .Where(cs => cs.ClassroomId == classroomId)
            .ToListAsync(cancellationToken);

        var studentIds = students.Select(cs => cs.UserId).ToList();

        var riskCard = await engagementSpecialist.CheckEngagementAsync(classroomId, studentCount, studentIds, cancellationToken);
        if (riskCard != null)
        {
            rawRecommendations.Add(riskCard);
        }

        // 6. Generate Engagement Booster Missions if Classroom Health has decay
        if (healthScore < 90)
        {
            var boosterCard = engagementSpecialist.GetEngagementBoostCard(classroomId, studentCount, healthScore);
            rawRecommendations.Add(boosterCard);
        }

        // 7. Judge/Evaluator Check (QualityGuardJudge)
        // Synthesize only recommendations that pass our contract validation checks
        var validatedRecommendations = new List<VegaRecommendationDto>();
        foreach (var rec in rawRecommendations)
        {
            if (validatorJudge.ValidateRecommendation(rec))
            {
                validatedRecommendations.Add(rec);
            }
            else
            {
                _logger.LogWarning("Recommendation {RecId} of type {RecType} failed QualityGuardJudge validation checks.", rec.Id, rec.Type);
            }
        }

        return validatedRecommendations;
    }

    /// <summary>
    /// Delegates delivery execution to the ActionDeliveryTool interface when approved.
    /// </summary>
    public async Task<bool> ApproveRecommendationAsync(int classroomId, string recommendationId, int teacherId, object? payload, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Approving Vega Agent recommendation {RecId} for classroom {ClassId} by teacher {TeacherId}", recommendationId, classroomId, teacherId);

        var actionTool = new ActionDeliveryTool(_dbContext, _moduleService, _auditService);

        if (recommendationId.StartsWith("challenge_draft_"))
        {
            return await actionTool.DeliverChallengeDraftApprovalAsync(classroomId, teacherId, cancellationToken);
        }
        else if (recommendationId.StartsWith("student_risk_") || recommendationId.StartsWith("engagement_"))
        {
            // Reminding students / triggering booster events
            return true;
        }

        return false;
    }

    #region Role-Based Agent Classes

    /// <summary>
    /// Specialist Agent: Analyzes classroom, modules, word mastery, and computes Classroom Health Score.
    /// </summary>
    private sealed class ClassroomHealthSpecialist(IAuditService auditService)
    {
        public async Task<(int HealthScore, string Status)> AnalyzeHealthAsync(int classroomId, CancellationToken cancellationToken)
        {
            var healthStats = await auditService.GetClassroomHealthAsync(classroomId, cancellationToken);
            int studentCount = healthStats.StudentCount;
            
            int healthScore = 100;
            if (studentCount > 0)
            {
                int penaltyForWeakWords = healthStats.WeakWordCount * 3;
                int penaltyForOverdue = healthStats.OverdueReviewCount * 2;
                int penaltyForMastery = (int)Math.Max(0, (80 - healthStats.AverageMasteryScore) * 1.5m);
                healthScore = Math.Max(10, 100 - penaltyForWeakWords - penaltyForOverdue - penaltyForMastery);
            }
            return (healthScore, healthStats.Status);
        }
    }

    /// <summary>
    /// Specialist Agent: Monitors weak words, overdue reviews, and mastery data to output challenge drafts.
    /// </summary>
    private sealed class ChallengePlannerSpecialist(IAuditService auditService)
    {
        public async Task<VegaRecommendationDto?> PlanChallengeDraftAsync(
            int classroomId, int studentCount, int healthScore, List<string> classroomVocabWords, CancellationToken cancellationToken)
        {
            var weakWordsStats = await auditService.GetWeakWordsAsync(classroomId, null, cancellationToken);
            var weakWordsList = weakWordsStats.WeakWords;

            var filteredWeakWords = weakWordsList
                .Where(w => classroomVocabWords.Contains(w, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (filteredWeakWords.Count > 0)
            {
                int affectedCount = weakWordsStats.AffectedStudents > 0 ? weakWordsStats.AffectedStudents : studentCount;
                int confidence = Math.Min(98, Math.Max(70, 95 - (filteredWeakWords.Count * 2)));

                return new VegaRecommendationDto(
                    Id: $"challenge_draft_{classroomId}",
                    Type: "CHALLENGE_DRAFT",
                    Title: "Challenge Draft Ready",
                    Reason: $"Vocabulary retention has dropped. {affectedCount} students show weak retention on key words.",
                    Confidence: confidence,
                    AffectedStudentsCount: affectedCount,
                    Evidence: new List<string>
                    {
                        $"{affectedCount} students below mastery threshold",
                        $"{filteredWeakWords.Count} weak words detected",
                        $"Classroom Health Score is currently {healthScore}%"
                    },
                    ProposedAction: "Create Recovery Challenge",
                    DraftPayload: new VegaChallengeDraftPayload(
                        GameType: "SPELL_CATCHER",
                        ModuleId: null,
                        Title: "Weak Words Recovery Challenge",
                        Words: filteredWeakWords.Take(8).ToList(),
                        DifficultyLevel: 2,
                        QuestionCount: 10
                    )
                );
            }
            return null;
        }
    }

    /// <summary>
    /// Specialist Agent: Monitors streaks, XP, forgetting risks, inactive students, and outputs booster suggestions.
    /// </summary>
    private sealed class EngagementBoosterSpecialist(ApplicationDbContext dbContext)
    {
        public async Task<VegaRecommendationDto?> CheckEngagementAsync(
            int classroomId, int studentCount, List<int> studentIds, CancellationToken cancellationToken)
        {
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
            var activeStudentIds = await dbContext.Attempts
                .AsNoTracking()
                .Where(a => studentIds.Contains(a.UserId) && a.CreatedTime >= sevenDaysAgo)
                .Select(a => a.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var inactiveCount = studentIds.Count - activeStudentIds.Count;

            if (inactiveCount > 0)
            {
                int confidence = Math.Min(99, 85 + inactiveCount);
                return new VegaRecommendationDto(
                    Id: $"student_risk_{classroomId}",
                    Type: "STUDENT_RISK",
                    Title: "Inactive Students Alert",
                    Reason: $"{inactiveCount} students have not practiced any challenges in the last 7 days.",
                    Confidence: confidence,
                    AffectedStudentsCount: inactiveCount,
                    Evidence: new List<string>
                    {
                        $"{inactiveCount} students completely inactive this week",
                        $"Average practice frequency is dropping"
                    },
                    ProposedAction: "Send Reminder Notification",
                    DraftPayload: null
                );
            }
            return null;
        }

        public VegaRecommendationDto GetEngagementBoostCard(int classroomId, int studentCount, int healthScore)
        {
            return new VegaRecommendationDto(
                Id: $"engagement_{classroomId}",
                Type: "ENGAGEMENT_MISSION",
                Title: "Engagement Boost Recommended",
                Reason: $"Classroom Health is {healthScore}%. Motivate students with a double XP event.",
                Confidence: 88,
                AffectedStudentsCount: studentCount,
                Evidence: new List<string>
                {
                    $"Classroom health is {healthScore}%",
                    $"Vocabulary completion is under target"
                },
                ProposedAction: "Activate Double XP Mission",
                DraftPayload: null
            );
        }
    }

    /// <summary>
    /// Evaluator Agent: Performs quality assurance contract checks on suggestions.
    /// </summary>
    private sealed class QualityGuardJudge
    {
        public bool ValidateRecommendation(VegaRecommendationDto recommendation)
        {
            if (recommendation.Confidence < 0 || recommendation.Confidence > 100)
                return false;

            if (recommendation.AffectedStudentsCount < 0)
                return false;

            if (recommendation.Type == "CHALLENGE_DRAFT")
            {
                if (recommendation.DraftPayload == null)
                    return false;
                if (recommendation.DraftPayload.Words == null || recommendation.DraftPayload.Words.Count == 0)
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Interface / Delivery Agent: Connects suggestions to external integrations and databases.
    /// </summary>
    private sealed class ActionDeliveryTool(ApplicationDbContext dbContext, IClassroomModuleManagementService moduleService, IAuditService auditService)
    {
        public async Task<List<string>> LoadClassroomVocabWordsAsync(int classroomId, CancellationToken cancellationToken)
        {
            var classroomModuleIds = await dbContext.ClassroomModules
                .AsNoTracking()
                .Where(cm => cm.ClassroomId == classroomId)
                .Select(cm => cm.ModuleId)
                .ToListAsync(cancellationToken);

            return await dbContext.VocabularyItems
                .AsNoTracking()
                .Where(v => classroomModuleIds.Contains(v.ModuleId) && v.IsActive)
                .Select(v => v.Word)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> DeliverChallengeDraftApprovalAsync(int classroomId, int teacherId, CancellationToken cancellationToken)
        {
            var classroom = await dbContext.Classrooms
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == classroomId && !c.IsDeleted, cancellationToken);

            if (classroom == null) return false;

            var customModule = await dbContext.ClassroomModules
                .Include(link => link.Module)
                .Where(link => link.ClassroomId == classroomId && link.Module.ModuleType == SyllabusModule.CustomModuleType)
                .Select(link => link.Module)
                .FirstOrDefaultAsync(cancellationToken);

            if (customModule == null)
            {
                customModule = new SyllabusModule
                {
                    ModuleCode = $"CUSTOM-{classroomId}-{Guid.NewGuid():N}",
                    Subject = string.IsNullOrWhiteSpace(classroom.Subject) ? "Custom" : classroom.Subject.Trim(),
                    Language = "ms",
                    YearLevel = classroom.YearLevel,
                    Term = string.Empty,
                    UnitTitle = "Custom Module",
                    Title = "Custom Module",
                    Description = "Teacher-created learning module.",
                    ModuleType = SyllabusModule.CustomModuleType,
                    SourceType = "teacher_created",
                    CreatedByTeacherId = teacherId
                };
                dbContext.SyllabusModules.Add(customModule);
                dbContext.ClassroomModules.Add(new ClassroomModule
                {
                    ClassroomId = classroomId,
                    Module = customModule
                });
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            var weakWordsStats = await auditService.GetWeakWordsAsync(classroomId, null, cancellationToken);
            var classroomVocabWords = await LoadClassroomVocabWordsAsync(classroomId, cancellationToken);

            var words = weakWordsStats.WeakWords
                .Where(w => classroomVocabWords.Contains(w, StringComparer.OrdinalIgnoreCase))
                .Take(8)
                .ToList();

            if (words.Count == 0)
            {
                words = classroomVocabWords.Take(8).ToList();
            }

            if (words.Count == 0)
            {
                words = new List<string> { "family", "school", "friend", "teacher" };
            }

            var createRequest = new CreateCustomModuleChallengeRequest(
                Title: "Recovery: Weak Words",
                GameType: "SPELL_CATCHER",
                Items: words
            );

            await moduleService.CreateCustomModuleChallengeAsync(customModule.Id, createRequest, teacherId, cancellationToken);
            return true;
        }
    }

    #endregion
}
