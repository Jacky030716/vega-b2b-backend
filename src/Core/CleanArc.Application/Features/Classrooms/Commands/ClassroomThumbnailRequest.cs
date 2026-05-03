namespace CleanArc.Application.Features.Classrooms.Commands;

public record ClassroomThumbnailRequest(
  string Type,
  string? AssetId = null,
  string? Url = null,
  string? Prompt = null);
