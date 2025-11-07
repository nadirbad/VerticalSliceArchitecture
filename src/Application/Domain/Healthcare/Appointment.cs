using System.ComponentModel.DataAnnotations.Schema;

using VerticalSliceArchitecture.Application.Common;

namespace VerticalSliceArchitecture.Application.Domain.Healthcare;

public class Appointment : AuditableEntity, IHasDomainEvent
{
    public static Appointment Schedule(Guid patientId, Guid doctorId, DateTime startUtc, DateTime endUtc, string? notes = null)
    {
        return new Appointment(patientId, doctorId, startUtc, endUtc, notes);
    }

    private Appointment()
    {
        // Private parameterless constructor for EF Core
    }

    private Appointment(Guid patientId, Guid doctorId, DateTime startUtc, DateTime endUtc, string? notes)
    {
        ValidateDateTime(startUtc, nameof(startUtc));
        ValidateDateTime(endUtc, nameof(endUtc));

        if (startUtc >= endUtc)
        {
            throw new ArgumentException("Start time must be before end time", nameof(startUtc));
        }

        PatientId = patientId;
        DoctorId = doctorId;
        StartUtc = startUtc;
        EndUtc = endUtc;
        Status = AppointmentStatus.Scheduled;
        UpdateNotes(notes);
    }

    public Guid Id { get; internal set; }
    public Guid PatientId { get; private set; }
    public Guid DoctorId { get; private set; }
    public DateTime StartUtc { get; private set; }
    public DateTime EndUtc { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? CompletedUtc { get; private set; }
    public DateTime? CancelledUtc { get; private set; }
    public string? CancellationReason { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public Patient Patient { get; private set; } = null!;
    public Doctor Doctor { get; private set; } = null!;

    [NotMapped]
    public List<DomainEvent> DomainEvents { get; } = new List<DomainEvent>();

    /// <summary>
    /// Reschedules the appointment to new times. Assumes caller has validated business rules
    /// (status checks, time windows, conflicts). This method only mutates state.
    /// </summary>
    /// <param name="newStartUtc">The new start time in UTC.</param>
    /// <param name="newEndUtc">The new end time in UTC.</param>
    /// <param name="reason">Optional reason for rescheduling, appended to notes.</param>
    public void Reschedule(DateTime newStartUtc, DateTime newEndUtc, string? reason = null)
    {
        // Technical invariants only - business rules validated by caller
        ValidateDateTime(newStartUtc, nameof(newStartUtc));
        ValidateDateTime(newEndUtc, nameof(newEndUtc));

        // Mutate state
        StartUtc = newStartUtc;
        EndUtc = newEndUtc;
        Status = AppointmentStatus.Rescheduled;

        if (!string.IsNullOrWhiteSpace(reason))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? reason : $"{Notes}; {reason}";
        }
    }

    public void Complete(string? notes = null)
    {
        // Validation
        if (Status == AppointmentStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot complete a cancelled appointment");
        }

        if (Status == AppointmentStatus.Completed)
        {
            return; // Idempotent - already completed
        }

        if (!string.IsNullOrEmpty(notes) && notes.Length > 1024)
        {
            throw new ArgumentException("Notes cannot exceed 1024 characters", nameof(notes));
        }

        // State transition
        Status = AppointmentStatus.Completed;
        CompletedUtc = DateTime.UtcNow;
        Notes = notes;
    }

    public void Cancel(string reason)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Cancellation reason is required", nameof(reason));
        }

        if (reason.Length > 512)
        {
            throw new ArgumentException("Cancellation reason cannot exceed 512 characters", nameof(reason));
        }

        if (Status == AppointmentStatus.Completed)
        {
            throw new InvalidOperationException("Cannot cancel a completed appointment");
        }

        if (Status == AppointmentStatus.Cancelled)
        {
            return; // Idempotent - already cancelled
        }

        // State transition
        Status = AppointmentStatus.Cancelled;
        CancelledUtc = DateTime.UtcNow;
        CancellationReason = reason;
    }

    public void UpdateNotes(string? newNotes)
    {
        if (newNotes != null && newNotes.Length > 1024)
        {
            throw new ArgumentException("Notes cannot exceed 1024 characters", nameof(newNotes));
        }

        Notes = string.IsNullOrWhiteSpace(newNotes) ? null : newNotes.Trim();
    }

    private static void ValidateDateTime(DateTime dateTime, string parameterName)
    {
        if (dateTime.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("DateTime must be in UTC", parameterName);
        }
    }
}

public enum AppointmentStatus
{
    Scheduled = 1,
    Rescheduled = 2,
    Completed = 3,
    Cancelled = 4,
}