using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Activity.Queries;

public record GetRecentActivityQuery(int UserId, int Count = 20) : IRequest<OperationResult<List<ActivityDto>>>;

public record ActivityDto(int Id, string Type, string Title, string Description, int? PointsEarned, string? ReferenceId, DateTime CreatedAt);
