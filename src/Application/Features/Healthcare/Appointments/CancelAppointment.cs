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
public static class CancelAppointmentEndpoint
{
    public static async Task<IResult> Handle(
        Guid appointmentId,
        CancelAppointmentCommand command,
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

public record CancelAppointmentCommand(
    Guid AppointmentId,
    string Reason) : IRequest<ErrorOr<CancelAppointmentResult>>;

public record CancelAppointmentResult(
    Guid Id,
    AppointmentStatus Status,
    DateTime CancelledUtc,
    string CancellationReason);

// Domain Event
public class AppointmentCancelledEvent(
    Guid appointmentId,
    Guid patientId,
    Guid doctorId,
    DateTime cancelledUtc,
    string cancellationReason) : DomainEvent
{
    public Guid AppointmentId { get; } = appointmentId;
    public Guid PatientId { get; } = patientId;
    public Guid DoctorId { get; } = doctorId;
    public DateTime CancelledUtc { get; } = cancelledUtc;
    public string CancellationReason { get; } = cancellationReason;
}

internal sealed class CancelAppointmentCommandValidator : AbstractValidator<CancelAppointmentCommand>
{
    public CancelAppointmentCommandValidator()
    {
        RuleFor(v => v.AppointmentId)
            .NotEmpty()
            .WithMessage("AppointmentId is required");

        RuleFor(v => v.Reason)
            .NotEmpty()
            .WithMessage("Cancellation reason is required");

        RuleFor(v => v.Reason)
            .MaximumLength(512)
            .WithMessage("Cancellation reason cannot exceed 512 characters");
    }
}

internal sealed class CancelAppointmentCommandHandler(ApplicationDbContext context) : IRequestHandler<CancelAppointmentCommand, ErrorOr<CancelAppointmentResult>>
{
    private readonly ApplicationDbContext _context = context;

    public async Task<ErrorOr<CancelAppointmentResult>> Handle(CancelAppointmentCommand request, CancellationToken cancellationToken)
    {
        // Load appointment
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken);

        if (appointment is null)
        {
            return Error.NotFound("Appointment.NotFound", $"Appointment with ID {request.AppointmentId} not found");
        }

        // Try to cancel the appointment - let domain method handle validation
        try
        {
            appointment.Cancel(request.Reason);
        }
        catch (InvalidOperationException ex)
        {
            return Error.Validation("Appointment.CannotCancel", ex.Message);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation("Appointment.ValidationFailed", ex.Message);
        }

        // Raise domain event
        appointment.DomainEvents.Add(
            new AppointmentCancelledEvent(
                appointment.Id,
                appointment.PatientId,
                appointment.DoctorId,
                appointment.CancelledUtc!.Value,
                appointment.CancellationReason!));

        // Persist changes
        await _context.SaveChangesAsync(cancellationToken);

        // Return result
        return new CancelAppointmentResult(
            appointment.Id,
            appointment.Status,
            appointment.CancelledUtc!.Value,
            appointment.CancellationReason!);
    }
}