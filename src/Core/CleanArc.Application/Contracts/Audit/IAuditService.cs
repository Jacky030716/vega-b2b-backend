namespace CleanArc.Application.Contracts.Audit;

public interface IAuditService
{
    Task<ClassroomHealthDto> GetClassroomHealthAsync(int classroomId, CancellationToken cancellationToken);

    Task<StudentPerformanceAuditDto> GetStudentPerformanceAsync(int studentId, CancellationToken cancellationToken);

    Task<ModuleHealthDto> GetModuleHealthAsync(int classroomId, int moduleId, CancellationToken cancellationToken);

    Task<WeakWordsAuditDto> GetWeakWordsAsync(int classroomId, int? moduleId, CancellationToken cancellationToken);
}
