using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Mascots.Commands;

public record EquipMascotCommand(int UserId, int MascotId) : IRequest<OperationResult<bool>>;
