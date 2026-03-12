using AutoMapper;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Contracts.DTOs.User;
using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Models.Common;
using Mediator;
using Microsoft.Extensions.Logging;

namespace CleanArc.Application.Features.Users.Queries.GetUserProfile;

internal class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, OperationResult<GetUserProfileResponse>>
{
  private readonly IAppUserManager _userManager;
  private readonly IUnitOfWork _unitOfWork;
  private readonly IMapper _mapper;
  private readonly ILogger<GetUserProfileQueryHandler> _logger;

  public GetUserProfileQueryHandler(
      IAppUserManager userManager,
      IUnitOfWork unitOfWork,
      IMapper mapper,
      ILogger<GetUserProfileQueryHandler> logger)
  {
    _userManager = userManager;
    _unitOfWork = unitOfWork;
    _mapper = mapper;
    _logger = logger;
  }

  public async ValueTask<OperationResult<GetUserProfileResponse>> Handle(
      GetUserProfileQuery request,
      CancellationToken cancellationToken)
  {
    // Retrieve user by ID with minimal database queries
    var user = await _userManager.GetUserById(request.UserId);

    if (user == null)
    {
      _logger.LogWarning($"User profile request for non-existent user ID: {request.UserId}");
      return OperationResult<GetUserProfileResponse>.FailureResult("User not found");
    }

    var userProfile = _mapper.Map<GetUserProfileResponse>(user);

    if (int.TryParse(user.AvatarId, out var avatarItemId) && avatarItemId > 0)
    {
      var avatarItem = await _unitOfWork.ShopRepository.GetShopItemByIdAsync(avatarItemId);
      if (avatarItem != null && string.Equals(avatarItem.Category, "avatar", StringComparison.OrdinalIgnoreCase))
      {
        userProfile.AvatarId = avatarItem.ImageUrl;
      }
      else
      {
        userProfile.AvatarId = "bear";
      }
    }
    else if (string.IsNullOrWhiteSpace(user.AvatarId) || user.AvatarId == "0")
    {
      userProfile.AvatarId = "bear";
    }

    return OperationResult<GetUserProfileResponse>.SuccessResult(userProfile);
  }
}
