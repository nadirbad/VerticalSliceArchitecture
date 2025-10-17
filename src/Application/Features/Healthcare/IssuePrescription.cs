using ErrorOr;

using FluentValidation;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

using VerticalSliceArchitecture.Application.Common;
using VerticalSliceArchitecture.Application.Domain.Healthcare;
using VerticalSliceArchitecture.Application.Infrastructure.Persistence;

namespace VerticalSliceArchitecture.Application.Features.Healthcare;

public static class IssuePrescriptionEndpoint
{
    public static void MapIssuePrescription(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/prescriptions", async (
            IssuePrescriptionCommand command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);

            return result.Match(
                prescription => Results.Created($"/api/prescriptions/{prescription.Id}", prescription),
                errors => MinimalApiProblemHelper.Problem(errors));
        })
        .WithName("IssuePrescription")
        .Produces<PrescriptionResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithTags("Prescriptions");
    }
}

public record IssuePrescriptionCommand(
    Guid PatientId,
    Guid DoctorId,
    string MedicationName,
    string Dosage,
    string Directions,
    int Quantity,
    int NumberOfRefills,
    int DurationInDays) : IRequest<ErrorOr<PrescriptionResponse>>;

internal sealed class IssuePrescriptionCommandValidator : AbstractValidator<IssuePrescriptionCommand>
{
    public IssuePrescriptionCommandValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("Patient ID is required");

        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("Doctor ID is required");

        RuleFor(x => x.MedicationName)
            .NotEmpty().WithMessage("Medication name is required")
            .MaximumLength(200).WithMessage("Medication name cannot exceed 200 characters");

        RuleFor(x => x.Dosage)
            .NotEmpty().WithMessage("Dosage is required")
            .MaximumLength(50).WithMessage("Dosage cannot exceed 50 characters");

        RuleFor(x => x.Directions)
            .NotEmpty().WithMessage("Directions are required")
            .MaximumLength(500).WithMessage("Directions cannot exceed 500 characters");

        RuleFor(x => x.Quantity)
            .InclusiveBetween(1, 999).WithMessage("Quantity must be between 1 and 999");

        RuleFor(x => x.NumberOfRefills)
            .InclusiveBetween(0, 12).WithMessage("Number of refills must be between 0 and 12");

        RuleFor(x => x.DurationInDays)
            .InclusiveBetween(1, 365).WithMessage("Duration must be between 1 and 365 days");
    }
}

public record PrescriptionResponse(
    Guid Id,
    Guid PatientId,
    string PatientName,
    Guid DoctorId,
    string DoctorName,
    string MedicationName,
    string Dosage,
    string Directions,
    int Quantity,
    int NumberOfRefills,
    int RemainingRefills,
    DateTime IssuedDateUtc,
    DateTime ExpirationDateUtc,
    string Status,
    bool IsExpired,
    bool IsDepleted);

internal sealed class IssuePrescriptionCommandHandler : IRequestHandler<IssuePrescriptionCommand, ErrorOr<PrescriptionResponse>>
{
    private readonly ApplicationDbContext _context;

    public IssuePrescriptionCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ErrorOr<PrescriptionResponse>> Handle(IssuePrescriptionCommand request, CancellationToken cancellationToken)
    {
        // Verify patient exists
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken);

        if (patient == null)
        {
            return Error.NotFound(code: "Patient.NotFound", description: $"Patient with ID {request.PatientId} was not found");
        }

        // Verify doctor exists
        var doctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.Id == request.DoctorId, cancellationToken);

        if (doctor == null)
        {
            return Error.NotFound(code: "Doctor.NotFound", description: $"Doctor with ID {request.DoctorId} was not found");
        }

        // Issue the prescription using the domain factory method
        Prescription prescription;
        try
        {
            prescription = Prescription.Issue(
                request.PatientId,
                request.DoctorId,
                request.MedicationName,
                request.Dosage,
                request.Directions,
                request.Quantity,
                request.NumberOfRefills,
                request.DurationInDays);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(code: "Prescription.ValidationFailed", description: ex.Message);
        }

        // Add to context and save
        _context.Prescriptions.Add(prescription);
        await _context.SaveChangesAsync(cancellationToken);

        // Map to response DTO
        var response = new PrescriptionResponse(
            prescription.Id,
            prescription.PatientId,
            patient.FullName,
            prescription.DoctorId,
            doctor.FullName,
            prescription.MedicationName,
            prescription.Dosage,
            prescription.Directions,
            prescription.Quantity,
            prescription.NumberOfRefills,
            prescription.RemainingRefills,
            prescription.IssuedDateUtc,
            prescription.ExpirationDateUtc,
            prescription.Status.ToString(),
            prescription.IsExpired,
            prescription.IsDepleted);

        return response;
    }
}