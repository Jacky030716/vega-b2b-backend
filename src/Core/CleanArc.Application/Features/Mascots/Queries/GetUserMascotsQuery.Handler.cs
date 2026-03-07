using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Mascots.Queries;

internal class GetUserMascotsQueryHandler : IRequestHandler<GetUserMascotsQuery, OperationResult<UserMascotsResult>>
{
  private readonly IUnitOfWork _unitOfWork;

  public GetUserMascotsQueryHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<UserMascotsResult>> Handle(GetUserMascotsQuery request, CancellationToken cancellationToken)
  {
    var userMascots = await _unitOfWork.MascotRepository.GetUserMascotsAsync(request.UserId);
    var equipped = await _unitOfWork.MascotRepository.GetEquippedMascotAsync(request.UserId);

    var result = userMascots.Select(um => new UserMascotDto(
        um.MascotId, um.Mascot.Name, um.Mascot.ImageUrl, um.IsEquipped, um.UnlockedAt)).ToList();

    return OperationResult<UserMascotsResult>.SuccessResult(
        new UserMascotsResult(result, equipped?.MascotId));
  }
}
