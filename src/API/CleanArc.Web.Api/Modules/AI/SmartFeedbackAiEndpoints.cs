using Carter;
using CleanArc.Application.Features.Games.Commands;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CleanArc.Web.Api.Endpoints;

public sealed class SmartFeedbackAiEndpoints : ICarterModule
{
    private const string RoutePrefix = "/api/v{version:apiVersion}/ai/game/";
    private const double Version = 1.1;
    private const string Tag = "AI Game Feedback";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapEndpoint(builder => builder.MapPost(
            $"{RoutePrefix}smart-feedback",
            async (
                [FromBody] SmartFeedbackRequest request,
                ClaimsPrincipal user,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var userId = int.Parse(user.Identity!.GetUserId());
                var command = new GenerateSmartFeedbackCommand(
                    request.GameName,
                    request.Score,
                    request.StarsEarned,
                    request.Accuracy,
                    userId);

                var result = await sender.Send(command, cancellationToken);
                return result.ToEndpointResult();
            }), Version, "GenerateSmartFeedback", Tag)
            .RequireAuthorization();
    }
}

public sealed record SmartFeedbackRequest(
    string GameName,
    int Score,
    int StarsEarned,
    decimal? Accuracy);
