using CleanArc.Application.Features.Admin.Queries.AskAuditor;

namespace CleanArc.Tests.Setup.Features.Audit;

public class AuditRouterResponseComposerTests
{
    [Fact]
    public void AttachSummary_AddsSummaryProperty()
    {
        const string payload = """{"source":"audit_router","intent":"WEAK_WORD_ANALYSIS","data":{"classrooms":[]}}""";

        var enriched = AuditRouterResponseComposer.AttachSummary(
            payload,
            "Five students are struggling with key vocabulary.");

        Assert.Contains("\"summary\":", enriched);
        Assert.Contains("Five students are struggling", enriched);
        Assert.Contains("WEAK_WORD_ANALYSIS", enriched);
    }

    [Fact]
    public void ExtractFindingsJson_ReturnsDataSection()
    {
        const string payload = """{"source":"audit_router","intent":"WEAK_WORD_ANALYSIS","data":{"classrooms":[{"weakWords":["a"],"affectedStudents":1}]}}""";

        var findings = AuditRouterResponseComposer.ExtractFindingsJson(payload);

        Assert.NotNull(findings);
        Assert.Contains("weakWords", findings);
        Assert.DoesNotContain("audit_router", findings);
    }
}
