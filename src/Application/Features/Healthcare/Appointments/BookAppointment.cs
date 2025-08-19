using ErrorOr;

using FluentValidation;

using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using VerticalSliceArchitecture.Application.Common;
using VerticalSliceArchitecture.Application.Domain.Healthcare;
using VerticalSliceArchitecture.Application.Infrastructure.Persistence;

namespace VerticalSliceArchitecture.Application.Features.Healthcare.Appointments;

public class BookAppointmentController : ApiControllerBase
{
    [HttpPost("/api/healthcare/appointments")]
    public async Task<IActionResult> Book(BookAppointmentCommand command)
    {
        var result = await Mediator.Send(command);

        return result.Match(
            success => Created($"/api/healthcare/appointments/{success.Id}", success),
            Problem);
    }
}

public record BookAppointmentCommand(int PatientId, int DoctorId, DateTimeOffset Start, DateTimeOffset End, string? Notes) : IRequest<ErrorOr<BookAppointmentResult>>;

public record BookAppointmentResult(int Id, DateTime StartUtc, DateTime EndUtc);

internal sealed class BookAppointmentCommandValidator : AbstractValidator<BookAppointmentCommand>
{
    public BookAppointmentCommandValidator()
    {
        RuleFor(v => v.PatientId)
            .GreaterThan(0)
            .WithMessage("PatientId must be greater than 0");

        RuleFor(v => v.DoctorId)
            .GreaterThan(0)
            .WithMessage("DoctorId must be greater than 0");

        RuleFor(v => v.Start)
            .LessThan(v => v.End)
            .WithMessage("Start time must be before end time");

        RuleFor(v => v.End)
            .GreaterThan(v => v.Start.AddMinutes(10))
            .WithMessage("Appointment must be at least 10 minutes long");

        RuleFor(v => v.End)
            .LessThanOrEqualTo(v => v.Start.AddHours(8))
            .WithMessage("Appointment cannot be longer than 8 hours");

        RuleFor(v => v.Start)
            .GreaterThan(DateTimeOffset.UtcNow.AddMinutes(15))
            .WithMessage("Appointment must be scheduled at least 15 minutes in advance");

        RuleFor(v => v.Notes)
            .MaximumLength(1024)
            .WithMessage("Notes cannot exceed 1024 characters");
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
        var hasOverlap = await _context.Appointments
            .AsNoTracking()
            .AnyAsync(
                a => a.DoctorId == request.DoctorId
                     && (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Rescheduled)
                     && a.StartUtc < endUtc
                     && a.EndUtc > startUtc,
                cancellationToken);

        if (hasOverlap)
        {
            return Error.Conflict("Appointment.Conflict", $"Doctor has a conflicting appointment during the requested time");
        }

        // Create the appointment
        var appointment = new Appointment
        {
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            StartUtc = startUtc,
            EndUtc = endUtc,
            Status = AppointmentStatus.Scheduled,
            Notes = request.Notes,
        };

        // Add domain event
        appointment.DomainEvents.Add(
            new AppointmentBookedEvent(
                appointment.Id,
                appointment.PatientId,
                appointment.DoctorId,
                appointment.StartUtc,
                appointment.EndUtc));

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync(cancellationToken);

        return new BookAppointmentResult(appointment.Id, appointment.StartUtc, appointment.EndUtc);
    }
}
