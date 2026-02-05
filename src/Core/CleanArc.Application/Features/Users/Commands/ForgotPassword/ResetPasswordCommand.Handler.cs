using System.Security.Cryptography;
using System.Text;
using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Models.Common;
using Mediator;
using Microsoft.Extensions.Logging;

namespace CleanArc.Application.Features.Users.Commands.ForgotPassword;

internal class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, OperationResult<bool>>
{
  private readonly IAppUserManager _userManager;
  private readonly ILogger<ResetPasswordCommandHandler> _logger;

  public ResetPasswordCommandHandler(
      IAppUserManager userManager,
      ILogger<ResetPasswordCommandHandler> logger)
  {
    _userManager = userManager;
    _logger = logger;
  }

  public async ValueTask<OperationResult<bool>> Handle(
      ResetPasswordCommand request,
      CancellationToken cancellationToken)
  {
    // Find user by email
    var user = await _userManager.FindUserByEmail(request.Email);

    if (user == null)
    {
      _logger.LogWarning($"Password reset attempted for non-existent email: {request.Email}");
      return OperationResult<bool>.FailureResult("Invalid email or token");
    }

    // Validate reset token (compare hash)
    var incomingTokenHash = HashToken(request.ResetToken);
    if (string.IsNullOrEmpty(user.PasswordResetTokenHash) || user.PasswordResetTokenHash != incomingTokenHash)
    {
      _logger.LogWarning($"Invalid reset token provided for user: {user.Email}");
      return OperationResult<bool>.FailureResult("Invalid token");
    }

    // Check token expiration
    if (user.PasswordResetTokenExpiresAt == null || user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
    {
      _logger.LogWarning($"Expired reset token used for user: {user.Email}");
      return OperationResult<bool>.FailureResult("Reset token has expired");
    }

    // Prevent token reuse
    if (user.PasswordResetTokenUsed)
    {
      _logger.LogWarning($"Already-used reset token attempted for user: {user.Email}");
      return OperationResult<bool>.FailureResult("This reset token has already been used");
    }

    // Reset password
    var resetResult = await _userManager.ResetPassword(user, request.ResetToken, request.NewPassword);

    if (!resetResult.Succeeded)
    {
      _logger.LogError($"Failed to reset password for user: {user.Email}");
      return OperationResult<bool>.FailureResult(
          $"Failed to reset password: {string.Join(", ", resetResult.Errors.Select(e => e.Description))}");
    }

    // Mark token as used
    user.PasswordResetTokenUsed = true;
    user.PasswordResetTokenHash = null;
    user.PasswordResetTokenExpiresAt = null;

    var updateResult = await _userManager.UpdateUser(user);

    if (!updateResult.Succeeded)
    {
      _logger.LogError($"Failed to update reset token status for user: {user.Email}");
      return OperationResult<bool>.FailureResult("Password reset completed but status update failed");
    }

    _logger.LogInformation($"Password successfully reset for user: {user.Email}");
    return OperationResult<bool>.SuccessResult(true);
  }

  private static string HashToken(string token)
  {
    var bytes = Encoding.UTF8.GetBytes(token);
    var hash = SHA256.HashData(bytes);
    return Convert.ToBase64String(hash);
  }
}
