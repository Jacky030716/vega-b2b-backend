using System.Threading.Tasks;
using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Application.Contracts.Infrastructure.Documents;
using CleanArc.Application.Features.Classrooms.Commands;
using CleanArc.Application.Features.Games.Commands;
using Mediator;

namespace CleanArc.Infrastructure.Persistence.Services.AI;

public sealed class BackgroundJobExecutor(ISender sender) : IBackgroundJobExecutor
{
    public async Task ExecuteWeeklyReportJobAsync(int auditLogId, int classroomId, int teacherId)
    {
        await sender.Send(new GenerateWeeklyReportJobCommand(auditLogId, classroomId, teacherId));
    }

    public async Task ExecuteChallengeDraftJobAsync(
        int auditLogId, 
        string gameKey, 
        int userId, 
        int classroomId, 
        string prompt, 
        ChallengeDocumentPayload? syllabusFile,
        int? moduleId = null,
        string? mode = null)
    {
        await sender.Send(new GenerateAiChallengeDraftJobCommand(auditLogId, userId, gameKey, classroomId, prompt, syllabusFile, moduleId, mode));
    }

    public async Task ExecuteClassroomThumbnailJobAsync(
        int auditLogId, 
        int userId, 
        string classroomName, 
        string? description, 
        string thumbnailPrompt)
    {
        await sender.Send(new GenerateClassroomThumbnailJobCommand(auditLogId, userId, classroomName, description, thumbnailPrompt));
    }
}
