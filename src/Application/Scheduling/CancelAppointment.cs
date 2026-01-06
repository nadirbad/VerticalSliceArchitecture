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
/// Cancel an existing appointment.
/// </summary>
public static class CancelAppointment
{
    public record Command(Guid AppointmentId, string Reason)
        : IRequest<ErrorOr<Result>>;

    public record Result(
        Guid Id,
        AppointmentStatus Status,
        DateTime CancelledUtc,
        string CancellationReason);

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

            RuleFor(v => v.Reason)
                .NotEmpty()
                .WithMessage("Cancellation reason is required");

            RuleFor(v => v.Reason)
                .MaximumLength(MaxCancellationReasonLength)
                .WithMessage($"Cancellation reason cannot exceed {MaxCancellationReasonLength} characters");
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
                appointment.CancelledUtc!.Value,
                appointment.CancellationReason!);
        }
    }
}