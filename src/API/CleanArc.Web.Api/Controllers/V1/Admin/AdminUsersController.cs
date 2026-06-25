using Asp.Versioning;
using CleanArc.Application.Features.Admin.Commands.UpdateAdminUser;
using CleanArc.Application.Features.Admin.Queries.GetAdminUserDetails;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.BaseController;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Controllers.V1.Admin;

[ApiVersion("1")]
[ApiVersion("1.1")]
[ApiController]
[Route("api/v{version:apiVersion}/admin/users")]
[Authorize(Roles = "InstitutionAdmin,institutionadmin,Admin,admin")]
public class AdminUsersController(ISender sender) : BaseController
{
    [HttpGet("{id:int}/details")]
    public async Task<IActionResult> GetUserDetails([FromRoute] int id)
    {
        var adminUserId = int.Parse(User.Identity!.GetUserId());
        var result = await sender.Send(new GetAdminUserDetailsQuery
        {
            UserId = id,
            AdminUserId = adminUserId
        });

        return base.OperationResult(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUser([FromRoute] int id, [FromBody] UpdateAdminUserCommand command)
    {
        var adminUserId = int.Parse(User.Identity!.GetUserId());
        command.UserId = id;
        command.AdminUserId = adminUserId;

        var result = await sender.Send(command);
        return base.OperationResult(result);
    }
}
