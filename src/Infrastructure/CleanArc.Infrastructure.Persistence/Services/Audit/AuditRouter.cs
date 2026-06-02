using System.Text.RegularExpressions;
using CleanArc.Application.Contracts.Audit;

namespace CleanArc.Infrastructure.Persistence.Services.Audit;

public sealed class AuditRouter : IAuditRouter
{
    public AuditRouterResult Route(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return new AuditRouterResult(AuditIntentTypes.Unknown, new AuditRouteParameters());
        }

        var normalized = question.Trim().ToLowerInvariant();
        var intent = DetectIntent(normalized);
        var parameters = ExtractParameters(question);

        return new AuditRouterResult(intent, parameters);
    }

    internal static string DetectIntent(string normalizedQuestion)
    {
        if (MatchesAny(normalizedQuestion,
                "weak word", "weak words", "problematic word", "problematic words",
                "causing problem", "causing problems", "spelling weakness", "spelling weaknesses",
                "difficult word", "difficult words", "words students struggle", "struggling with words",
                "which words", "what words", "vocabulary problem", "vocabulary problems"))
        {
            return AuditIntentTypes.WeakWordAnalysis;
        }

        if (MatchesModuleHealthIntent(normalizedQuestion))
        {
            return AuditIntentTypes.ModuleHealth;
        }

        if (MatchesAny(normalizedQuestion,
                "classroom health", "class health", "classroom status", "class status",
                "how is the class", "how is the classroom", "overall class", "classroom overview",
                "classroom performance"))
        {
            return AuditIntentTypes.ClassroomHealth;
        }

        if (MatchesAny(normalizedQuestion,
                "struggling student", "struggling students", "student performance", "students performance",
                "underperforming", "low performing", "low-performing", "students need help",
                "show struggling", "who is struggling", "who are struggling", "at-risk student", "at risk student"))
        {
            return AuditIntentTypes.StudentPerformance;
        }

        return AuditIntentTypes.Unknown;
    }

    internal static AuditRouteParameters ExtractParameters(string rawQuestion)
    {
        var classroomId = TryExtractScopedId(rawQuestion, "classroom", "class");
        var studentId = TryExtractScopedId(rawQuestion, "student", "user");
        var moduleId = TryExtractScopedId(rawQuestion, "module", "unit");

        return new AuditRouteParameters(classroomId, studentId, moduleId);
    }

    private static bool MatchesAny(string text, params string[] phrases)
        => phrases.Any(phrase => text.Contains(phrase, StringComparison.Ordinal));

    private static bool MatchesModuleHealthIntent(string text)
    {
        if (MatchesAny(text,
                "module health", "unit health", "module status", "unit status",
                "module performance", "unit performance", "how is the module", "how is the unit",
                "how is module", "how is unit", "module progress", "unit progress"))
        {
            return true;
        }

        var mentionsModule = text.Contains("module", StringComparison.Ordinal)
                             || text.Contains("unit", StringComparison.Ordinal);
        var mentionsPerformance = text.Contains("performing", StringComparison.Ordinal)
                                  || text.Contains("performance", StringComparison.Ordinal)
                                  || text.Contains("progress", StringComparison.Ordinal)
                                  || text.Contains("health", StringComparison.Ordinal);

        return mentionsModule && mentionsPerformance;
    }

    private static int? TryExtractScopedId(string question, params string[] scopes)
    {
        foreach (var scope in scopes)
        {
            var match = Regex.Match(
                question,
                $@"\b{scope}\s*#?(?<id>\d+)\b",
                RegexOptions.IgnoreCase);
            if (match.Success)
                return int.Parse(match.Groups["id"].Value);
        }

        return null;
    }
}
