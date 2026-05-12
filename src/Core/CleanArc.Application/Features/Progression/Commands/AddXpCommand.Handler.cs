using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Contracts.Achievements;
using CleanArc.Application.Models.Common;
using Mediator;
using System.Text.Json;

namespace CleanArc.Application.Features.Progression.Commands;

internal class AddXpCommandHandler : IRequestHandler<AddXpCommand, OperationResult<AddXpResult>>
{
  private readonly IUnitOfWork _unitOfWork;
  private readonly IAchievementTrackingService _achievementTrackingService;

  public AddXpCommandHandler(
    IUnitOfWork unitOfWork,
    IAchievementTrackingService achievementTrackingService)
  {
    _unitOfWork = unitOfWork;
    _achievementTrackingService = achievementTrackingService;
  }

  public async ValueTask<OperationResult<AddXpResult>> Handle(AddXpCommand request, CancellationToken cancellationToken)
  {
    var progress = await _unitOfWork.ProgressionRepository.GetOrCreateUserProgressAsync(request.UserId);
    var previousLevel = progress.CurrentLevel;

    await _unitOfWork.ProgressionRepository.AddXpAsync(request.UserId, request.XpAmount);

    // Re-fetch to get updated values
    progress = await _unitOfWork.ProgressionRepository.GetUserProgressAsync(request.UserId);
    var leveledUp = progress.CurrentLevel > previousLevel;

    if (leveledUp)
    {
      await _achievementTrackingService.TrackEventAsync(
        request.UserId,
        "LEVEL_REACHED",
        $"level-reached:{request.UserId}:{progress.CurrentLevel}",
        JsonSerializer.Serialize(new
        {
          level = progress.CurrentLevel,
          previousLevel,
          source = "add_xp",
          xpAmount = request.XpAmount,
        }),
        cancellationToken);
    }

    return OperationResult<AddXpResult>.SuccessResult(
        new AddXpResult(progress.TotalXP, progress.CurrentLevel, leveledUp));
  }
}
