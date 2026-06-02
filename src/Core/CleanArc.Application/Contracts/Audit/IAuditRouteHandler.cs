namespace CleanArc.Application.Contracts.Audit;

public interface IAuditRouteHandler
{
    Task<AuditRouteResponse?> TryHandleAsync(
        AuditRouterResult route,
        AuditRouteRequest request,
        CancellationToken cancellationToken);
}
