using Asp.Versioning;
using CleanArc.Application.Contracts.Infrastructure.Exports;
using CleanArc.Application.Features.Admin.Commands.BulkCreateUsers;
using CleanArc.Application.Features.Admin.Queries.AskAuditor;
using CleanArc.Application.Features.Admin.Queries.GetInstitutionUsers;
using CleanArc.Application.Features.Admin.Queries.GetInstitutionStats;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.BaseController;
using CleanArc.WebFramework.WebExtensions;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Controllers.V1.Admin;

[ApiVersion("1")]
[ApiController]
[Route("api/v{version:apiVersion}/Admin/Institution")]
[Authorize(Roles = "InstitutionAdmin,institutionadmin,Admin,admin")]
public class InstitutionController(
    ISender sender,
    IInstitutionUserCsvExportService csvExportService) : BaseController
{
    [HttpPost("users/bulk-generate")]
    public async Task<IActionResult> BulkGenerateUsers([FromBody] BulkCreateUsersCommand command)
    {
        // Here we could extract InstitutionId from the logged-in admin's claims
        // but for now, we'll allow the command to specify it, or default to 1 if not provided.
        if (command.InstitutionId == 0) command.InstitutionId = 1;

        var result = await sender.Send(command);

        if (!result.IsSuccess)
            return base.OperationResult(result);

        Response.Headers["X-Generated-Count"] = result.Result.GeneratedCount.ToString();
        Response.Headers["Access-Control-Expose-Headers"] = "X-Generated-Count";

        var csvBytes = System.Text.Encoding.UTF8.GetBytes(result.Result.CsvContent);
        return File(csvBytes, "text/csv", $"bulk_users_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] GetInstitutionUsersQuery query)
    {
        if (query.InstitutionId == 0) query.InstitutionId = 1;
        var result = await sender.Send(query);
        return base.OperationResult(result);
    }

    [HttpGet("users/export")]
    public async Task<IActionResult> ExportUsers([FromQuery] GetInstitutionUsersQuery query, CancellationToken cancellationToken)
    {
        if (query.InstitutionId == 0) query.InstitutionId = 1;

        var result = await sender.Send(query);
        if (!result.IsSuccess)
        {
            return base.OperationResult(result);
        }

        var csv = await csvExportService.ExportAsync(result.Result.Users, cancellationToken);
        var fileBytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(fileBytes, "text/csv", $"institution_users_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] int institutionId = 1)
    {
        var result = await sender.Send(new GetInstitutionStatsQuery { InstitutionId = institutionId });
        return base.OperationResult(result);
    }

    [HttpPost("auditor/ask")]
    public async Task<IActionResult> AskAuditor([FromBody] AskAuditorQuery query)
    {
        if (query.InstitutionId == 0) query.InstitutionId = 1;
        if (int.TryParse(User.Identity!.GetUserId(), out var userId))
            query.UserId = userId;

        var result = await sender.Send(query);
        return base.OperationResult(result);
    }
}
