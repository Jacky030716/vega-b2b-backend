using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Application.Models.Common;
using CleanArc.Infrastructure.Persistence.Services.AI;
using CleanArc.Infrastructure.Persistence.Services.Audit;
using NSubstitute;

namespace CleanArc.Tests.Setup.Features.Audit;

public class AuditFindingsSummarizerTests
{
    [Theory]
    [InlineData("""{"summary":"Five students struggle with keluarga and membantu."}""", "Five students struggle with keluarga and membantu.")]
    [InlineData("```json\n{\"summary\":\"Module progress is on track.\"}\n```", "Module progress is on track.")]
    public void ParseSummary_ExtractsSummaryText(string raw, string expected)
    {
        var result = AuditFindingsSummarizer.ParseSummary(raw);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task SummarizeAsync_UsesFindingsOnlyPrompt()
    {
        var ai = Substitute.For<IAiGenerationService>();
        ai.GenerateJsonAsync(Arg.Any<ChallengeGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(OperationResult<ChallengeGenerationResult>.SuccessResult(
                new ChallengeGenerationResult("""{"summary":"Two weak words affect five students."}""")));

        var summarizer = new AuditFindingsSummarizer(ai, new AiPromptRegistry());

        var result = await summarizer.SummarizeAsync(
            "Which words are causing problems?",
            """{"weakWords":["keluarga","membantu"],"affectedStudents":5}""",
            CancellationToken.None);

        Assert.Equal("Two weak words affect five students.", result);

        await ai.Received(1).GenerateJsonAsync(
            Arg.Is<ChallengeGenerationRequest>(request =>
                request.UserPrompt.Contains("keluarga")
                && request.UserPrompt.Contains("Which words are causing problems?")
                && request.SystemPrompt.Contains("authoritative audit findings")),
            Arg.Any<CancellationToken>());
    }
}
