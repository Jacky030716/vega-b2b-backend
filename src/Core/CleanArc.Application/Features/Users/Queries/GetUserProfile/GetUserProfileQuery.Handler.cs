using AutoMapper;
using CleanArc.Application.Contracts.DTOs.User;
using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Models.Common;
using Mediator;
using Microsoft.Extensions.Logging;

namespace CleanArc.Application.Features.Users.Queries.GetUserProfile;

internal class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, OperationResult<GetUserProfileResponse>>
{
  private readonly IAppUserManager _userManager;
  private readonly IMapper _mapper;
  private readonly ILogger<GetUserProfileQueryHandler> _logger;

  public GetUserProfileQueryHandler(
      IAppUserManager userManager,
      IMapper mapper,
      ILogger<GetUserProfileQueryHandler> logger)
  {
    _userManager = userManager;
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
    return OperationResult<GetUserProfileResponse>.SuccessResult(userProfile);
  }
}
