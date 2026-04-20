using System.Security.Claims;
using Carter;
using CleanArc.Application.Features.Stickers.Commands;
using CleanArc.Application.Features.Stickers.Queries;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Endpoints;

public class StickersEndpoints : ICarterModule
{
  private readonly string _routePrefix = "/api/v{version:apiVersion}/Stickers/";
  private readonly double _version = 1.1;
  private readonly string _tag = "Stickers";

  public void AddRoutes(IEndpointRouteBuilder app)
  {
    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}prompts", async (ISender sender) =>
    {
      var result = await sender.Send(new GetStickerPromptOptionsQuery());
      return result.ToEndpointResult();
    }), _version, "GetStickerPromptOptions", _tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}book", async (ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new GetStickerBookQuery(userId));
      return result.ToEndpointResult();
    }), _version, "GetStickerBook", _tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapPost($"{_routePrefix}generate", async (
      [FromBody] GenerateStickerRequest request,
      ClaimsPrincipal user,
      ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new GenerateStickerCommand(userId, request.Subject, request.Style, request.Mood));
      return result.ToEndpointResult();
    }), _version, "GenerateSticker", _tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapPost($"{_routePrefix}gifts/send", async (
      [FromBody] SendStickerGiftRequest request,
      ClaimsPrincipal user,
      ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new SendStickerGiftCommand(userId, request.RecipientUserId, request.StickerId));
      return result.ToEndpointResult();
    }), _version, "SendStickerGift", _tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}gifts/unclaimed", async (ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new GetUnclaimedStickerGiftsQuery(userId));
      return result.ToEndpointResult();
    }), _version, "GetUnclaimedStickerGifts", _tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapPost($"{_routePrefix}gifts/{{giftId:int}}/claim", async (
      int giftId,
      ClaimsPrincipal user,
      ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new ClaimStickerGiftCommand(userId, giftId));
      return result.ToEndpointResult();
    }), _version, "ClaimStickerGift", _tag).RequireAuthorization();
  }
}

public record GenerateStickerRequest(string Subject, string Style, string Mood);
public record SendStickerGiftRequest(int RecipientUserId, int StickerId);
