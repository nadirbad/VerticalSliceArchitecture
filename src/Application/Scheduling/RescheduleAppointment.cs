using ErrorOr;

using FluentValidation;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using VerticalSliceArchitecture.Application.Common;
using VerticalSliceArchitecture.Application.Domain;
using VerticalSliceArchitecture.Application.Infrastructure.Persistence;

namespace VerticalSliceArchitecture.Application.Scheduling;

// Minimal API Endpoint Handler
public static class RescheduleAppointmentEndpoint
{
    public static async Task<IResult> Handle(
        Guid appointmentId,
        RescheduleAppointmentCommand command,
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

public record RescheduleAppointmentCommand(
    Guid AppointmentId,
    DateTimeOffset NewStart,
    DateTimeOffset NewEnd,
    string? Reason) : IRequest<ErrorOr<RescheduleAppointmentResult>>;

public record RescheduleAppointmentResult(
    Guid Id,
    DateTime StartUtc,
    DateTime EndUtc,
    DateTime PreviousStartUtc,
    DateTime PreviousEndUtc);

internal sealed class RescheduleAppointmentCommandValidator : AbstractValidator<RescheduleAppointmentCommand>
{
    public RescheduleAppointmentCommandValidator()
    {
        RuleFor(v => v.AppointmentId)
            .NotEmpty()
            .WithMessage("AppointmentId is required");

        RuleFor(v => v.NewStart)
            .LessThan(v => v.NewEnd)
            .WithMessage("New start time must be before new end time");

        RuleFor(v => v.NewEnd)
            .GreaterThanOrEqualTo(v => v.NewStart.AddMinutes(SchedulingPolicies.MinimumAppointmentDurationMinutes))
            .WithMessage($"Appointment must be at least {SchedulingPolicies.MinimumAppointmentDurationMinutes} minutes long");

        RuleFor(v => v.NewEnd)
            .LessThanOrEqualTo(v => v.NewStart.AddHours(SchedulingPolicies.MaximumAppointmentDurationHours))
            .WithMessage($"Appointment cannot be longer than {SchedulingPolicies.MaximumAppointmentDurationHours} hours");

        RuleFor(v => v.NewStart)
            .GreaterThan(DateTimeOffset.UtcNow.AddHours(SchedulingPolicies.MinimumRescheduleAdvanceHours))
            .WithMessage($"Appointment must be rescheduled at least {SchedulingPolicies.MinimumRescheduleAdvanceHours} hours in advance");

        RuleFor(v => v.Reason)
            .MaximumLength(SchedulingPolicies.MaxRescheduleReasonLength)
            .WithMessage($"Reason cannot exceed {SchedulingPolicies.MaxRescheduleReasonLength} characters");
    }
}

internal sealed class RescheduleAppointmentCommandHandler(ApplicationDbContext context) : IRequestHandler<RescheduleAppointmentCommand, ErrorOr<RescheduleAppointmentResult>>
{
    private readonly ApplicationDbContext _context = context;

    public async Task<ErrorOr<RescheduleAppointmentResult>> Handle(RescheduleAppointmentCommand request, CancellationToken cancellationToken)
    {
        // Normalize to UTC
        var newStartUtc = request.NewStart.UtcDateTime;
        var newEndUtc = request.NewEnd.UtcDateTime;

        // Load appointment
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken);

        if (appointment is null)
        {
            return Error.NotFound("Appointment.NotFound", $"Appointment with ID {request.AppointmentId} not found");
        }

        // Store original times for response
        var previousStartUtc = appointment.StartUtc;
        var previousEndUtc = appointment.EndUtc;

        // Check appointment status
        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            return Error.Validation("Appointment.CannotRescheduleCancelled", "Cannot reschedule a cancelled appointment");
        }

        if (appointment.Status == AppointmentStatus.Completed)
        {
            return Error.Validation("Appointment.CannotRescheduleCompleted", "Cannot reschedule a completed appointment");
        }

        // Enforce reschedule window cutoff rule
        if (DateTime.UtcNow >= appointment.StartUtc.AddHours(-SchedulingPolicies.RescheduleWindowCutoffHours))
        {
            return Error.Validation("Appointment.RescheduleWindowClosed", $"Appointments cannot be rescheduled within {SchedulingPolicies.RescheduleWindowCutoffHours} hours of the start time");
        }

        // Check doctor availability - exclude current appointment
        // NOTE: This check-then-act pattern has a race condition window (see BookAppointment.cs for details)
        var hasOverlap = await _context.Appointments
            .AsNoTracking()
            .AnyAsync(
                a => a.Id != request.AppointmentId
                     && a.DoctorId == appointment.DoctorId
                     && (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Rescheduled)
                     && a.StartUtc < newEndUtc
                     && a.EndUtc > newStartUtc,
                cancellationToken);

        if (hasOverlap)
        {
            return Error.Conflict("Appointment.Conflict", "Doctor has a conflicting appointment during the requested time");
        }

        // Update appointment via domain method
        // Note: Domain event (AppointmentRescheduledEvent) is raised inside Appointment.Reschedule()
        appointment.Reschedule(newStartUtc, newEndUtc, request.Reason);

        try
        {
            // Persist changes
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Optimistic concurrency conflict - another user modified this appointment
            return Error.Conflict("Appointment.ConcurrencyConflict", "The appointment was modified by another user. Please refresh and try again.");
        }
        catch (DbUpdateException)
        {
            // Could be a concurrent insert causing overlap - return conflict
            return Error.Conflict("Appointment.Conflict", "Doctor has a conflicting appointment during the requested time");
        }

        // Return result
        return new RescheduleAppointmentResult(
            appointment.Id,
            appointment.StartUtc,
            appointment.EndUtc,
            previousStartUtc,
            previousEndUtc);
    }
}