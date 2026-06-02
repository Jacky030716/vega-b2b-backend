namespace CleanArc.Application.Contracts.Audit;

public interface IAuditRouter
{
    AuditRouterResult Route(string question);
}
