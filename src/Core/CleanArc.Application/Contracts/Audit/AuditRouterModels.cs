namespace CleanArc.Application.Contracts.Audit;

public sealed record AuditRouterResult(
    string Intent,
    AuditRouteParameters Parameters);

public sealed record AuditRouteParameters(
    int? ClassroomId = null,
    int? StudentId = null,
    int? ModuleId = null);

public sealed record AuditRouteRequest(
    int InstitutionId,
    int? UserId,
    string Question,
    IReadOnlyList<AuditRouteUserContext> Users);

public sealed record AuditRouteUserContext(
    int Id,
    string UserName,
    string Role,
    string? ClassName,
    string? FullName = null);

public sealed record AuditRouteResponse(
    string Intent,
    string AnswerJson,
    IReadOnlyList<int> MatchedUserIds);
