using CleanArc.Application.Contracts.Achievements;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Features.Shop.Commands;
using CleanArc.Domain.Entities.Shop;
using NSubstitute;

namespace CleanArc.Tests.Setup.Features.Shop;

public class EquipItemTests
{
  [Fact]
  public async Task EquipAvatar_RejectsItemNotOwnedByUser()
  {
    var unitOfWork = Substitute.For<IUnitOfWork>();
    unitOfWork.ShopRepository.GetShopItemByIdAsync(5).Returns(SetId(new ShopItem
    {
      Name = "Scholar Fox",
      Category = "avatar"
    }, 5));
    unitOfWork.ShopRepository.GetUserInventoryItemAsync(7, 5).Returns((UserInventoryItem)null);
    var achievements = Substitute.For<IAchievementTrackingService>();
    var handler = new EquipItemCommandHandler(unitOfWork, achievements);

    var result = await handler.Handle(
      new EquipItemCommand(7, "avatar", 5),
      CancellationToken.None);

    Assert.False(result.IsSuccess);
    await unitOfWork.ShopRepository.DidNotReceive().EquipItemAsync(7, "avatar", 5);
  }

  [Fact]
  public async Task EquipAvatar_PersistsNumericShopItemIdForOwnedAvatar()
  {
    var unitOfWork = Substitute.For<IUnitOfWork>();
    unitOfWork.ShopRepository.GetShopItemByIdAsync(5).Returns(SetId(new ShopItem
    {
      Name = "Scholar Fox",
      Category = "avatar"
    }, 5));
    unitOfWork.ShopRepository.GetUserInventoryItemAsync(7, 5).Returns(new UserInventoryItem
    {
      UserId = 7,
      ShopItemId = 5
    });
    var achievements = Substitute.For<IAchievementTrackingService>();
    var handler = new EquipItemCommandHandler(unitOfWork, achievements);

    var result = await handler.Handle(
      new EquipItemCommand(7, "avatar", 5),
      CancellationToken.None);

    Assert.True(result.IsSuccess);
    await unitOfWork.ShopRepository.Received(1).EquipItemAsync(7, "avatar", 5);
  }

  private static T SetId<T>(T entity, int id)
  {
    entity!.GetType().GetProperty("Id")!.SetValue(entity, id);
    return entity;
  }
}
