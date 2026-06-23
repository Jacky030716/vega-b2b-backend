using CleanArc.Application.Contracts.DTOs.User;
using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Features.Users.Commands.UpdateUserProfile;
using CleanArc.Domain.Entities.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CleanArc.Tests.Setup.Features.Users;

public class UpdateUserProfileTests
{
  [Theory]
  [InlineData(nameof(UpdateUserProfileRequest.Name))]
  [InlineData(nameof(UpdateUserProfileRequest.FamilyName))]
  [InlineData("UserName")]
  [InlineData("Email")]
  [InlineData("PhoneNumber")]
  public void UpdateUserProfileRequest_ContainsPersistedProfileField(string propertyName)
  {
    var property = typeof(UpdateUserProfileRequest).GetProperty(propertyName);

    Assert.NotNull(property);
  }

  [Fact]
  public async Task UpdateUserProfile_PersistsIdentityFields()
  {
    var user = SetId(new User
    {
      Name = "Old",
      FamilyName = "Name",
      UserName = "old-user",
      Email = "old@example.test",
      PhoneNumber = "0123456789"
    }, 7);
    var userManager = Substitute.For<IAppUserManager>();
    userManager.GetUserById(user.Id).Returns(user);
    userManager.IsExistUserName("new-user").Returns(false);
    userManager.FindUserByEmail("new@example.test").Returns((User)null);
    userManager.IsExistUser("0198765432").Returns(false);
    userManager.UpdateUser(user).Returns(IdentityResult.Success);
    var handler = CreateHandler(userManager);

    var result = await handler.Handle(
      new UpdateUserProfileCommand(user.Id, CreateRequest()),
      CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.Equal("New", user.Name);
    Assert.Equal("Profile", user.FamilyName);
    Assert.Equal("new-user", user.UserName);
    Assert.Equal("new@example.test", user.Email);
    Assert.Equal("0198765432", user.PhoneNumber);
    await userManager.Received(1).UpdateUser(user);
  }

  [Fact]
  public async Task UpdateUserProfile_RejectsDuplicateUserName()
  {
    var user = SetId(new User { UserName = "old-user", Email = "old@example.test" }, 7);
    var userManager = Substitute.For<IAppUserManager>();
    userManager.GetUserById(user.Id).Returns(user);
    userManager.IsExistUserName("new-user").Returns(true);
    userManager.UpdateUser(user).Returns(IdentityResult.Success);
    var handler = CreateHandler(userManager);

    var result = await handler.Handle(
      new UpdateUserProfileCommand(user.Id, CreateRequest()),
      CancellationToken.None);

    Assert.False(result.IsSuccess);
    await userManager.DidNotReceive().UpdateUser(Arg.Any<User>());
  }

  [Fact]
  public async Task UpdateUserProfile_RejectsDuplicateEmail()
  {
    var user = SetId(new User { UserName = "old-user", Email = "old@example.test" }, 7);
    var otherUser = SetId(new User { UserName = "other", Email = "new@example.test" }, 8);
    var userManager = Substitute.For<IAppUserManager>();
    userManager.GetUserById(user.Id).Returns(user);
    userManager.IsExistUserName("new-user").Returns(false);
    userManager.FindUserByEmail("new@example.test").Returns(otherUser);
    userManager.UpdateUser(user).Returns(IdentityResult.Success);
    var handler = CreateHandler(userManager);

    var result = await handler.Handle(
      new UpdateUserProfileCommand(user.Id, CreateRequest()),
      CancellationToken.None);

    Assert.False(result.IsSuccess);
    await userManager.DidNotReceive().UpdateUser(Arg.Any<User>());
  }

  [Fact]
  public async Task UpdateUserProfile_RejectsDuplicatePhoneNumber()
  {
    var user = SetId(new User { UserName = "old-user", Email = "old@example.test" }, 7);
    var userManager = Substitute.For<IAppUserManager>();
    userManager.GetUserById(user.Id).Returns(user);
    userManager.IsExistUserName("new-user").Returns(false);
    userManager.FindUserByEmail("new@example.test").Returns((User)null);
    userManager.IsExistUser("0198765432").Returns(true);
    userManager.UpdateUser(user).Returns(IdentityResult.Success);
    var handler = CreateHandler(userManager);

    var result = await handler.Handle(
      new UpdateUserProfileCommand(user.Id, CreateRequest()),
      CancellationToken.None);

    Assert.False(result.IsSuccess);
    await userManager.DidNotReceive().UpdateUser(Arg.Any<User>());
  }

  private static UpdateUserProfileCommandHandler CreateHandler(IAppUserManager userManager)
  {
    return new UpdateUserProfileCommandHandler(
      userManager,
      NullLogger<UpdateUserProfileCommandHandler>.Instance);
  }

  private static UpdateUserProfileRequest CreateRequest()
  {
    return new UpdateUserProfileRequest
    {
      Name = "New",
      FamilyName = "Profile",
      UserName = "new-user",
      Email = "new@example.test",
      PhoneNumber = "0198765432",
      AvatarId = "0"
    };
  }

  private static T SetId<T>(T entity, int id)
  {
    entity!.GetType().GetProperty("Id")!.SetValue(entity, id);
    return entity;
  }
}
