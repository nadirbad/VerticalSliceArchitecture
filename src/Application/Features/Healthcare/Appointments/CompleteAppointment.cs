using ErrorOr;

using FluentValidation;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using VerticalSliceArchitecture.Application.Common;
using VerticalSliceArchitecture.Application.Domain.Healthcare;
using VerticalSliceArchitecture.Application.Infrastructure.Persistence;

namespace VerticalSliceArchitecture.Application.Features.Healthcare.Appointments;

// Minimal API Endpoint Handler
public static class CompleteAppointmentEndpoint
{
    public static async Task<IResult> Handle(
        Guid appointmentId,
        CompleteAppointmentCommand command,
        ISender mediator)
    {
        // Validate that the route parameter matches the command
        if (appointmentId != command.AppointmentId)
        {
            return Results.BadRequest(new { error = "Route appointmentId does not match command AppointmentId" });
        }

        var result = await mediator.Send(command);

        return result.Match(
            success => Results.Ok(success),
            errors => MinimalApiProblemHelper.Problem(errors));
    }
}

public record CompleteAppointmentCommand(
    Guid AppointmentId,
    string? Notes) : IRequest<ErrorOr<CompleteAppointmentResult>>;

public record CompleteAppointmentResult(
    Guid Id,
    AppointmentStatus Status,
    DateTime CompletedUtc,
    string? Notes);

// Domain Event
public class AppointmentCompletedEvent(
    Guid appointmentId,
    Guid patientId,
    Guid doctorId,
    DateTime completedUtc,
    string? notes) : DomainEvent
{
    public Guid AppointmentId { get; } = appointmentId;
    public Guid PatientId { get; } = patientId;
    public Guid DoctorId { get; } = doctorId;
    public DateTime CompletedUtc { get; } = completedUtc;
    public string? Notes { get; } = notes;
}

internal sealed class CompleteAppointmentCommandValidator : AbstractValidator<CompleteAppointmentCommand>
{
    public CompleteAppointmentCommandValidator()
    {
        RuleFor(v => v.AppointmentId)
            .NotEmpty()
            .WithMessage("AppointmentId is required");

        RuleFor(v => v.Notes)
            .MaximumLength(1024)
            .WithMessage("Notes cannot exceed 1024 characters");
    }
}

internal sealed class CompleteAppointmentCommandHandler(ApplicationDbContext context) : IRequestHandler<CompleteAppointmentCommand, ErrorOr<CompleteAppointmentResult>>
{
    private readonly ApplicationDbContext _context = context;

    public async Task<ErrorOr<CompleteAppointmentResult>> Handle(CompleteAppointmentCommand request, CancellationToken cancellationToken)
    {
        // Load appointment
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken);

        if (appointment is null)
        {
            return Error.NotFound("Appointment.NotFound", $"Appointment with ID {request.AppointmentId} not found");
        }

        // Try to complete the appointment - let domain method handle validation
        try
        {
            appointment.Complete(request.Notes);
        }
        catch (InvalidOperationException ex)
        {
            return Error.Validation("Appointment.CannotComplete", ex.Message);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation("Appointment.ValidationFailed", ex.Message);
        }

        // Raise domain event
        appointment.DomainEvents.Add(
            new AppointmentCompletedEvent(
                appointment.Id,
                appointment.PatientId,
                appointment.DoctorId,
                appointment.CompletedUtc!.Value,
                appointment.Notes));

        // Persist changes
        await _context.SaveChangesAsync(cancellationToken);

        // Return result
        return new CompleteAppointmentResult(
            appointment.Id,
            appointment.Status,
            appointment.CompletedUtc!.Value,
            appointment.Notes);
    }
}