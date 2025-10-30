using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using VerticalSliceArchitecture.Application.Common;

namespace VerticalSliceArchitecture.Application.Features.Healthcare.Appointments;

/// <summary>
/// Extension methods for mapping Healthcare Appointment endpoints to Minimal APIs.
/// </summary>
public static class AppointmentEndpoints
{
    /// <summary>
    /// Maps all appointment-related endpoints.
    /// </summary>
    /// <param name="group">The route group builder.</param>
    /// <returns>The route group builder for chaining.</returns>
    public static RouteGroupBuilder MapAppointmentEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", BookAppointmentEndpoint.Handle)
            .WithName("BookAppointment")
            .Produces<BookAppointmentResult>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddEndpointFilter<ValidationFilter<BookAppointmentCommand>>();

        group.MapPost("/{appointmentId}/reschedule", RescheduleAppointmentEndpoint.Handle)
            .WithName("RescheduleAppointment")
            .Produces<RescheduleAppointmentResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddEndpointFilter<ValidationFilter<RescheduleAppointmentCommand>>();

        group.MapPost("/{appointmentId}/complete", CompleteAppointmentEndpoint.Handle)
            .WithName("CompleteAppointment")
            .Produces<CompleteAppointmentResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<CompleteAppointmentCommand>>();

        group.MapPost("/{appointmentId}/cancel", CancelAppointmentEndpoint.Handle)
            .WithName("CancelAppointment")
            .Produces<CancelAppointmentResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<CancelAppointmentCommand>>();

        group.MapGet("/{id}", GetAppointmentByIdEndpoint.Handle)
            .WithName("GetAppointmentById")
            .Produces<AppointmentDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<GetAppointmentByIdQuery>>();

        group.MapGet("/patient/{patientId}", GetPatientAppointmentsEndpoint.Handle)
            .WithName("GetPatientAppointments")
            .Produces<Common.Models.PaginatedList<AppointmentDto>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<GetPatientAppointmentsQuery>>();

        return group;
    }
}