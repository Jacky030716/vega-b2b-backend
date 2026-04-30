using System.Text.Json;
using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Domain.Entities.AI;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Services.AI;

public sealed class AiAuditService(ApplicationDbContext dbContext) : IAiAuditService
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public async Task<int> StartAsync(AiAuditStartRequest request, CancellationToken cancellationToken)
  {
    var entry = new AiAuditLog
    {
      UseCase = request.UseCase,
      Provider = request.Provider,
      ModelName = request.ModelName,
      PromptVersion = request.PromptVersion,
      InputPayloadJson = EnsureJsonObject(request.InputPayloadJson),
      ValidationStatus = AiValidationStatuses.Pending,
      ValidationErrorsJson = "[]",
      RelatedUserId = request.RelatedUserId,
      RelatedClassroomId = request.RelatedClassroomId,
      RelatedModuleId = request.RelatedModuleId,
      RelatedChallengeId = request.RelatedChallengeId,
      CreatedAt = DateTime.UtcNow
    };

    dbContext.AiAuditLogs.Add(entry);
    await dbContext.SaveChangesAsync(cancellationToken);
    return entry.Id;
  }

  public async Task CompleteAsync(
    int auditLogId,
    string rawOutputJson,
    string parsedOutputJson,
    string validationStatus,
    IReadOnlyList<string> validationErrors,
    CancellationToken cancellationToken)
  {
    var entry = await GetEntryAsync(auditLogId, cancellationToken);
    entry.RawOutputJson = EnsureJsonValue(rawOutputJson);
    entry.ParsedOutputJson = EnsureJsonValue(parsedOutputJson);
    entry.ValidationStatus = validationStatus;
    entry.ValidationErrorsJson = JsonSerializer.Serialize(validationErrors, JsonOptions);
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task FailAsync(
    int auditLogId,
    string? rawOutputJson,
    IReadOnlyList<string> validationErrors,
    CancellationToken cancellationToken)
  {
    var entry = await GetEntryAsync(auditLogId, cancellationToken);
    entry.RawOutputJson = string.IsNullOrWhiteSpace(rawOutputJson) ? null : EnsureJsonValue(rawOutputJson);
    entry.ValidationStatus = AiValidationStatuses.Failed;
    entry.ValidationErrorsJson = JsonSerializer.Serialize(validationErrors, JsonOptions);
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task AttachChallengeAsync(int auditLogId, int challengeId, CancellationToken cancellationToken)
  {
    var entry = await GetEntryAsync(auditLogId, cancellationToken);
    entry.RelatedChallengeId = challengeId;
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  private async Task<AiAuditLog> GetEntryAsync(int auditLogId, CancellationToken cancellationToken)
    => await dbContext.AiAuditLogs.FirstOrDefaultAsync(x => x.Id == auditLogId, cancellationToken)
       ?? throw new InvalidOperationException("AI audit log not found.");

  private static string EnsureJsonObject(string value)
  {
    if (string.IsNullOrWhiteSpace(value))
      return "{}";

    return IsValidJson(value) ? value : JsonSerializer.Serialize(new { value }, JsonOptions);
  }

  private static string EnsureJsonValue(string value)
  {
    if (string.IsNullOrWhiteSpace(value))
      return "null";

    return IsValidJson(value) ? value : JsonSerializer.Serialize(new { value }, JsonOptions);
  }

  private static bool IsValidJson(string value)
  {
    try
    {
      using var _ = JsonDocument.Parse(value);
      return true;
    }
    catch
    {
      return false;
    }
  }
}
