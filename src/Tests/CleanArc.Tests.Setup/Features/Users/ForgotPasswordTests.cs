using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Contracts.Notifications;
using CleanArc.Application.Features.Users.Commands.ForgotPassword;
using CleanArc.Domain.Entities.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace CleanArc.Tests.Setup.Features.Users;

public class ForgotPasswordTests
{
  [Fact]
  public async Task RequestPasswordReset_GeneratesTokenAndSendsEmail()
  {
    // Arrange
    var email = "user@example.test";
    var user = new User { Email = email };
    var resetToken = "fake-reset-token";

    var userManager = Substitute.For<IAppUserManager>();
    userManager.FindUserByEmail(email).Returns(user);
    userManager.GeneratePasswordResetToken(user).Returns(resetToken);
    userManager.UpdateUser(user).Returns(IdentityResult.Success);

    var emailService = Substitute.For<IEmailService>();

    var handler = new RequestPasswordResetCommandHandler(
        userManager,
        emailService,
        NullLogger<RequestPasswordResetCommandHandler>.Instance);

    // Act
    var result = await handler.Handle(
        new RequestPasswordResetCommand(email),
        CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    await userManager.Received(1).FindUserByEmail(email);
    await userManager.Received(1).GeneratePasswordResetToken(user);
    await userManager.Received(1).UpdateUser(user);
    await emailService.Received(1).SendEmailAsync(
        email,
        Arg.Any<string>(),
        Arg.Is<string>(body => body.Contains(resetToken)),
        isHtml: true);
  }

  [Fact]
  public async Task RequestPasswordReset_ReturnsSuccessEvenIfUserNotFound()
  {
    // Arrange
    var email = "nonexistent@example.test";
    var userManager = Substitute.For<IAppUserManager>();
    userManager.FindUserByEmail(email).Returns((User)null);

    var emailService = Substitute.For<IEmailService>();

    var handler = new RequestPasswordResetCommandHandler(
        userManager,
        emailService,
        NullLogger<RequestPasswordResetCommandHandler>.Instance);

    // Act
    var result = await handler.Handle(
        new RequestPasswordResetCommand(email),
        CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    await userManager.Received(1).FindUserByEmail(email);
    await userManager.DidNotReceive().GeneratePasswordResetToken(Arg.Any<User>());
    await emailService.DidNotReceive().SendEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
  }
}
