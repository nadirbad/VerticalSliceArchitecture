using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using VerticalSliceArchitecture.Application.Common;
using VerticalSliceArchitecture.Application.Common.Models;

namespace VerticalSliceArchitecture.Application.Scheduling;

/// <summary>
/// Extension methods for mapping Healthcare Scheduling endpoints to Minimal APIs.
/// </summary>
public static class SchedulingEndpoints
{
    /// <summary>
    /// Maps all appointment-related endpoints.
    /// </summary>
    /// <param name="group">The route group builder.</param>
    /// <returns>The route group builder for chaining.</returns>
    internal static RouteGroupBuilder MapAppointmentEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", BookAppointment.Endpoint.Handle)
            .WithName("BookAppointment")
            .Produces<BookAppointment.Result>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddEndpointFilter<ValidationFilter<BookAppointment.Command>>();

        group.MapPost("/{appointmentId}/complete", CompleteAppointment.Endpoint.Handle)
            .WithName("CompleteAppointment")
            .Produces<CompleteAppointment.Result>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<CompleteAppointment.Command>>();

        group.MapPost("/{appointmentId}/cancel", CancelAppointment.Endpoint.Handle)
            .WithName("CancelAppointment")
            .Produces<CancelAppointment.Result>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<CancelAppointment.Command>>();

        group.MapGet("/", GetAppointments.Endpoint.Handle)
            .WithName("GetAppointments")
            .Produces<PaginatedList<AppointmentDto>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<GetAppointments.Query>>();

        group.MapGet("/{id}", GetAppointmentById.Endpoint.Handle)
            .WithName("GetAppointmentById")
            .Produces<AppointmentDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<GetAppointmentById.Query>>();

        return group;
    }
}