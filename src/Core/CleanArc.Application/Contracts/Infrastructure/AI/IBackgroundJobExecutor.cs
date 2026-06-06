using System.Threading.Tasks;
using CleanArc.Application.Contracts.Infrastructure.Documents;

namespace CleanArc.Application.Contracts.Infrastructure.AI;

public interface IBackgroundJobExecutor
{
    Task ExecuteWeeklyReportJobAsync(int auditLogId, int classroomId, int teacherId);
    
    Task ExecuteChallengeDraftJobAsync(
        int auditLogId, 
        string gameKey, 
        int userId, 
        int classroomId, 
        string prompt, 
        ChallengeDocumentPayload? syllabusFile);
        
    Task ExecuteClassroomThumbnailJobAsync(
        int auditLogId, 
        int userId, 
        string classroomName, 
        int yearLevel, 
        string[] subjects, 
        string? description, 
        string thumbnailPrompt);
}
