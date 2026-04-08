using Asp.Versioning;
using CleanArc.Application.Features.Classrooms.Commands.BulkCreateClassroom;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Controllers.V1.Educator;

[ApiVersion("1")]
[ApiController]
[Route("api/v{version:apiVersion}/educator/[controller]")]
public class ClassroomsController : ControllerBase
{
    private readonly ISender _sender;

    public ClassroomsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("bulk-create")]
    public async Task<IActionResult> BulkCreateClassroom([FromBody] BulkCreateClassroomCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (result.IsSuccess)
        {
            return File(result.Result, "application/pdf", $"{command.Name.Replace(" ", "_")}_Roster.pdf");
        }
        
        return BadRequest(new { message = result.ErrorMessage });
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardStats()
    {
        // Returning mocked dashboard stats for now. Should be mapped to actual MediatR queries later.
        return Ok(new
        {
            TotalActiveStudents = 30,
            ChallengesCompleted = 120,
            Alerts = 3,
            Classrooms = new[]
            {
                new { Id = 1, Name = "Standard 3 Science", Progress = 80 },
                new { Id = 2, Name = "Art Club", Progress = 40 }
            }
        });
    }
}
