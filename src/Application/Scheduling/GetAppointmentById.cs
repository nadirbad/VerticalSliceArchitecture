using ErrorOr;

using FluentValidation;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using VerticalSliceArchitecture.Application.Common;
using VerticalSliceArchitecture.Application.Infrastructure.Persistence;

namespace VerticalSliceArchitecture.Application.Scheduling;

/// <summary>
/// Get a single appointment by ID.
/// </summary>
public static class GetAppointmentById
{
    public record Query(Guid Id) : IRequest<ErrorOr<AppointmentDto>>;

    internal static class Endpoint
    {
        internal static async Task<IResult> Handle(
            Guid id,
            ISender mediator)
        {
            var query = new Query(id);
            var result = await mediator.Send(query);

            return result.Match(
                appointment => Results.Ok(appointment),
                errors => MinimalApiProblemHelper.Problem(errors));
        }
    }

    internal sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(v => v.Id)
                .NotEmpty()
                .WithMessage("Id is required");
        }
    }

    internal sealed class Handler(ApplicationDbContext context)
        : IRequestHandler<Query, ErrorOr<AppointmentDto>>
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<ErrorOr<AppointmentDto>> Handle(
            Query request,
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
}