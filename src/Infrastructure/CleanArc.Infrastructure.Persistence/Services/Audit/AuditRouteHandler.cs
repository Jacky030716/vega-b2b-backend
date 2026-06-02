using System.Text.Json;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Application.Contracts.Audit;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Domain.Entities.Classroom;

namespace CleanArc.Infrastructure.Persistence.Services.Audit;

public sealed class AuditRouteHandler(
    IAuditService auditService,
    IClassroomRepository classroomRepository,
    IAdaptiveAnalyticsService adaptiveAnalyticsService) : IAuditRouteHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task<AuditRouteResponse?> TryHandleAsync(
        AuditRouterResult route,
        AuditRouteRequest request,
        CancellationToken cancellationToken)
    {
        if (string.Equals(route.Intent, AuditIntentTypes.Unknown, StringComparison.Ordinal))
            return null;

        var classrooms = await ResolveClassroomsAsync(request, cancellationToken);
        if (classrooms.Count == 0)
            return null;

        return route.Intent switch
        {
            AuditIntentTypes.ClassroomHealth => await HandleClassroomHealthAsync(route, classrooms, cancellationToken),
            AuditIntentTypes.StudentPerformance => await HandleStudentPerformanceAsync(route, request, classrooms, cancellationToken),
            AuditIntentTypes.ModuleHealth => await HandleModuleHealthAsync(route, classrooms, cancellationToken),
            AuditIntentTypes.WeakWordAnalysis => await HandleWeakWordAnalysisAsync(route, classrooms, cancellationToken),
            _ => null
        };
    }

    private async Task<AuditRouteResponse> HandleClassroomHealthAsync(
        AuditRouterResult route,
        IReadOnlyList<Classroom> classrooms,
        CancellationToken cancellationToken)
    {
        var targets = ResolveTargetClassrooms(route.Parameters, classrooms);
        var results = new List<ClassroomHealthDto>();
        foreach (var classroom in targets)
        {
            results.Add(await auditService.GetClassroomHealthAsync(classroom.Id, cancellationToken));
        }

        return BuildResponse(
            AuditIntentTypes.ClassroomHealth,
            new { classrooms = results },
            []);
    }

    private async Task<AuditRouteResponse> HandleStudentPerformanceAsync(
        AuditRouterResult route,
        AuditRouteRequest request,
        IReadOnlyList<Classroom> classrooms,
        CancellationToken cancellationToken)
    {
        var students = request.Users
            .Where(user => string.Equals(user.Role, "student", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (route.Parameters.StudentId is int studentId)
        {
            students = students.Where(student => student.Id == studentId).ToList();
        }

        var performances = new List<StudentPerformanceAuditDto>();
        var matchedUserIds = new List<int>();

        foreach (var student in students)
        {
            var performance = await auditService.GetStudentPerformanceAsync(student.Id, cancellationToken);
            if (!IsStruggling(performance) && route.Parameters.StudentId is null)
                continue;

            performance = performance with { StudentName = student.FullName ?? student.UserName };
            performances.Add(performance);
            if (IsStruggling(performance))
                matchedUserIds.Add(student.Id);
        }

        if (performances.Count == 0 && students.Count > 0)
        {
            var fallbackStudent = students[0];
            var performance = await auditService.GetStudentPerformanceAsync(fallbackStudent.Id, cancellationToken);
            performance = performance with { StudentName = fallbackStudent.FullName ?? fallbackStudent.UserName };
            performances.Add(performance);
            if (IsStruggling(performances[0]))
                matchedUserIds.Add(fallbackStudent.Id);
        }

        if (route.Parameters.ClassroomId is int classroomFilterId)
        {
            var memberIds = await GetClassroomStudentIdsAsync(classroomFilterId, cancellationToken);
            performances = performances.Where(performance => memberIds.Contains(performance.StudentId)).ToList();
            matchedUserIds = matchedUserIds.Where(memberIds.Contains).ToList();
        }
        else if (classrooms.Count == 1)
        {
            var memberIds = await GetClassroomStudentIdsAsync(classrooms[0].Id, cancellationToken);
            performances = performances.Where(performance => memberIds.Contains(performance.StudentId)).ToList();
            matchedUserIds = matchedUserIds.Where(memberIds.Contains).ToList();
        }

        return BuildResponse(
            AuditIntentTypes.StudentPerformance,
            new { students = performances },
            matchedUserIds.Distinct().ToList());
    }

    private async Task<AuditRouteResponse> HandleModuleHealthAsync(
        AuditRouterResult route,
        IReadOnlyList<Classroom> classrooms,
        CancellationToken cancellationToken)
    {
        var targets = ResolveTargetClassrooms(route.Parameters, classrooms);
        var classroom = targets[0];
        var summaries = await adaptiveAnalyticsService.GetModuleProgressSummaryAsync(classroom.Id, cancellationToken);

        if (summaries.Count == 0)
            return null;

        var moduleIds = route.Parameters.ModuleId is int moduleId
            ? summaries.Where(summary => summary.ModuleId == moduleId).Select(summary => summary.ModuleId)
            : summaries.Select(summary => summary.ModuleId);

        var results = new List<ModuleHealthDto>();
        foreach (var id in moduleIds)
        {
            results.Add(await auditService.GetModuleHealthAsync(classroom.Id, id, cancellationToken));
        }

        return BuildResponse(
            AuditIntentTypes.ModuleHealth,
            new { classroomId = classroom.Id, modules = results },
            []);
    }

    private async Task<AuditRouteResponse> HandleWeakWordAnalysisAsync(
        AuditRouterResult route,
        IReadOnlyList<Classroom> classrooms,
        CancellationToken cancellationToken)
    {
        var targets = ResolveTargetClassrooms(route.Parameters, classrooms);
        var results = new List<WeakWordsAuditDto>();
        foreach (var classroom in targets)
        {
            results.Add(await auditService.GetWeakWordsAsync(
                classroom.Id,
                route.Parameters.ModuleId,
                cancellationToken));
        }

        return BuildResponse(
            AuditIntentTypes.WeakWordAnalysis,
            new { classrooms = results },
            []);
    }

    private async Task<List<Classroom>> ResolveClassroomsAsync(
        AuditRouteRequest request,
        CancellationToken cancellationToken)
    {
        if (request.UserId is int userId)
        {
            return await classroomRepository.GetTeacherClassroomsAsync(userId);
        }

        var teacher = request.Users.FirstOrDefault(user =>
            string.Equals(user.Role, "teacher", StringComparison.OrdinalIgnoreCase));
        if (teacher is null)
            return [];

        return await classroomRepository.GetTeacherClassroomsAsync(teacher.Id);
    }

    private static IReadOnlyList<Classroom> ResolveTargetClassrooms(
        AuditRouteParameters parameters,
        IReadOnlyList<Classroom> classrooms)
    {
        if (parameters.ClassroomId is int classroomId)
        {
            var match = classrooms.Where(classroom => classroom.Id == classroomId).ToList();
            return match.Count > 0 ? match : classrooms.Take(1).ToList();
        }

        return classrooms.Count <= 3
            ? classrooms.ToList()
            : classrooms.Take(1).ToList();
    }

    private async Task<HashSet<int>> GetClassroomStudentIdsAsync(int classroomId, CancellationToken cancellationToken)
    {
        var members = await classroomRepository.GetClassroomMembersAsync(classroomId);
        return members.Select(member => member.UserId).ToHashSet();
    }

    private static bool IsStruggling(StudentPerformanceAuditDto performance)
        => performance.WeakWordCount > 0
           || performance.AverageMasteryScore < 65m
           || performance.OverdueReviewCount > 0;

    private static AuditRouteResponse BuildResponse(
        string intent,
        object data,
        IReadOnlyList<int> matchedUserIds)
    {
        var payload = new
        {
            source = "audit_router",
            intent,
            data
        };

        return new AuditRouteResponse(
            intent,
            JsonSerializer.Serialize(payload, JsonOptions),
            matchedUserIds);
    }
}
