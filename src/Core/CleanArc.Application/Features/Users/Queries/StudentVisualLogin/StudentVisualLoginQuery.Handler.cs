using BCrypt.Net;
using CleanArc.Application.Contracts;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Application.Models.Jwt;
using Mediator;
using Microsoft.Extensions.Logging;

namespace CleanArc.Application.Features.Users.Queries.StudentVisualLogin;

internal class StudentVisualLoginQueryHandler : IRequestHandler<StudentVisualLoginQuery, OperationResult<AccessToken>>
{
  private readonly IUnitOfWork _unitOfWork;
  private readonly IJwtService _jwtService;
  private readonly ILogger<StudentVisualLoginQueryHandler> _logger;

  public StudentVisualLoginQueryHandler(
      IUnitOfWork unitOfWork,
      IJwtService jwtService,
      ILogger<StudentVisualLoginQueryHandler> logger)
  {
    _unitOfWork = unitOfWork;
    _jwtService = jwtService;
    _logger = logger;
  }

  public async ValueTask<OperationResult<AccessToken>> Handle(
      StudentVisualLoginQuery request,
      CancellationToken cancellationToken)
  {
    var normalizedLoginCode = request.LoginCode.Trim().ToUpperInvariant();
    var credential = await _unitOfWork.StudentCredentialRepository.GetByLoginCodeAsync(normalizedLoginCode);

    if (credential is null || !credential.IsActive)
    {
      _logger.LogWarning("Student visual login failed: unknown login code {LoginCode}", normalizedLoginCode);
      return OperationResult<AccessToken>.UnauthorizedResult("Invalid credentials");
    }

    // Handle uninitialized accounts that have a placeholder hash
    bool isValidSequence;
    if (credential.VisualPasswordHash == "DEFAULT")
    {
      // Account hasn't had a picture password set yet — allow any sequence
      isValidSequence = !string.IsNullOrWhiteSpace(request.VisualSequence);
      _logger.LogInformation("Student {UserId} has DEFAULT password hash; allowing any sequence.", credential.UserId);
    }
    else
    {
      try
      {
        isValidSequence = BCrypt.Net.BCrypt.Verify(request.VisualSequence, credential.VisualPasswordHash);
      }
      catch (BCrypt.Net.SaltParseException ex)
      {
        _logger.LogError(ex, "Failed to parse BCrypt salt for userId {UserId}", credential.UserId);
        return OperationResult<AccessToken>.UnauthorizedResult("Invalid credentials");
      }
    }

    if (!isValidSequence)
    {
      credential.FailedAttempts += 1;
      credential.LastFailedAt = DateTime.UtcNow;
      await _unitOfWork.StudentCredentialRepository.UpdateAsync(credential);

      _logger.LogWarning("Student visual login failed for userId {UserId}", credential.UserId);
      return OperationResult<AccessToken>.UnauthorizedResult("Invalid credentials");
    }

    credential.FailedAttempts = 0;
    credential.LastFailedAt = null;
    credential.LastSuccessfulLoginAt = DateTime.UtcNow;
    await _unitOfWork.StudentCredentialRepository.UpdateAsync(credential);

    var token = await _jwtService.GenerateAsync(credential.User);
    return OperationResult<AccessToken>.SuccessResult(token);
  }
}
