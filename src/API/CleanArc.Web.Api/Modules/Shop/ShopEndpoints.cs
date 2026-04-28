using System.Security.Claims;
using Carter;
using CleanArc.Application.Features.Shop.Commands;
using CleanArc.Application.Features.Shop.Queries;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Mediator;
using Microsoft.AspNetCore.Mvc;

#nullable enable

namespace CleanArc.Web.Api.Endpoints;

public class ShopEndpoints : ICarterModule
{
  private readonly string _routePrefix = "/api/v{version:apiVersion}/Shop/";
  private readonly double _version = 1.1;
  private readonly string _tag = "Shop";

  public void AddRoutes(IEndpointRouteBuilder app)
  {
    // Get shop items
    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}items", async ([FromQuery] string? category, ISender sender) =>
    {
      var result = await sender.Send(new GetShopItemsQuery(category));
      return result.ToEndpointResult();
    }), _version, "GetShopItems", _tag).RequireAuthorization();

    // Get daily specials
    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}daily-specials", async (ISender sender) =>
    {
      var result = await sender.Send(new GetDailySpecialsQuery());
      return result.ToEndpointResult();
    }), _version, "GetDailySpecials", _tag).RequireAuthorization();

    // Get user inventory
    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}inventory", async (ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new GetUserInventoryQuery(userId));
      return result.ToEndpointResult();
    }), _version, "GetUserInventory", _tag).RequireAuthorization();

    // Purchase item
    app.MapEndpoint(builder => builder.MapPost($"{_routePrefix}purchase", async ([FromBody] PurchaseRequest request, ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new PurchaseItemCommand(userId, request.ShopItemId));
      return result.ToEndpointResult();
    }), _version, "PurchaseItem", _tag).RequireAuthorization();

    // Equip item
    app.MapEndpoint(builder => builder.MapPost($"{_routePrefix}equip", async ([FromBody] EquipRequest request, ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new EquipItemCommand(userId, request.Category, request.ShopItemId));
      return result.ToEndpointResult();
    }), _version, "EquipItem", _tag).RequireAuthorization();

    // Admin: Create shop item
    app.MapEndpoint(builder => builder.MapPost($"{_routePrefix}items", async ([FromBody] CreateShopItemCommand command, ISender sender) =>
    {
      var result = await sender.Send(command);
      return result.ToEndpointResult();
    }), _version, "CreateShopItem", _tag).RequireAuthorization("AdminPolicy");
  }
}

public record PurchaseRequest(int ShopItemId);
public record EquipRequest(string Category, int ShopItemId);
