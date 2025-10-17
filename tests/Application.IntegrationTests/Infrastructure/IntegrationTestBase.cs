using Microsoft.Extensions.DependencyInjection;
using VerticalSliceArchitecture.Application.Domain.Healthcare;
using VerticalSliceArchitecture.Application.Infrastructure.Persistence;

namespace VerticalSliceArchitecture.Application.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for integration tests providing common setup and teardown logic.
/// Implements IAsyncLifetime for xUnit test lifecycle management.
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    protected readonly ApplicationDbContext DbContext;
    protected readonly HttpClient Client;
    private readonly IServiceScope _scope;

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        // Create a new scope for each test to ensure isolation
        _scope = factory.Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Client = factory.CreateClient();
    }

    /// <summary>
    /// Called before each test - resets database to clean state.
    /// </summary>
    public virtual async Task InitializeAsync()
    {
        await ResetDatabaseAsync();
    }

    /// <summary>
    /// Called after each test - cleanup resources.
    /// </summary>
    public virtual async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
        _scope.Dispose();
        Client.Dispose();
    }

    /// <summary>
    /// Resets the database to a clean state before each test.
    /// Uses EnsureDeleted + EnsureCreated pattern for in-memory database.
    /// </summary>
    private async Task ResetDatabaseAsync()
    {
        // Delete and recreate database for complete isolation
        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.Database.EnsureCreatedAsync();

        // Re-seed test data after reset
        await SeedTestDataAsync();
    }

    /// <summary>
    /// Seeds deterministic test data matching HTTP request file GUIDs.
    /// </summary>
    private async Task SeedTestDataAsync()
    {
        // Re-seed patients and doctors after database reset
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

        DbContext.Patients.AddRange(patient1, patient2, patient3);
        DbContext.Doctors.AddRange(doctor1, doctor2, doctor3);
        await DbContext.SaveChangesAsync();
    }
}
