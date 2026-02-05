using System.Security.Cryptography;
using System.Text;
using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Models.Common;
using Mediator;
using Microsoft.Extensions.Logging;

namespace CleanArc.Application.Features.Users.Commands.ForgotPassword;

internal class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand, OperationResult<bool>>
{
  private readonly IAppUserManager _userManager;
  private readonly ILogger<RequestPasswordResetCommandHandler> _logger;

  public RequestPasswordResetCommandHandler(
      IAppUserManager userManager,
      ILogger<RequestPasswordResetCommandHandler> logger)
  {
    _userManager = userManager;
    _logger = logger;
  }

  public async ValueTask<OperationResult<bool>> Handle(
      RequestPasswordResetCommand request,
      CancellationToken cancellationToken)
  {
    // Find user by email (case-insensitive)
    var user = await _userManager.FindUserByEmail(request.Email);

    if (user == null)
    {
      // Security: Don't reveal if email exists in system
      _logger.LogWarning($"Password reset requested for non-existent email: {request.Email}");
      return OperationResult<bool>.SuccessResult(true);
    }

    // Generate reset token (valid for 24 hours). Store only the hash.
    var resetToken = await _userManager.GeneratePasswordResetToken(user);
    var resetTokenHash = HashToken(resetToken);
    user.PasswordResetTokenHash = resetTokenHash;
    user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(24);
    user.PasswordResetTokenUsed = false;

    var updateResult = await _userManager.UpdateUser(user);

    if (!updateResult.Succeeded)
    {
      _logger.LogError($"Failed to generate reset token for user: {user.Email}");
      return OperationResult<bool>.FailureResult("Failed to initiate password reset");
    }

    // TODO: Send email with reset link and token
    // Format: https://yourapp.com/reset-password?token={resetToken}&email={email}
    // NOTE: Logging raw tokens is only acceptable in non-production environments.
    _logger.LogInformation($"Password reset token generated for user: {user.Email}. Token: {resetToken} (expires at {user.PasswordResetTokenExpiresAt})");

    return OperationResult<bool>.SuccessResult(true);
  }

  private static string HashToken(string token)
  {
    var bytes = Encoding.UTF8.GetBytes(token);
    var hash = SHA256.HashData(bytes);
    return Convert.ToBase64String(hash);
  }
}
