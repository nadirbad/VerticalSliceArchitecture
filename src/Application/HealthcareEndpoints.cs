using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using VerticalSliceArchitecture.Application.Scheduling;

namespace VerticalSliceArchitecture.Application;

/// <summary>
/// Extension methods for mapping all Healthcare feature endpoints to Minimal APIs.
/// </summary>
public static class HealthcareEndpoints
{
    /// <summary>
    /// Maps all healthcare-related endpoints under the /api/healthcare route prefix.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapHealthcareEndpoints(this IEndpointRouteBuilder app)
    {
        var apiGroup = app.MapGroup("/api");

        // Map appointment endpoints under /api/healthcare/appointments
        apiGroup.MapGroup("/healthcare/appointments")
            .WithTags("Appointments")
            .MapAppointmentEndpoints();

        return app;
    }
}