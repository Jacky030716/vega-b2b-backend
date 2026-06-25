using System.Security.Claims;
using Carter;
using CleanArc.Application.Features.Admin.Commands.CreateAdminStudent;
using CleanArc.Application.Features.Admin.Commands.CreateAdminTeacher;
using CleanArc.Application.Features.Admin.Commands.DeleteAdminUser;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Endpoints;

public sealed class AdminUserEndpoints : ICarterModule
{
    private const double Version = 1.1;
    private const string Tag = "Admin Users";
    private const string AdminRoles = "InstitutionAdmin,institutionadmin,Admin,admin";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapEndpoint(builder => builder.MapPost(
            "/api/v{version:apiVersion}/admin/users/students",
            async (
                [FromBody] CreateAdminStudentCommand command,
                ClaimsPrincipal user,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var userId = int.Parse(user.Identity!.GetUserId());
                var result = await sender.Send(command with { UserId = userId }, cancellationToken);
                return result.ToEndpointResult();
            }), Version, "CreateAdminStudent", Tag)
            .RequireAuthorization(builder => builder.RequireRole(AdminRoles.Split(',')));

        app.MapEndpoint(builder => builder.MapPost(
            "/api/v{version:apiVersion}/admin/users/teachers",
            async (
                [FromBody] CreateAdminTeacherCommand command,
                ClaimsPrincipal user,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var userId = int.Parse(user.Identity!.GetUserId());
                var result = await sender.Send(command with { UserId = userId }, cancellationToken);
                return result.ToEndpointResult();
            }), Version, "CreateAdminTeacher", Tag)
            .RequireAuthorization(builder => builder.RequireRole(AdminRoles.Split(',')));

        app.MapEndpoint(builder => builder.MapDelete(
            "/api/v{version:apiVersion}/admin/users/{id:int}",
            async (
                [FromRoute] int id,
                ClaimsPrincipal user,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var userId = int.Parse(user.Identity!.GetUserId());
                var result = await sender.Send(new DeleteAdminUserCommand(userId, id), cancellationToken);
                return result.ToEndpointResult();
            }), Version, "DeleteAdminUser", Tag)
            .RequireAuthorization(builder => builder.RequireRole(AdminRoles.Split(',')));
    }
}
