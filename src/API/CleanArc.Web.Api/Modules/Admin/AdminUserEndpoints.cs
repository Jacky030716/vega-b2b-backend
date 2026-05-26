using Carter;
using CleanArc.Application.Features.Admin.Commands.CreateAdminStudent;
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
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(command, cancellationToken);
                return result.ToEndpointResult();
            }), Version, "CreateAdminStudent", Tag)
            .RequireAuthorization(builder => builder.RequireRole(AdminRoles.Split(',')));
    }
}
