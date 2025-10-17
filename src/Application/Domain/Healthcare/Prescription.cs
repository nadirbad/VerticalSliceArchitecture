using System.ComponentModel.DataAnnotations.Schema;

using VerticalSliceArchitecture.Application.Common;

namespace VerticalSliceArchitecture.Application.Domain.Healthcare;

public class Prescription : AuditableEntity, IHasDomainEvent
{
    public static Prescription Issue(
        Guid patientId,
        Guid doctorId,
        string medicationName,
        string dosage,
        string directions,
        int quantity,
        int numberOfRefills,
        int durationInDays)
    {
        return new Prescription(
            patientId,
            doctorId,
            medicationName,
            dosage,
            directions,
            quantity,
            numberOfRefills,
            durationInDays);
    }

    private Prescription()
    {
        // Private parameterless constructor for EF Core
    }

    private Prescription(
        Guid patientId,
        Guid doctorId,
        string medicationName,
        string dosage,
        string directions,
        int quantity,
        int numberOfRefills,
        int durationInDays)
    {
        // Validate medication name
        if (string.IsNullOrWhiteSpace(medicationName))
        {
            throw new ArgumentException("Medication name is required", nameof(medicationName));
        }

        if (medicationName.Length > 200)
        {
            throw new ArgumentException("Medication name cannot exceed 200 characters", nameof(medicationName));
        }

        // Validate dosage
        if (string.IsNullOrWhiteSpace(dosage))
        {
            throw new ArgumentException("Dosage is required", nameof(dosage));
        }

        if (dosage.Length > 50)
        {
            throw new ArgumentException("Dosage cannot exceed 50 characters", nameof(dosage));
        }

        // Validate directions
        if (string.IsNullOrWhiteSpace(directions))
        {
            throw new ArgumentException("Directions are required", nameof(directions));
        }

        if (directions.Length > 500)
        {
            throw new ArgumentException("Directions cannot exceed 500 characters", nameof(directions));
        }

        // Validate quantity
        if (quantity < 1 || quantity > 999)
        {
            throw new ArgumentException("Quantity must be between 1 and 999", nameof(quantity));
        }

        // Validate number of refills
        if (numberOfRefills < 0 || numberOfRefills > 12)
        {
            throw new ArgumentException("Number of refills must be between 0 and 12", nameof(numberOfRefills));
        }

        // Validate duration
        if (durationInDays < 1 || durationInDays > 365)
        {
            throw new ArgumentException("Duration must be between 1 and 365 days", nameof(durationInDays));
        }

        // Set properties
        PatientId = patientId;
        DoctorId = doctorId;
        MedicationName = medicationName.Trim();
        Dosage = dosage.Trim();
        Directions = directions.Trim();
        Quantity = quantity;
        NumberOfRefills = numberOfRefills;
        RemainingRefills = numberOfRefills;
        IssuedDateUtc = DateTime.UtcNow;
        ExpirationDateUtc = IssuedDateUtc.AddDays(durationInDays);
        Status = PrescriptionStatus.Active;

        // Raise domain event
        DomainEvents.Add(new PrescriptionIssuedEvent(
            Id,
            PatientId,
            DoctorId,
            MedicationName,
            Dosage,
            IssuedDateUtc,
            ExpirationDateUtc));
    }

    public Guid Id { get; internal set; }
    public Guid PatientId { get; private set; }
    public Guid DoctorId { get; private set; }
    public string MedicationName { get; private set; } = string.Empty;
    public string Dosage { get; private set; } = string.Empty;
    public string Directions { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public int NumberOfRefills { get; private set; }
    public int RemainingRefills { get; private set; }
    public DateTime IssuedDateUtc { get; private set; }
    public DateTime ExpirationDateUtc { get; private set; }
    public PrescriptionStatus Status { get; private set; }

    public Patient Patient { get; private set; } = null!;
    public Doctor Doctor { get; private set; } = null!;

    [NotMapped]
    public List<DomainEvent> DomainEvents { get; } = new List<DomainEvent>();

    public bool IsExpired => ExpirationDateUtc < DateTime.UtcNow;
    public bool IsDepleted => RemainingRefills <= 0;
}

public enum PrescriptionStatus
{
    Active = 1,
    Expired = 2,
    Depleted = 3,
}
