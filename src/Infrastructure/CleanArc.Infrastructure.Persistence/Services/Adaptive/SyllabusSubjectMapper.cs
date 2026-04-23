namespace CleanArc.Infrastructure.Persistence.Services.Adaptive;

internal static class SyllabusSubjectMapper
{
    public static string? NormalizeSubject(string? rawSubject)
    {
        if (string.IsNullOrWhiteSpace(rawSubject))
            return null;

        var value = rawSubject.Trim().ToLowerInvariant();

        if (value.Contains("bahasa melayu") || value.Contains("malay") || value == "bm")
            return "Bahasa Melayu";

        if (value.Contains("english") || value == "bi")
            return "English";

        return rawSubject.Trim();
    }
}
