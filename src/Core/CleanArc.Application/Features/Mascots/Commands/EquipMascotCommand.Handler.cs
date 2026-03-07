using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Mascots.Commands;

internal class EquipMascotCommandHandler : IRequestHandler<EquipMascotCommand, OperationResult<bool>>
{
  private readonly IUnitOfWork _unitOfWork;

  public EquipMascotCommandHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<bool>> Handle(EquipMascotCommand request, CancellationToken cancellationToken)
  {
    await _unitOfWork.MascotRepository.EquipMascotAsync(request.UserId, request.MascotId);
    return OperationResult<bool>.SuccessResult(true);
  }
}
