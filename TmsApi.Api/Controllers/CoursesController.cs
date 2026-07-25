
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.Dtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers;

// [Tags] at class level — groups ALL course actions under "Courses" in Scalar
// Per-action [Tags] would break grouping
// [Produces] — declares response content type for Scalar's Try It panel
// Class-level 500 — every action inherits it, no need to repeat per-action
[ApiController]
[Route("api/courses")]
[Tags("Courses")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
//Troubleshoot - add 409 to the action
[ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status409Conflict)]
public class CoursesController(
    ICourseService courseService,
    LinkGenerator linkGenerator) : ControllerBase // LinkGenerator injected — no string interpolation
{
    // ── Exercise 4: GET paginated collection 
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<CourseResponseDto>), StatusCodes.Status200OK)]
    [EndpointSummary("List courses with pagination")]
    [EndpointDescription("Returns a paginated, optionally filtered list of TMS courses. PageSize is capped at 50.")]
    public async Task<IActionResult> GetCourses(
        [FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await courseService.GetCoursesAsync(request, ct);
        return Ok(result);
    }

    // ── Exercise 5: GET single course with HATEOAS links ─────
    
    [HttpGet("{id:int}", Name = nameof(GetCourseById))]
    [ProducesResponseType(typeof(CourseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a course by ID")]
    [EndpointDescription("Returns course details with HATEOAS links. Returns 404 if the course does not exist.")]
    public async Task<IActionResult> GetCourseById(int id, CancellationToken ct)
    {
        var course = await courseService.GetByIdAsync(id, ct);
        if (course is null) return NotFound();
//TODO 1:
        // Build the self/update/delete href — all point to the same route, different methods
        var selfHref = linkGenerator.GetPathByName(
            HttpContext, nameof(GetCourseById), new { id })!;

        // Build the enrollments list href — uses the named route on EnrollmentsController
        var enrollmentsHref = linkGenerator.GetPathByName(
            HttpContext, "ListCourseEnrollments", new { courseId = id })!;
//TODO 2:
        // Build the links list
        // Always present: self, update, delete, enrollments (4 links)   
        var links = new List<LinkDto>
        {
            new(selfHref,        "self",        "GET"),
            new(selfHref,        "update",      "PUT"),
            new(selfHref,        "delete",      "DELETE"),
            new(enrollmentsHref, "enrollments", "GET"),
        };

        // Conditional: enroll only appears when seats are available
        // Angular team reads presence/absence 
        if (course.EnrollmentCount < course.MaxCapacity)
            links.Add(new(enrollmentsHref, "enroll", "POST"));

//TODO 3: 
        var detail = new CourseDetailDto
        {
            Id              = course.Id,
            Code            = course.Code,
            Title           = course.Title,
            MaxCapacity     = course.MaxCapacity,
            EnrollmentCount = course.EnrollmentCount,
            Links           = links.AsReadOnly()
        };

        return Ok(detail);
    }

    //  Exercise 2+3: POST create course ─────────────────────
    [HttpPost]
    [ProducesResponseType(typeof(CourseDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Create a new course")]
    [EndpointDescription("Creates a course with a unique code. Returns 409 if the course code already exists.")]
    public async Task<IActionResult> CreateCourse(
        CreateCourseRequest request, CancellationToken ct)
    {
        if (await courseService.CodeExistsAsync(request.Code, ct))
        {
            return Conflict(new ProblemDetails
            {
                Title  = "Course code already exists",
                Detail = $"A course with code '{request.Code}' is already registered.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var result = await courseService.CreateAsync(request, ct);

       

        return CreatedAtAction(nameof(GetCourseById), new { id = result.Id }, result);
    }
}











































