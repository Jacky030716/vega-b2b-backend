using CleanArc.Application.Contracts.Audit;
using CleanArc.Infrastructure.Persistence.Services.Audit;

namespace CleanArc.Tests.Setup.Features.Audit;

public class AuditRouterTests
{
    private readonly AuditRouter _router = new();

    [Theory]
    [InlineData("Show struggling students", AuditIntentTypes.StudentPerformance)]
    [InlineData("Which words are causing problems?", AuditIntentTypes.WeakWordAnalysis)]
    [InlineData("What is the classroom health?", AuditIntentTypes.ClassroomHealth)]
    [InlineData("How is module 5 performing?", AuditIntentTypes.ModuleHealth)]
    [InlineData("List all inactive teachers", AuditIntentTypes.Unknown)]
    [InlineData("How many seats are used?", AuditIntentTypes.Unknown)]
    public void Route_DetectsExpectedIntent(string question, string expectedIntent)
    {
        var result = _router.Route(question);

        Assert.Equal(expectedIntent, result.Intent);
    }

    [Fact]
    public void Route_ExtractsScopedIds()
    {
        var result = _router.Route("Show weak words for classroom 12 module 3");

        Assert.Equal(AuditIntentTypes.WeakWordAnalysis, result.Intent);
        Assert.Equal(12, result.Parameters.ClassroomId);
        Assert.Equal(3, result.Parameters.ModuleId);
    }

    [Theory]
    [InlineData("struggling student", AuditIntentTypes.StudentPerformance)]
    [InlineData("problematic words in class", AuditIntentTypes.WeakWordAnalysis)]
    [InlineData("unit health report", AuditIntentTypes.ModuleHealth)]
    public void DetectIntent_MatchesKeywordPhrases(string question, string expectedIntent)
    {
        var intent = AuditRouter.DetectIntent(question.ToLowerInvariant());

        Assert.Equal(expectedIntent, intent);
    }
}
