using Asp.Versioning;
using CleanArc.Application.Features.Admin.Commands.UpdateAdminUser;
using CleanArc.Application.Features.Admin.Queries.GetAdminUserDetails;
using CleanArc.WebFramework.BaseController;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Controllers.V1.Admin;

[ApiVersion("1")]
[ApiController]
[Route("api/v{version:apiVersion}/admin/users")]
[Authorize(Roles = "InstitutionAdmin,institutionadmin,Admin,admin")]
public class AdminUsersController(ISender sender) : BaseController
{
    [HttpGet("{id:int}/details")]
    public async Task<IActionResult> GetUserDetails([FromRoute] int id, [FromQuery] int institutionId = 1)
    {
        var result = await sender.Send(new GetAdminUserDetailsQuery
        {
            UserId = id,
            InstitutionId = institutionId <= 0 ? 1 : institutionId
        });

        return base.OperationResult(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUser([FromRoute] int id, [FromBody] UpdateAdminUserCommand command)
    {
        command.UserId = id;
        if (command.InstitutionId <= 0) command.InstitutionId = 1;

        var result = await sender.Send(command);
        return base.OperationResult(result);
    }
}
