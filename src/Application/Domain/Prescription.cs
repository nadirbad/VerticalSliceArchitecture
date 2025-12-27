using System.ComponentModel.DataAnnotations.Schema;

using VerticalSliceArchitecture.Application.Common;

namespace VerticalSliceArchitecture.Application.Domain;

public class Prescription : AuditableEntity, IHasDomainEvent
{
    /// <summary>
    /// Issues a new prescription.
    /// </summary>
    /// <param name="patientId">The patient receiving the prescription.</param>
    /// <param name="doctorId">The prescribing doctor.</param>
    /// <param name="medicationName">Name of the medication.</param>
    /// <param name="dosage">Dosage instructions.</param>
    /// <param name="directions">Administration directions.</param>
    /// <param name="quantity">Quantity to dispense.</param>
    /// <param name="numberOfRefills">Number of refills allowed.</param>
    /// <param name="durationInDays">Duration of the prescription in days.</param>
    /// <param name="issuedAtUtc">Optional timestamp for when prescription was issued. Defaults to DateTime.UtcNow if not provided.</param>
    public static Prescription Issue(
        Guid patientId,
        Guid doctorId,
        string medicationName,
        string dosage,
        string directions,
        int quantity,
        int numberOfRefills,
        int durationInDays,
        DateTime? issuedAtUtc = null)
    {
        return new Prescription(
            patientId,
            doctorId,
            medicationName,
            dosage,
            directions,
            quantity,
            numberOfRefills,
            durationInDays,
            issuedAtUtc);
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
        int durationInDays,
        DateTime? issuedAtUtc)
    {
        // Validate medication name
        if (string.IsNullOrWhiteSpace(medicationName))
        {
            throw new ArgumentException("Medication name is required", nameof(medicationName));
        }

        if (medicationName.Length > PrescriptionPolicies.MaxMedicationNameLength)
        {
            throw new ArgumentException($"Medication name cannot exceed {PrescriptionPolicies.MaxMedicationNameLength} characters", nameof(medicationName));
        }

        // Validate dosage
        if (string.IsNullOrWhiteSpace(dosage))
        {
            throw new ArgumentException("Dosage is required", nameof(dosage));
        }

        if (dosage.Length > PrescriptionPolicies.MaxDosageLength)
        {
            throw new ArgumentException($"Dosage cannot exceed {PrescriptionPolicies.MaxDosageLength} characters", nameof(dosage));
        }

        // Validate directions
        if (string.IsNullOrWhiteSpace(directions))
        {
            throw new ArgumentException("Directions are required", nameof(directions));
        }

        if (directions.Length > PrescriptionPolicies.MaxDirectionsLength)
        {
            throw new ArgumentException($"Directions cannot exceed {PrescriptionPolicies.MaxDirectionsLength} characters", nameof(directions));
        }

        // Validate quantity
        if (quantity < PrescriptionPolicies.MinQuantity || quantity > PrescriptionPolicies.MaxQuantity)
        {
            throw new ArgumentException($"Quantity must be between {PrescriptionPolicies.MinQuantity} and {PrescriptionPolicies.MaxQuantity}", nameof(quantity));
        }

        // Validate number of refills
        if (numberOfRefills < PrescriptionPolicies.MinRefills || numberOfRefills > PrescriptionPolicies.MaxRefills)
        {
            throw new ArgumentException($"Number of refills must be between {PrescriptionPolicies.MinRefills} and {PrescriptionPolicies.MaxRefills}", nameof(numberOfRefills));
        }

        // Validate duration
        if (durationInDays < PrescriptionPolicies.MinDurationDays || durationInDays > PrescriptionPolicies.MaxDurationDays)
        {
            throw new ArgumentException($"Duration must be between {PrescriptionPolicies.MinDurationDays} and {PrescriptionPolicies.MaxDurationDays} days", nameof(durationInDays));
        }

        // Validate and set timestamp
        var timestamp = issuedAtUtc ?? DateTime.UtcNow;
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must be in UTC", nameof(issuedAtUtc));
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
        IssuedDateUtc = timestamp;
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

    /// <summary>
    /// Gets a value indicating whether the prescription is expired at the current time.
    /// For testing, use <see cref="IsExpiredAt"/> instead.
    /// </summary>
    public bool IsExpired => IsExpiredAt(DateTime.UtcNow);

    /// <summary>
    /// Gets a value indicating whether the prescription is expired at a specific point in time.
    /// Useful for testing and querying.
    /// </summary>
    /// <param name="checkTimeUtc">The UTC time to check expiration against.</param>
    /// <returns>True if expired at the given time; otherwise false.</returns>
    public bool IsExpiredAt(DateTime checkTimeUtc) => ExpirationDateUtc < checkTimeUtc;

    /// <summary>
    /// Gets a value indicating whether the prescription has no remaining refills.
    /// </summary>
    public bool IsDepleted => RemainingRefills <= 0;

    /// <summary>
    /// Uses one refill from the prescription.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when prescription is expired, depleted, or already in terminal state.</exception>
    public void UseRefill()
    {
        if (Status == PrescriptionStatus.Expired)
        {
            throw new InvalidOperationException("Cannot use refill on an expired prescription");
        }

        if (Status == PrescriptionStatus.Depleted)
        {
            throw new InvalidOperationException("Cannot use refill on a depleted prescription");
        }

        if (RemainingRefills <= 0)
        {
            throw new InvalidOperationException("No refills remaining");
        }

        RemainingRefills--;

        if (RemainingRefills <= 0)
        {
            Status = PrescriptionStatus.Depleted;
        }
    }

    /// <summary>
    /// Marks the prescription as expired. Called when expiration date has passed.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when prescription is already expired or depleted.</exception>
    public void MarkExpired()
    {
        if (Status == PrescriptionStatus.Expired)
        {
            return; // Idempotent
        }

        if (Status == PrescriptionStatus.Depleted)
        {
            throw new InvalidOperationException("Cannot expire a depleted prescription");
        }

        Status = PrescriptionStatus.Expired;
    }

    /// <summary>
    /// Checks current state and updates status if needed. Call this before operations that depend on current status.
    /// </summary>
    /// <param name="currentTimeUtc">Optional current time for testability. Defaults to DateTime.UtcNow.</param>
    public void RefreshStatus(DateTime? currentTimeUtc = null)
    {
        if (Status != PrescriptionStatus.Active)
        {
            return; // Already in terminal state
        }

        var now = currentTimeUtc ?? DateTime.UtcNow;

        if (IsExpiredAt(now))
        {
            Status = PrescriptionStatus.Expired;
        }
        else if (IsDepleted)
        {
            Status = PrescriptionStatus.Depleted;
        }
    }
}

public enum PrescriptionStatus
{
    Active = 1,
    Expired = 2,
    Depleted = 3,
}