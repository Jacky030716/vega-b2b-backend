using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Mascot;
using Mediator;

namespace CleanArc.Application.Features.Mascots.Commands;

internal class CreateMascotCommandHandler : IRequestHandler<CreateMascotCommand, OperationResult<int>>
{
  private readonly IUnitOfWork _unitOfWork;

  public CreateMascotCommandHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<int>> Handle(CreateMascotCommand request, CancellationToken cancellationToken)
  {
    var mascot = new Mascot
    {
      Name = request.Name,
      ImageUrl = request.ImageUrl,
      Description = request.Description,
      IsDefault = request.IsDefault,
      UnlockCondition = request.UnlockCondition
    };

    var created = await _unitOfWork.MascotRepository.CreateMascotAsync(mascot);
    return OperationResult<int>.SuccessResult(created.Id);
  }
}
