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
public static class GetAppointmentByIdEndpoint
{
    public static async Task<IResult> Handle(
        Guid id,
        ISender mediator)
    {
        var query = new GetAppointmentByIdQuery(id);
        var result = await mediator.Send(query);

        return result.Match(
            appointment => Results.Ok(appointment),
            errors => MinimalApiProblemHelper.Problem(errors));
    }
}

public record GetAppointmentByIdQuery(Guid Id) : IRequest<ErrorOr<AppointmentDto>>;

public record AppointmentDto(
    Guid Id,
    Guid PatientId,
    string PatientFullName,
    Guid DoctorId,
    string DoctorFullName,
    string DoctorSpecialty,
    DateTime StartUtc,
    DateTime EndUtc,
    AppointmentStatus Status,
    string? Notes,
    DateTime? CompletedUtc,
    DateTime? CancelledUtc,
    string? CancellationReason,
    DateTime Created,
    DateTime? LastModified);

internal sealed class GetAppointmentByIdQueryValidator : AbstractValidator<GetAppointmentByIdQuery>
{
    public GetAppointmentByIdQueryValidator()
    {
        RuleFor(v => v.Id)
            .NotEmpty()
            .WithMessage("Id is required");
    }
}

internal sealed class GetAppointmentByIdQueryHandler(ApplicationDbContext context)
    : IRequestHandler<GetAppointmentByIdQuery, ErrorOr<AppointmentDto>>
{
    private readonly ApplicationDbContext _context = context;

    public async Task<ErrorOr<AppointmentDto>> Handle(
        GetAppointmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .AsNoTracking()
            .Where(a => a.Id == request.Id)
            .Select(a => new AppointmentDto(
                a.Id,
                a.PatientId,
                a.Patient.FullName,
                a.DoctorId,
                a.Doctor.FullName,
                a.Doctor.Specialty,
                a.StartUtc,
                a.EndUtc,
                a.Status,
                a.Notes,
                a.CompletedUtc,
                a.CancelledUtc,
                a.CancellationReason,
                a.Created,
                a.LastModified))
            .FirstOrDefaultAsync(cancellationToken);

        if (appointment is null)
        {
            return Error.NotFound(
                "Appointment.NotFound",
                $"Appointment with ID {request.Id} not found");
        }

        return appointment;
    }
}