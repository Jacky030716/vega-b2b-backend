using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Mascots.Queries;

internal class GetAllMascotsQueryHandler : IRequestHandler<GetAllMascotsQuery, OperationResult<List<MascotDto>>>
{
  private readonly IUnitOfWork _unitOfWork;

  public GetAllMascotsQueryHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<List<MascotDto>>> Handle(GetAllMascotsQuery request, CancellationToken cancellationToken)
  {
    var mascots = await _unitOfWork.MascotRepository.GetAllMascotsAsync();
    var result = mascots.Select(m => new MascotDto(m.Id, m.Name, m.ImageUrl, m.Description, m.IsDefault, m.UnlockCondition)).ToList();
    return OperationResult<List<MascotDto>>.SuccessResult(result);
  }
}
