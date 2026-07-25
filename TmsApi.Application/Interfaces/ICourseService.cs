using TmsApi.Application.Dtos;
using TmsApi.Domain.Entities;

namespace TmsApi.Application.Interfaces;

public interface ICourseService
{
    Task<Course?> GetByCodeAsync(string code, CancellationToken ct);
    Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct);

    // Exercise 2: accept request DTO, return response DTO
    Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct);

    // Exercise 3: pre-check before insert — prevents 500 from unique index violation
    Task<bool> CodeExistsAsync(string code, CancellationToken ct);

    Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(PagedRequest
request, CancellationToken ct);
}