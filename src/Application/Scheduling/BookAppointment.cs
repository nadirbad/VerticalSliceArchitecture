using ErrorOr;

using FluentValidation;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using VerticalSliceArchitecture.Application.Common;
using VerticalSliceArchitecture.Application.Domain;
using VerticalSliceArchitecture.Application.Infrastructure.Persistence;

namespace VerticalSliceArchitecture.Application.Scheduling;

public static class BookAppointmentEndpoint
{
    public static async Task<IResult> Handle(
        BookAppointmentCommand command,
        ISender mediator)
    {
        var result = await mediator.Send(command);

        return result.Match(
            success => Results.Created($"/api/healthcare/appointments/{success.Id}", success),
            errors => MinimalApiProblemHelper.Problem(errors));
    }
}

public record BookAppointmentCommand(Guid PatientId, Guid DoctorId, DateTimeOffset Start, DateTimeOffset End, string? Notes) : IRequest<ErrorOr<BookAppointmentResult>>;

public record BookAppointmentResult(Guid Id, DateTime StartUtc, DateTime EndUtc);

internal sealed class BookAppointmentCommandValidator : AbstractValidator<BookAppointmentCommand>
{
    public BookAppointmentCommandValidator()
    {
        RuleFor(v => v.PatientId)
            .NotEmpty()
            .WithMessage("PatientId is required");

        RuleFor(v => v.DoctorId)
            .NotEmpty()
            .WithMessage("DoctorId is required");

        RuleFor(v => v.Start)
            .LessThan(v => v.End)
            .WithMessage("Start time must be before end time");

        RuleFor(v => v.End)
            .GreaterThanOrEqualTo(v => v.Start.AddMinutes(SchedulingPolicies.MinimumAppointmentDurationMinutes))
            .WithMessage($"Appointment must be at least {SchedulingPolicies.MinimumAppointmentDurationMinutes} minutes long");

        RuleFor(v => v.End)
            .LessThanOrEqualTo(v => v.Start.AddHours(SchedulingPolicies.MaximumAppointmentDurationHours))
            .WithMessage($"Appointment cannot be longer than {SchedulingPolicies.MaximumAppointmentDurationHours} hours");

        RuleFor(v => v.Start)
            .GreaterThan(DateTimeOffset.UtcNow.AddMinutes(SchedulingPolicies.MinimumBookingAdvanceMinutes))
            .WithMessage($"Appointment must be scheduled at least {SchedulingPolicies.MinimumBookingAdvanceMinutes} minutes in advance");

        RuleFor(v => v.Notes)
            .MaximumLength(SchedulingPolicies.MaxNotesLength)
            .WithMessage($"Notes cannot exceed {SchedulingPolicies.MaxNotesLength} characters");
    }
}

internal sealed class BookAppointmentCommandHandler(ApplicationDbContext context) : IRequestHandler<BookAppointmentCommand, ErrorOr<BookAppointmentResult>>
{
    private readonly ApplicationDbContext _context = context;

    public async Task<ErrorOr<BookAppointmentResult>> Handle(BookAppointmentCommand request, CancellationToken cancellationToken)
    {
        // Normalize to UTC
        var startUtc = request.Start.UtcDateTime;
        var endUtc = request.End.UtcDateTime;

        // Check if patient exists
        var patientExists = await _context.Patients
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.PatientId, cancellationToken);

        if (!patientExists)
        {
            return Error.NotFound("Appointment.PatientNotFound", $"Patient with ID {request.PatientId} not found");
        }

        // Check if doctor exists
        var doctorExists = await _context.Doctors
            .AsNoTracking()
            .AnyAsync(d => d.Id == request.DoctorId, cancellationToken);

        if (!doctorExists)
        {
            return Error.NotFound("Appointment.DoctorNotFound", $"Doctor with ID {request.DoctorId} not found");
        }

        // Check for overlapping appointments for the doctor
        // NOTE: This check-then-act pattern has a race condition window. Two concurrent requests
        // could both pass the overlap check before either inserts. For production systems with
        // SQL Server, wrap in a SERIALIZABLE transaction. The optimistic concurrency (RowVersion)
        // can help detect concurrent modifications but won't prevent overlapping inserts.
        // Consider using a database exclusion constraint (PostgreSQL) or application-level locking.
        var hasOverlap = await _context.Appointments
            .AsNoTracking()
            .AnyAsync(
                a => a.DoctorId == request.DoctorId
                     && a.Status == AppointmentStatus.Scheduled
                     && a.StartUtc < endUtc
                     && a.EndUtc > startUtc,
                cancellationToken);

        if (hasOverlap)
        {
            return Error.Conflict("Appointment.Conflict", "Doctor has a conflicting appointment during the requested time");
        }

        // Create the appointment using factory method
        // Note: Domain event (AppointmentBookedEvent) is raised inside Appointment.Schedule()
        // Note: Domain validates invariants (start < end). Validator also checks this for fast-fail UX,
        // but domain is the authoritative source of truth.
        Appointment appointment;
        try
        {
            appointment = Appointment.Schedule(
                request.PatientId,
                request.DoctorId,
                startUtc,
                endUtc,
                request.Notes);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation("Appointment.ValidationFailed", ex.Message);
        }

        _context.Appointments.Add(appointment);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Could be a concurrent insert - return conflict
            return Error.Conflict("Appointment.Conflict", "Doctor has a conflicting appointment during the requested time");
        }

        return new BookAppointmentResult(appointment.Id, appointment.StartUtc, appointment.EndUtc);
    }
}