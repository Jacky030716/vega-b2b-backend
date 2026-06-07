using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

#nullable enable

namespace CleanArc.Web.Api.Contracts.Requests.Games;

public class GenerateAiChallengeDraftRequest
{
  [FromForm(Name = "classroomId")]
  public int ClassroomId { get; set; }

  [FromForm(Name = "prompt")]
  public string? Prompt { get; set; }

  [FromForm(Name = "syllabusFile")]
  public IFormFile? SyllabusFile { get; set; }

  [FromForm(Name = "moduleId")]
  public int? ModuleId { get; set; }

  [FromForm(Name = "mode")]
  public string? Mode { get; set; }
}
