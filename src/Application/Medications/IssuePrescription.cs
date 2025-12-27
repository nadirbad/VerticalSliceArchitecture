using ErrorOr;

using FluentValidation;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using VerticalSliceArchitecture.Application.Common;
using VerticalSliceArchitecture.Application.Domain;
using VerticalSliceArchitecture.Application.Infrastructure.Persistence;

using static VerticalSliceArchitecture.Application.Domain.PrescriptionPolicies;

namespace VerticalSliceArchitecture.Application.Medications;

public static class IssuePrescriptionEndpoint
{
    public static async Task<IResult> Handle(
        IssuePrescriptionCommand command,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Match(
            prescription => Results.Created($"/api/prescriptions/{prescription.Id}", prescription),
            errors => MinimalApiProblemHelper.Problem(errors));
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
            .MaximumLength(MaxMedicationNameLength).WithMessage($"Medication name cannot exceed {MaxMedicationNameLength} characters");

        RuleFor(x => x.Dosage)
            .NotEmpty().WithMessage("Dosage is required")
            .MaximumLength(MaxDosageLength).WithMessage($"Dosage cannot exceed {MaxDosageLength} characters");

        RuleFor(x => x.Directions)
            .NotEmpty().WithMessage("Directions are required")
            .MaximumLength(MaxDirectionsLength).WithMessage($"Directions cannot exceed {MaxDirectionsLength} characters");

        RuleFor(x => x.Quantity)
            .InclusiveBetween(MinQuantity, MaxQuantity).WithMessage($"Quantity must be between {MinQuantity} and {MaxQuantity}");

        RuleFor(x => x.NumberOfRefills)
            .InclusiveBetween(MinRefills, MaxRefills).WithMessage($"Number of refills must be between {MinRefills} and {MaxRefills}");

        RuleFor(x => x.DurationInDays)
            .InclusiveBetween(MinDurationDays, MaxDurationDays).WithMessage($"Duration must be between {MinDurationDays} and {MaxDurationDays} days");
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

internal sealed class IssuePrescriptionCommandHandler(ApplicationDbContext context)
    : IRequestHandler<IssuePrescriptionCommand, ErrorOr<PrescriptionResponse>>
{
    private readonly ApplicationDbContext _context = context;

    public async Task<ErrorOr<PrescriptionResponse>> Handle(IssuePrescriptionCommand request, CancellationToken cancellationToken)
    {
        // Verify patient exists
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken);

        if (patient == null)
        {
            return Error.NotFound(code: "Prescription.PatientNotFound", description: $"Patient with ID {request.PatientId} was not found");
        }

        // Verify doctor exists
        var doctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.Id == request.DoctorId, cancellationToken);

        if (doctor == null)
        {
            return Error.NotFound(code: "Prescription.DoctorNotFound", description: $"Doctor with ID {request.DoctorId} was not found");
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
        // Note: Domain event (PrescriptionIssuedEvent) is already raised inside Prescription.Issue()
        // and will be dispatched by ApplicationDbContext.SaveChangesAsync()
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