using CleanArc.Application.Contracts.DTOs.User;
using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Models.Common;
using Mediator;
using Microsoft.Extensions.Logging;

namespace CleanArc.Application.Features.Users.Commands.UpdateUserProfile;

internal class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, OperationResult<UpdateUserProfileResponse>>
{
  private readonly IAppUserManager _userManager;
  private readonly ILogger<UpdateUserProfileCommandHandler> _logger;

  public UpdateUserProfileCommandHandler(
      IAppUserManager userManager,
      ILogger<UpdateUserProfileCommandHandler> logger)
  {
    _userManager = userManager;
    _logger = logger;
  }

  public async ValueTask<OperationResult<UpdateUserProfileResponse>> Handle(
      UpdateUserProfileCommand request,
      CancellationToken cancellationToken)
  {
    // Fetch user and verify existence
    var user = await _userManager.GetUserById(request.UserId);

    if (user == null)
    {
      _logger.LogWarning($"Update profile attempt for non-existent user ID: {request.UserId}");
      return OperationResult<UpdateUserProfileResponse>.FailureResult("User not found");
    }

    var userName = request.Profile.UserName.Trim();
    var email = request.Profile.Email.Trim();
    var phoneNumber = string.IsNullOrWhiteSpace(request.Profile.PhoneNumber)
        ? null
        : request.Profile.PhoneNumber.Trim();

    if (!string.Equals(user.UserName, userName, StringComparison.OrdinalIgnoreCase) &&
        await _userManager.IsExistUserName(userName))
    {
      return OperationResult<UpdateUserProfileResponse>.FailureResult("Username is already in use.");
    }

    if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
    {
      var existingEmailUser = await _userManager.FindUserByEmail(email);
      if (existingEmailUser is not null && existingEmailUser.Id != user.Id)
      {
        return OperationResult<UpdateUserProfileResponse>.FailureResult("Email is already in use.");
      }
    }

    if (!string.IsNullOrEmpty(phoneNumber) &&
        !string.Equals(user.PhoneNumber, phoneNumber, StringComparison.Ordinal) &&
        await _userManager.IsExistUser(phoneNumber))
    {
      return OperationResult<UpdateUserProfileResponse>.FailureResult("Phone number is already in use.");
    }

    user.Name = request.Profile.Name.Trim();
    user.FamilyName = request.Profile.FamilyName.Trim();
    user.UserName = userName;
    user.Email = email;
    user.PhoneNumber = phoneNumber;
    if (TryNormalizeAvatarItemId(request.Profile.AvatarId, out var normalizedAvatarItemId))
    {
      user.AvatarId = normalizedAvatarItemId;
    }

    // Update user in database
    var updateResult = await _userManager.UpdateUser(user);

    if (!updateResult.Succeeded)
    {
      _logger.LogError($"Failed to update profile for user ID {request.UserId}: {string.Join(", ", updateResult.Errors.Select(e => e.Description))}");
      return OperationResult<UpdateUserProfileResponse>.FailureResult(
          $"Failed to update profile: {string.Join(", ", updateResult.Errors.Select(e => e.Description))}");
    }

    _logger.LogInformation($"User profile updated successfully for user ID: {request.UserId}");

    return OperationResult<UpdateUserProfileResponse>.SuccessResult(new UpdateUserProfileResponse
    {
      UserId = request.UserId,
      Message = "Profile updated successfully"
    });
  }

  private static bool TryNormalizeAvatarItemId(string? rawAvatarId, out string normalizedAvatarItemId)
  {
    normalizedAvatarItemId = string.Empty;
    if (string.IsNullOrWhiteSpace(rawAvatarId))
      return false;

    var candidate = rawAvatarId.Trim();

    if (int.TryParse(candidate, out var avatarItemId) && avatarItemId >= 0)
    {
      normalizedAvatarItemId = avatarItemId.ToString();
      return true;
    }

    return false;
  }
}
