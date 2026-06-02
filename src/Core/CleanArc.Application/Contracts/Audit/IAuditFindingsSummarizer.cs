namespace CleanArc.Application.Contracts.Audit;

public interface IAuditFindingsSummarizer
{
    /// <summary>
    /// Produces a short administrator-facing summary from authoritative audit findings JSON only.
    /// Returns null when generation fails; callers should still return structured findings.
    /// </summary>
    Task<string?> SummarizeAsync(
        string administratorQuestion,
        string findingsJson,
        CancellationToken cancellationToken);
}
