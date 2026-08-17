using TmsApi.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using TmsApi.Services;
using Tms.Api.Persistence;
using Tms.Api.Filters;


var builder = WebApplication.CreateBuilder(args);
// Step 3: Register the DbContext in Program.cs
// Register TmsDbContext scoped for incoming HTTP requests

builder.Services.AddDbContext<TmsDbContext>(options =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("TmsDatabase"))
               //  Step 1: Enable Console SQL Logging
        .LogTo(Console.WriteLine, LogLevel.Information)  // Log SQL to output window
        .EnableSensitiveDataLogging()  // Show parameters in querylogs (dev only)
);
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.Services.AddControllers(options =>
{
options.Filters.Add<AuditLogFilter>();
});

// Register controller support (MVC pipeline for API controllers)
 builder.Services.AddControllers();
 builder.Services.AddOpenApi();

// AUTHENTICATION SETUP
// attach a custom handler (BasicAuthHandler) that defines how users are authenticated
builder.Services
    .AddAuthentication("Basic")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthHandler>("Basic", _ => { });

builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<ICertificateService, CertificateService>();
builder.Services.AddScoped<IAssessmentService, AssessmentService>();
builder.Services.AddProblemDetails();
// AUTHORIZATION SETUP (ARE YOU ALLOWED?)
// Enables [Authorize] / RequireAuthorization() checks 
builder.Services.AddAuthorization();
builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();
// APPLICATION PIPELINE (ORDER MATTERS)
var app = builder.Build();

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
// Minimal API endpoint protected by authorization
app.MapGet("/api/enrollments/worker-smoke", (EnrollmentWorker worker) =>
{
    worker.ProcessBatch();
    return Results.Ok("processed");
});
//
app.MapGet("/api/assessments/results", () => Results.Ok(new
{
// Placeholder response    
courseCode = "CS-101",
studentId = "S-001",
letterGrade = "A"
}))
.RequireAuthorization(); // Forces authentication before execution
app.MapControllers();
app.MapGet("/api/error", () =>
{
    //throw new TmsDatabaseException("Simulated database failure for ProblemDetails testing");
    throw new InvalidOperationException("Simulated database failure for ProblemDetails testing");
});
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // UI
}
if (app.Environment.IsDevelopment())
{
using var scope = app.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
await DataSeeder.SeedAsync(context);
}
app.Run();

