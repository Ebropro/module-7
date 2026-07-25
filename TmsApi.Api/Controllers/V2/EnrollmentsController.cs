using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.Enrollments.Commands;
using TmsApi.Application.Enrollments.Queries;

namespace TmsApi.Api.Controllers.V2;


[ApiController]
[Route("api/v{version:apiVersion}/enrollments")]
[ApiVersion("2.0")]
[Tags("Enrollments V2")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class EnrollmentsController(IMediator mediator) : ControllerBase
{
    // POST enroll a student 
    // Command goes to MediatR → LoggingBehavior → ValidationBehavior → Handler
    // Result comes back → Match() translates to HTTP
    [HttpPost]
    [ProducesResponseType(typeof(EnrollmentCreated), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Enrol a student (V2)")]
    [EndpointDescription("Routes through MediatR CQRS pipeline. Validates input, checks business rules, returns typed result.")]
    public async Task<IActionResult> Enroll(
        EnrollStudentCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);

        
        // No if/switch about business rules here — only HTTP status translation
        return result.Match<IActionResult>(
            onSuccess: created => CreatedAtAction(
                nameof(GetSchedule),
                new { studentId = created.StudentId },
                created),

            onFailure: error =>
            {
                // Map error code to HTTP status — these are the three M6 categories:
               
                var status = error.Code switch
                {
                    "course_not_found"                    => StatusCodes.Status404NotFound,
                    "course_full" or "already_enrolled"   => StatusCodes.Status409Conflict,
                    _                                     => StatusCodes.Status400BadRequest
                };

                return Problem(
                    statusCode: status,
                    title:      "Enrollment rejected",
                    detail:     error.Message,
                    type:       $"https://tms.local/errors/{error.Code}");
            });
    }

    //  GET student schedule 
   
    [HttpGet("{studentId:int}/schedule")]
    [ProducesResponseType(typeof(ScheduleDto), StatusCodes.Status200OK)]
    [EndpointSummary("Get a student's course schedule (V2)")]
    public async Task<IActionResult> GetSchedule(int studentId, CancellationToken ct)
    {
        var schedule = await mediator.Send(
            new GetStudentScheduleQuery(studentId), ct);
        return Ok(schedule);
    }
}
