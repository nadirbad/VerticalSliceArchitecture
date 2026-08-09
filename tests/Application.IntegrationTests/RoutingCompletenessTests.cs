using System.Reflection;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using VerticalSliceArchitecture.Application.Scheduling;

namespace VerticalSliceArchitecture.Application.IntegrationTests;

/// <summary>
/// Checks that every slice that defines an endpoint is actually reachable over HTTP.
/// </summary>
/// <remarks>
/// A slice can be written correctly and still be entirely dead code if nobody adds it to
/// SchedulingEndpoints. Nothing else in the build notices: it compiles, its unit tests pass, and
/// the route simply 404s. The architecture tests cannot catch this because registration happens
/// in a method body.
/// </remarks>
public class RoutingCompletenessTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RoutingCompletenessTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void EverySliceWithAnEndpoint_IsMappedToARoute()
    {
        // Arrange
        _ = _factory.CreateClient(); // Forces the host - and therefore routing - to be built.

        var mappedNames = _factory.Services
            .GetRequiredService<EndpointDataSource>()
            .Endpoints
            .Select(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        // Act
        var unmapped = SlicesWithEndpoints()
            .Where(envelope => !mappedNames.Contains(envelope.Name))
            .Select(envelope => envelope.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        // Assert
        unmapped.Should().BeEmpty(
            "every slice that declares a nested Endpoint must be registered with a matching "
            + "WithName in its feature's endpoint mapping, otherwise the slice is unreachable");
    }

    // A slice envelope is a public static class; it owns an endpoint when it nests an Endpoint type.
    private static IEnumerable<Type> SlicesWithEndpoints() => typeof(BookAppointment).Assembly
        .GetTypes()
        .Where(type => type.IsClass && type.IsAbstract && type.IsSealed && type.IsPublic)
        .Where(type => type.GetNestedType("Endpoint", BindingFlags.Public | BindingFlags.NonPublic) is not null);
}