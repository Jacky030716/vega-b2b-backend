using CleanArc.Application.Contracts;
using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Models.Common;
using CleanArc.Application.Models.Jwt;
using Mediator;
using Microsoft.Extensions.Logging;

namespace CleanArc.Application.Features.Users.Queries.PasswordLogin;

internal class UserPasswordLoginQueryHandler : IRequestHandler<UserPasswordLoginQuery, OperationResult<AccessToken>>
{
  private readonly IJwtService _jwtService;
  private readonly IAppUserManager _userManager;
  private readonly ILogger<UserPasswordLoginQueryHandler> _logger;

  public UserPasswordLoginQueryHandler(
      IJwtService jwtService,
      IAppUserManager userManager,
      ILogger<UserPasswordLoginQueryHandler> logger)
  {
    _jwtService = jwtService;
    _userManager = userManager;
    _logger = logger;
  }

  public async ValueTask<OperationResult<AccessToken>> Handle(
      UserPasswordLoginQuery request,
      CancellationToken cancellationToken)
  {
    // Support both username and email login
    var user = await _userManager.GetByUserName(request.UserName);

    if (user == null)
    {
      // Try lookup by email if username lookup fails
      user = await _userManager.FindUserByEmail(request.UserName);
    }

    if (user == null)
    {
      _logger.LogWarning("Invalid login attempt for username/email: {UserName}", request.UserName);
      return OperationResult<AccessToken>.UnauthorizedResult("Invalid credentials");
    }

    if (await _userManager.IsUserLockedOutAsync(user))
    {
      _logger.LogWarning("Locked out user attempted login: {UserName}", request.UserName);
      return OperationResult<AccessToken>.FailureResult("User is locked out. Please try again later.");
    }

    var loginResult = await _userManager.AdminLogin(user, request.Password);

    if (!loginResult.Succeeded)
    {
      await _userManager.IncrementAccessFailedCountAsync(user);
      _logger.LogWarning("Invalid password for username: {UserName}", request.UserName);
      return OperationResult<AccessToken>.UnauthorizedResult("Invalid credentials");
    }

    await _userManager.ResetUserLockoutAsync(user);

    var roles = await _userManager.GetUserRolesAsync(user);
    var hasAllowedRole = roles.Any(role =>
        role.Equals("teacher", StringComparison.OrdinalIgnoreCase)
        || role.Equals("admin", StringComparison.OrdinalIgnoreCase)
        || role.Equals("InstitutionAdmin", StringComparison.OrdinalIgnoreCase));

    if (!hasAllowedRole)
    {
      _logger.LogWarning(
          "Password login rejected for user {UserName} due to unsupported role set: {Roles}",
          request.UserName,
          string.Join(",", roles));
      return OperationResult<AccessToken>.UnauthorizedResult("Invalid credentials");
    }

    var token = await _jwtService.GenerateAsync(user);
    return OperationResult<AccessToken>.SuccessResult(token);
  }
}
