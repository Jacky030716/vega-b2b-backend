using System.Text.Json;
using System.Text.Json.Nodes;

namespace CleanArc.Application.Features.Admin.Queries.AskAuditor;

internal static class AuditRouterResponseComposer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string AttachSummary(string answerJson, string? summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
            return answerJson;

        try
        {
            var node = JsonNode.Parse(answerJson) as JsonObject;
            if (node is null)
                return answerJson;

            node["summary"] = summary;
            return node.ToJsonString(JsonOptions);
        }
        catch
        {
            return answerJson;
        }
    }

    public static string? ExtractFindingsJson(string answerJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(answerJson);
            if (doc.RootElement.TryGetProperty("data", out var dataElement))
                return dataElement.GetRawText();
        }
        catch
        {
            return null;
        }

        return null;
    }
}
