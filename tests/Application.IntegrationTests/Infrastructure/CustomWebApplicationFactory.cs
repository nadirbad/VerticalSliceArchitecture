using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using VerticalSliceArchitecture.Application.Domain.Healthcare;
using VerticalSliceArchitecture.Application.Infrastructure.Persistence;

namespace VerticalSliceArchitecture.Application.IntegrationTests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for integration tests.
/// Configures in-memory database and test-specific services.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove real database registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing with unique name per factory instance
            var databaseName = $"InMemoryTestDb_{Guid.NewGuid()}";
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName);
            });

            // Build service provider and initialize database
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Ensure database is created
            dbContext.Database.EnsureCreated();

            // Seed test data
            SeedTestData(dbContext);
        });
    }

    /// <summary>
    /// Seeds deterministic test data matching HTTP request file GUIDs.
    /// </summary>
    private static void SeedTestData(ApplicationDbContext context)
    {
        // Test Patients - using known GUIDs from HTTP request files
        var patient1 = new Patient("John Smith", "john.smith@example.com", "+1-555-0101")
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        };

        var patient2 = new Patient("Jane Doe", "jane.doe@example.com", "+1-555-0102")
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        };

        var patient3 = new Patient("Bob Johnson", "bob.johnson@example.com", "+1-555-0103")
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
        };

        // Test Doctors - using known GUIDs from HTTP request files
        var doctor1 = new Doctor("Dr. Sarah Wilson", "Family Medicine")
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        };

        var doctor2 = new Doctor("Dr. Michael Chen", "Cardiology")
        {
            Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        };

        var doctor3 = new Doctor("Dr. Emily Rodriguez", "Pediatrics")
        {
            Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
        };

        context.Patients.AddRange(patient1, patient2, patient3);
        context.Doctors.AddRange(doctor1, doctor2, doctor3);
        context.SaveChanges();
    }
}