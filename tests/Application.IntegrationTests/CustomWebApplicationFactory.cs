using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using VerticalSliceArchitecture.Application.Infrastructure.Persistence;

namespace VerticalSliceArchitecture.Application.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // EF Core shares an in-memory store between every context that names the same database, and
    // that sharing crosses host boundaries. xUnit builds one factory per test class, so a fixed
    // name would let unrelated test classes see each other's data - and would let them race each
    // other while seeding the same sample rows at startup. A name per factory keeps classes
    // isolated.
    private readonly string _databaseName = $"TestDb-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Override configuration so AddInfrastructure registers in-memory
        // even when environment variables (e.g., dev container) set UseInMemoryDatabase=false
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseInMemoryDatabase"] = "true",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove all EF Core and DbContext-related registrations that may have
            // been added before our configuration override took effect
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<DbContextOptions>();

            // Remove the DbContext service itself to avoid duplicate registrations
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ApplicationDbContext));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });
        });
    }
}