using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Dtos;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;


public class CourseService(TmsDbContext context, ILogger<CourseService> logger) : ICourseService
{
    
    
        public Task<Course?> GetByCodeAsync(string code, CancellationToken ct) =>
            context.Courses
                .Include(c => c.Enrollments) // required for Enrollments.Count check
                .FirstOrDefaultAsync(c => c.Code == code, ct);
        // No AsNoTracking here — we don't need to track but it's harmless
        // If you add AsNoTracking, course.Enrollments.Count still works
        
    // Exercise 1-2 read path
    public Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct) =>
        context.Courses
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CourseResponseDto(
                c.Id,
                c.Code,
                c.Title,
                c.MaxCapacity,
                c.Enrollments.Count))
            .FirstOrDefaultAsync(ct);

     //Exercise 2: Write path       

public async Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct)
{
    var course = new Course
    {
        Code = request.Code,
        Title = request.Title,
        MaxCapacity = request.MaxCapacity
    };

    context.Courses.Add(course);

await context.SaveChangesAsync(ct);

logger.LogInformation(
    "Created course {CourseId} ({Code})",
    course.Id,
    course.Code);

return (await GetByIdAsync(course.Id, ct))!;
}
//  Exercise 3: Duplicate check
public Task<bool> CodeExistsAsync(string code, CancellationToken ct) =>
context.Courses.AsNoTracking().AnyAsync(c => c.Code == code, ct);

// ── Session 2 Exercise 4: Paginated collection ────────────

    public async Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(
        PagedRequest request, CancellationToken ct)
    {
        // Step 1 — Start with a no-tracking IQueryable (nothing hits the DB yet)
        IQueryable<Course> query = context.Courses.AsNoTracking();

        // Step 2 — Filter: if search has a value, apply it to Title and Code
        // ILike = case-insensitive LIKE on PostgreSQL
        // "fund" matches "Web Development Fundamentals" regardless of case
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(c =>
                EF.Functions.ILike(c.Title, $"%{request.Search}%") ||
                EF.Functions.ILike(c.Code,  $"%{request.Search}%"));
        }

        // Step 3 — COUNT before paging
        // This gives total matching rows, not just the current page
        // SQL: SELECT COUNT(*) FROM "Courses" WHERE ...
        var totalCount = await query.CountAsync(ct);

        // Step 4 — Sort: whitelist allowed columns, fall back to Title
        // Never pass request.OrderBy directly into LINQ — client could inject anything
        IQueryable<Course> sorted = request.OrderBy switch
        {
            "Code"        => request.Descending ? query.OrderByDescending(c => c.Code)        : query.OrderBy(c => c.Code),
            "MaxCapacity" => request.Descending ? query.OrderByDescending(c => c.MaxCapacity) : query.OrderBy(c => c.MaxCapacity),
            _             => request.Descending ? query.OrderByDescending(c => c.Title)       : query.OrderBy(c => c.Title),
        };

        // Step 5 + 6 + 7 — Skip/Take + Project + Execute (single SQL SELECT)
        // Skip((page-1) * pageSize) → OFFSET
        // Take(pageSize)            → LIMIT
        // Select(...)               → projects to DTO inside the IQueryable
        //                            EF translates Enrollments.Count to SQL COUNT(*)
        // ToListAsync()             → sends the single SELECT to the database
        var items = await sorted
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CourseResponseDto(
                c.Id, c.Code, c.Title, c.MaxCapacity, c.Enrollments.Count))
            .ToListAsync(ct);

        // Step 6 — Assemble and return
        return new PagedResponse<CourseResponseDto>
        {
            Items      = items,
            TotalCount = totalCount,
            Page       = request.Page,
            PageSize   = request.PageSize
        };
    }

}


