using ErrorOr;

using FluentValidation;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using VerticalSliceArchitecture.Application.Common;
using VerticalSliceArchitecture.Application.Domain;
using VerticalSliceArchitecture.Application.Infrastructure.Persistence;

using static VerticalSliceArchitecture.Application.Domain.SchedulingPolicies;

namespace VerticalSliceArchitecture.Application.Scheduling;

/// <summary>
/// Complete an existing appointment.
/// </summary>
public static class CompleteAppointment
{
    public record Command(Guid AppointmentId, string? Notes)
        : IRequest<ErrorOr<Result>>;

    public record Result(
        Guid Id,
        AppointmentStatus Status,
        DateTime CompletedUtc,
        string? Notes);

    internal static class Endpoint
    {
        internal static async Task<IResult> Handle(
            Guid appointmentId,
            Command command,
            ISender mediator)
        {
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

    internal sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(v => v.AppointmentId)
                .NotEmpty()
                .WithMessage("AppointmentId is required");

            RuleFor(v => v.Notes)
                .MaximumLength(MaxNotesLength)
                .WithMessage($"Notes cannot exceed {MaxNotesLength} characters");
        }
    }

    internal sealed class Handler(ApplicationDbContext context)
        : IRequestHandler<Command, ErrorOr<Result>>
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<ErrorOr<Result>> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken);

            if (appointment is null)
            {
                return Error.NotFound(
                    "Appointment.NotFound",
                    $"Appointment with ID {request.AppointmentId} not found");
            }

            // Let domain method handle business rule validation
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

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Error.Conflict(
                    "Appointment.ConcurrencyConflict",
                    "The appointment was modified by another user. Please refresh and try again.");
            }

            return new Result(
                appointment.Id,
                appointment.Status,
                appointment.CompletedUtc!.Value,
                appointment.Notes);
        }
    }
}