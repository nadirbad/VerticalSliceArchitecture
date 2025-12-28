using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using VerticalSliceArchitecture.Application.Common;

namespace VerticalSliceArchitecture.Application.Medications;

/// <summary>
/// Extension methods for mapping Prescription endpoints to Minimal APIs.
/// </summary>
public static class PrescriptionEndpoints
{
    /// <summary>
    /// Maps all prescription-related endpoints.
    /// </summary>
    /// <param name="group">The route group builder.</param>
    /// <returns>The route group builder for chaining.</returns>
    public static RouteGroupBuilder MapPrescriptionEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", IssuePrescriptionEndpoint.Handle)
            .WithName("IssuePrescription")
            .Produces<PrescriptionResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<IssuePrescriptionCommand>>();

        return group;
    }
}