using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Games.Queries;

// ─── DTOs ────────────────────────────────────────────────────────────────────

public record GameDto(
    int Id,
    string Key,
    string Name,
    string Description,
    string ImageUrl,
    string Category,
    string SkillsTaught
);

// ─── Query ───────────────────────────────────────────────────────────────────

public record GetGamesQuery() : IRequest<OperationResult<List<GameDto>>>;
