using System.Text.Json;
using CleanArc.Application.Features.Classrooms.Commands;
using Microsoft.AspNetCore.Mvc;

#nullable enable

namespace CleanArc.Web.Api.Contracts.Requests.Classrooms;

public record CreateClassroomRequest(string Name, string Description, string Subject, IReadOnlyList<string>? Subjects, JsonElement? Thumbnail, int? YearLevel);

public record UpdateClassroomRequest(string Name, string Subject, IReadOnlyList<string>? Subjects, int? YearLevel, string? Description, JsonElement? Thumbnail = null);

public record JoinClassroomRequest(string JoinCode);

public class SetupClassroomWizardRequest
{
  [FromForm(Name = "className")]
  public string ClassName { get; set; } = string.Empty;

  [FromForm(Name = "subject")]
  public string Subject { get; set; } = string.Empty;

  [FromForm(Name = "challengeId")]
  public int ChallengeId { get; set; }

  [FromForm(Name = "csvContent")]
  public string CsvContent { get; set; } = string.Empty;
}
