using ErrorOr;

using FluentValidation;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using VerticalSliceArchitecture.Application.Common;
using VerticalSliceArchitecture.Application.Common.Mappings;
using VerticalSliceArchitecture.Application.Common.Models;
using VerticalSliceArchitecture.Application.Domain;
using VerticalSliceArchitecture.Application.Infrastructure.Persistence;

namespace VerticalSliceArchitecture.Application.Scheduling;

// Minimal API Endpoint Handler
public static class GetDoctorAppointmentsEndpoint
{
    public static async Task<IResult> Handle(
        Guid doctorId,
        AppointmentStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageNumber = 1,
        int pageSize = 10,
        ISender mediator = null!)
    {
        var query = new GetDoctorAppointmentsQuery(
            doctorId,
            status,
            startDate,
            endDate,
            pageNumber,
            pageSize);

        var result = await mediator.Send(query);

        return result.Match(
            appointments => Results.Ok(appointments),
            errors => MinimalApiProblemHelper.Problem(errors));
    }
}

public record GetDoctorAppointmentsQuery(
    Guid DoctorId,
    AppointmentStatus? Status,
    DateTime? StartDate,
    DateTime? EndDate,
    int PageNumber,
    int PageSize) : IRequest<ErrorOr<PaginatedList<AppointmentDto>>>;

internal sealed class GetDoctorAppointmentsQueryValidator : AbstractValidator<GetDoctorAppointmentsQuery>
{
    public GetDoctorAppointmentsQueryValidator()
    {
        RuleFor(v => v.DoctorId)
            .NotEmpty()
            .WithMessage("DoctorId is required");

        RuleFor(v => v.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber must be at least 1");

        RuleFor(v => v.PageSize)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageSize must be at least 1")
            .LessThanOrEqualTo(100)
            .WithMessage("PageSize must not exceed 100");

        RuleFor(v => v)
            .Must(v => !v.StartDate.HasValue || !v.EndDate.HasValue || v.StartDate.Value <= v.EndDate.Value)
            .WithMessage("StartDate must be before or equal to EndDate")
            .WithName("EndDate");
    }
}

internal sealed class GetDoctorAppointmentsQueryHandler(ApplicationDbContext context)
    : IRequestHandler<GetDoctorAppointmentsQuery, ErrorOr<PaginatedList<AppointmentDto>>>
{
    private readonly ApplicationDbContext _context = context;

    public async Task<ErrorOr<PaginatedList<AppointmentDto>>> Handle(
        GetDoctorAppointmentsQuery request,
        CancellationToken cancellationToken)
    {
        // Check if doctor exists
        var doctorExists = await _context.Doctors
            .AsNoTracking()
            .AnyAsync(d => d.Id == request.DoctorId, cancellationToken);

        if (!doctorExists)
        {
            return Error.NotFound(
                "Doctor.NotFound",
                $"Doctor with ID {request.DoctorId} not found");
        }

        // Build query with filters
        var query = _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .AsNoTracking()
            .Where(a => a.DoctorId == request.DoctorId);

        // Apply status filter
        if (request.Status.HasValue)
        {
            query = query.Where(a => a.Status == request.Status.Value);
        }

        // Apply date range filters
        if (request.StartDate.HasValue)
        {
            query = query.Where(a => a.StartUtc >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(a => a.EndUtc <= request.EndDate.Value);
        }

        // Sort by StartUtc ascending (chronological schedule - earliest first)
        query = query.OrderBy(a => a.StartUtc);

        // Project to DTO and paginate
        var paginatedResult = await query
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
            .PaginatedListAsync(request.PageNumber, request.PageSize);

        return paginatedResult;
    }
}