using Microsoft.OpenApi.Models;

using VerticalSliceArchitecture.Application;
using VerticalSliceArchitecture.Application.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options => options.AddDefaultPolicy(
        policy => policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod()));

builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo
{
    Title = "Healthcare Appointments API",
    Version = "v1",
    Description = """
        ## Sample Data (Development)

        The following sample data is seeded automatically in development mode.

        ### Patients
        | ID | Name | Email |
        |----|------|-------|
        | `11111111-1111-1111-1111-111111111111` | John Smith | john.smith@example.com |
        | `22222222-2222-2222-2222-222222222222` | Jane Doe | jane.doe@example.com |
        | `33333333-3333-3333-3333-333333333333` | Bob Johnson | bob.johnson@example.com |

        ### Doctors
        | ID | Name | Specialty |
        |----|------|-----------|
        | `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa` | Dr. Sarah Wilson | Family Medicine |
        | `bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb` | Dr. Michael Chen | Cardiology |
        | `cccccccc-cccc-cccc-cccc-cccccccccccc` | Dr. Emily Rodriguez | Pediatrics |
        """,
}));

builder.Services.AddProblemDetails();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Enable middleware to serve generated Swagger as a JSON endpoint.
app.UseSwagger();

// Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = string.Empty;
});

app.UseCors();

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error-development");
}
else
{
    app.UseExceptionHandler("/error");
}

app.UseAuthorization();
app.MapControllers();

// Map Minimal API endpoints
app.MapHealthcareEndpoints();

// Seed the database in development only
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await ApplicationDbContextSeed.SeedSampleDataAsync(context);
}

app.Run();

public partial class Program { }