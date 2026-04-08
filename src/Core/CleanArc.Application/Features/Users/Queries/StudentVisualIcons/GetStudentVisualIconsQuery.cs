using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Users.Queries.StudentVisualIcons;

public record GetStudentVisualIconsQuery() : IRequest<OperationResult<List<VisualIconDto>>>;

public record VisualIconDto(int Id, string Emoji, string Label);
