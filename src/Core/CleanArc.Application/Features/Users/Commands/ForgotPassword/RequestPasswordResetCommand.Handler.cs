using System.Security.Cryptography;
using System.Text;
using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Contracts.Notifications;
using CleanArc.Application.Models.Common;
using Mediator;
using Microsoft.Extensions.Logging;

namespace CleanArc.Application.Features.Users.Commands.ForgotPassword;

internal class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand, OperationResult<bool>>
{
  private readonly IAppUserManager _userManager;
  private readonly IEmailService _emailService;
  private readonly ILogger<RequestPasswordResetCommandHandler> _logger;

  public RequestPasswordResetCommandHandler(
      IAppUserManager userManager,
      IEmailService emailService,
      ILogger<RequestPasswordResetCommandHandler> logger)
  {
    _userManager = userManager;
    _emailService = emailService;
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

    // Send email with reset link and token
    var userEmail = user.Email ?? request.Email;
    var resetLink = $"https://yourapp.com/reset-password?token={System.Uri.EscapeDataString(resetToken)}&email={System.Uri.EscapeDataString(userEmail)}";
    var emailSubject = "Vega Platform - Password Reset Request";
    var emailBody = $@"
        <h2>Password Reset Request</h2>
        <p>Hello,</p>
        <p>You requested a password reset for your Vega account. Please click the link below to reset your password:</p>
        <p>
          <a href='{resetLink}' style='background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px; display: inline-block;'>Reset Password</a>
        </p>
        <p>If the button above does not work, copy and paste the following URL into your browser:</p>
        <p><a href='{resetLink}'>{resetLink}</a></p>
        <p>This link is valid for 24 hours.</p>
        <p>If you did not request a password reset, please ignore this email.</p>
        <p>Best regards,<br>The Vega Team</p>";

    try
    {
        await _emailService.SendEmailAsync(userEmail, emailSubject, emailBody, isHtml: true);
    }
    catch (System.Exception ex)
    {
        _logger.LogError(ex, $"Failed to send password reset email to: {userEmail}");
    }

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
