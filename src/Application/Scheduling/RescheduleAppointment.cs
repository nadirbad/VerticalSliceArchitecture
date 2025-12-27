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
            .GreaterThanOrEqualTo(v => v.NewStart.AddMinutes(10))
            .WithMessage("Appointment must be at least 10 minutes long");

        RuleFor(v => v.NewEnd)
            .LessThanOrEqualTo(v => v.NewStart.AddHours(8))
            .WithMessage("Appointment cannot be longer than 8 hours");

        RuleFor(v => v.NewStart)
            .GreaterThan(DateTimeOffset.UtcNow.AddHours(2))
            .WithMessage("Appointment must be rescheduled at least 2 hours in advance");

        RuleFor(v => v.Reason)
            .MaximumLength(512)
            .WithMessage("Reason cannot exceed 512 characters");
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

        // Store original times for response and event
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

        // Enforce 24-hour rule
        if (DateTime.UtcNow >= appointment.StartUtc.AddHours(-24))
        {
            return Error.Validation("Appointment.RescheduleWindowClosed", "Appointments cannot be rescheduled within 24 hours of the start time");
        }

        // Check doctor availability - exclude current appointment
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
        appointment.Reschedule(newStartUtc, newEndUtc, request.Reason);

        // Raise domain event
        appointment.DomainEvents.Add(
            new AppointmentRescheduledEvent(
                appointment.Id,
                previousStartUtc,
                previousEndUtc,
                appointment.StartUtc,
                appointment.EndUtc));

        // Persist changes
        await _context.SaveChangesAsync(cancellationToken);

        // Return result
        return new RescheduleAppointmentResult(
            appointment.Id,
            appointment.StartUtc,
            appointment.EndUtc,
            previousStartUtc,
            previousEndUtc);
    }
}