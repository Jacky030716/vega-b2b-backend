using Microsoft.AspNetCore.Mvc;

#nullable enable

namespace CleanArc.Web.Api.Contracts.Requests.Classrooms;

public record CreateClassroomRequest(string Name, string Description, string Subject, string? Thumbnail, int? YearLevel);

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
