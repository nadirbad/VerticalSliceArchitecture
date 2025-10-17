using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using VerticalSliceArchitecture.Application.Features.Healthcare.Appointments;

namespace VerticalSliceArchitecture.Application.Features.Healthcare;

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
        var healthcareGroup = app.MapGroup("/api/healthcare");

        // Map appointment endpoints under /api/healthcare/appointments
        healthcareGroup.MapGroup("/appointments")
            .MapAppointmentEndpoints();

        // Map prescription endpoints directly under /api (not under healthcare)
        // This is done separately since prescriptions use /api/prescriptions route
        app.MapIssuePrescription();

        return app;
    }
}