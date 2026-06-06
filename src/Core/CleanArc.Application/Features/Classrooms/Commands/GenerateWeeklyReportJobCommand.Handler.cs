using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Application.Models.Common;
using Mediator;
using Microsoft.Extensions.Logging;

namespace CleanArc.Application.Features.Classrooms.Commands;

internal sealed class GenerateWeeklyReportJobCommandHandler(
    IUnitOfWork unitOfWork,
    IAdaptiveAnalyticsService analyticsService,
    IAiGenerationService aiGenerationService,
    IAiAuditService aiAuditService,
    IAiUsageService aiUsageService,
    ILogger<GenerateWeeklyReportJobCommandHandler> logger)
    : IRequestHandler<GenerateWeeklyReportJobCommand, OperationResult<bool>>
{
    public async ValueTask<OperationResult<bool>> Handle(GenerateWeeklyReportJobCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Background execution started for weekly report generation. Classroom: {ClassroomId}, AuditLogId: {AuditLogId}", 
            request.ClassroomId, request.AuditLogId);

        try
        {
            // 1. Verify classroom
            var classroom = await unitOfWork.ClassroomRepository.GetClassroomByIdAsync(request.ClassroomId);
            if (classroom is null)
            {
                await aiAuditService.FailAsync(request.AuditLogId, null, new[] { "Classroom not found." }, cancellationToken);
                return OperationResult<bool>.FailureResult("Classroom not found.");
            }

            // 2. Gather classroom data
            var studentCount = await unitOfWork.ClassroomRepository.GetStudentCountAsync(request.ClassroomId);
            var challenges = await unitOfWork.ClassroomRepository.GetClassroomChallengesAsync(request.ClassroomId);
            var weaknessOverview = await analyticsService.GetClassWeaknessOverviewAsync(request.ClassroomId, cancellationToken);

            var challengeIds = challenges.Select(c => c.Id).ToList();
            var snapshots = await unitOfWork.ChallengeRepository.GetChallengeLeaderboardSnapshotsAsync(request.ClassroomId, challengeIds);

            // Format data context for prompt
            var sbContext = new StringBuilder();
            sbContext.AppendLine($"Classroom: {classroom.Name}");
            sbContext.AppendLine($"Subject: {classroom.Subject}");
            sbContext.AppendLine($"Year Level: {classroom.YearLevel}");
            sbContext.AppendLine($"Total Students: {studentCount}");
            sbContext.AppendLine();

            sbContext.AppendLine("--- ACTIVE & PAST CHALLENGES ---");
            if (challenges.Count == 0)
            {
                sbContext.AppendLine("No challenges assigned yet.");
            }
            else
            {
                foreach (var challenge in challenges)
                {
                    snapshots.TryGetValue(challenge.Id, out var snapshot);
                    var completionCount = snapshot?.CompletedCount ?? 0;
                    var completionPercent = studentCount > 0 ? (double)completionCount / studentCount * 100.0 : 0;

                    sbContext.AppendLine($"- Title: {challenge.Title}");
                    sbContext.AppendLine($"  Game Type: {challenge.Game?.Key ?? challenge.GameTemplate?.Code ?? "Unknown"}");
                    sbContext.AppendLine($"  State: {challenge.LifecycleState}");
                    sbContext.AppendLine($"  Completed: {completionCount}/{studentCount} ({completionPercent:F1}%)");
                    sbContext.AppendLine($"  Difficulty: Level {challenge.DifficultyLevel}");
                }
            }
            sbContext.AppendLine();

            sbContext.AppendLine("--- CLASSROOM SPELLING WEAKNESSES ---");
            sbContext.AppendLine($"Total Weak Words Tracked: {weaknessOverview.WeakWordCount}");
            if (weaknessOverview.WeakWords.Count == 0)
            {
                sbContext.AppendLine("No significant spelling weak words identified yet. Class is mastering their words well!");
            }
            else
            {
                var topWeakWords = weaknessOverview.WeakWords
                    .OrderBy(w => w.MasteryScore)
                    .Take(15);

                foreach (var word in topWeakWords)
                {
                    sbContext.AppendLine($"- Word: \"{word.Word}\" | Mastery Score: {word.MasteryScore}% (Attempts: {word.TotalAttempts}, Correct: {word.CorrectAttempts})");
                }
            }

            var systemPrompt = @"You are Professor Vega, an advanced educational AI copilot for primary schools.
Your task is to write a comprehensive, professional, and encouraging Weekly Classroom Performance Report in Markdown format.
The report must include:
1. Executive Summary: High-level overview of classroom engagement, active challenges, and general performance.
2. Key Strengths & Achievements: What went well, which games or challenges have the highest completion rates, and areas of excellence.
3. Attention Areas: Sticking points, lower performing challenges, and specific spelling/pronunciation weak words requiring revision.
4. Recommended Actions: Concrete, actionable recommendations (e.g. customized challenges to assign, student group focus, or words to practice).

Format the report using clean, modern markdown (use titles, lists, bold text, and blockquotes for highlights). Write in professional, friendly English. Keep it structured and easy to digest for educators.";

            var userPrompt = $"Analyze the following classroom performance metrics and generate the Weekly Report:\n\n{sbContext}";

            var aiRequest = new ChallengeGenerationRequest(
                Model: "gemini-3.5-flash",
                SystemPrompt: systemPrompt,
                UserPrompt: userPrompt,
                Temperature: 0.7,
                JsonMode: false
            );

            // 3. Generate report using Gemini API
            var aiResult = await aiGenerationService.GenerateJsonAsync(aiRequest, cancellationToken);
            if (!aiResult.IsSuccess)
            {
                await aiAuditService.FailAsync(request.AuditLogId, null, new[] { aiResult.ErrorMessage ?? "Weekly report AI generation failed." }, cancellationToken);
                return OperationResult<bool>.FailureResult(aiResult.ErrorMessage ?? "Failed to generate report.");
            }

            var reportMarkdown = aiResult.Result.RawResponse;

            // 4. Complete Audit Log and consume quota
            await aiAuditService.CompleteAsync(
                request.AuditLogId,
                reportMarkdown,
                JsonSerializer.Serialize(new { length = reportMarkdown.Length }),
                AiValidationStatuses.Valid,
                Array.Empty<string>(),
                cancellationToken);

            await aiUsageService.ConsumeUsageAsync(
                request.TeacherId,
                AiFeatureTypes.WeeklyReportGeneration,
                "POST /api/v1.1/ai/weekly-report",
                "GEMINI",
                null,
                1,
                true,
                null,
                "classroom",
                request.ClassroomId,
                cancellationToken);

            logger.LogInformation("Background execution completed successfully for Weekly Report. AuditLogId: {AuditLogId}", request.AuditLogId);
            return OperationResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during weekly report generation background job for classroom {ClassroomId}", request.ClassroomId);
            await aiAuditService.FailAsync(request.AuditLogId, null, new[] { ex.Message }, cancellationToken);
            return OperationResult<bool>.FailureResult($"Failed to generate weekly report: {ex.Message}");
        }
    }
}
