using System.Collections.Generic;
using System.Text.Json;
using CleanArc.Application.Features.Classrooms.Commands;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

#nullable enable

namespace CleanArc.Web.Api.Contracts.Requests.Classrooms;

public record CreateClassroomRequest(string Name, string Description, JsonElement? Thumbnail);

public record UpdateClassroomRequest(string Name, string? Description, JsonElement? Thumbnail = null);

public record JoinClassroomRequest(string JoinCode);

public class SetupClassroomWizardRequest
{
  [FromForm(Name = "className")]
  public string ClassName { get; set; } = string.Empty;

  [FromForm(Name = "subject")]
  public string Subject { get; set; } = string.Empty;

  [FromForm(Name = "yearLevel")]
  public int? YearLevel { get; set; }

  [FromForm(Name = "subjects")]
  public List<string>? Subjects { get; set; }

  [FromForm(Name = "gameKey")]
  public string? GameKey { get; set; }

  [FromForm(Name = "challengeId")]
  public int? ChallengeId { get; set; }

  [FromForm(Name = "csvContent")]
  public string CsvContent { get; set; } = string.Empty;

  [FromForm(Name = "csvFile")]
  public IFormFile? CsvFile { get; set; }
}
