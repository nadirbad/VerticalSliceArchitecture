using System.ComponentModel.DataAnnotations.Schema;

using VerticalSliceArchitecture.Application.Common;
using VerticalSliceArchitecture.Application.Domain.Events;

namespace VerticalSliceArchitecture.Application.Domain;

public class Appointment : AuditableEntity, IHasDomainEvent
{
    public static Appointment Schedule(Guid patientId, Guid doctorId, DateTime startUtc, DateTime endUtc, string? notes = null)
    {
        var appointment = new Appointment(patientId, doctorId, startUtc, endUtc, notes);

        // Raise domain event
        appointment.DomainEvents.Add(new AppointmentBookedEvent(
            appointment.Id,
            appointment.PatientId,
            appointment.DoctorId,
            appointment.StartUtc,
            appointment.EndUtc));

        return appointment;
    }

    private Appointment()
    {
        // Private parameterless constructor for EF Core
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Appointment"/> class, enforcing domain invariants.
    /// Note: FluentValidation also validates these rules for fast-fail UX.
    /// Domain validation is authoritative - handlers catch ArgumentException and convert to ErrorOr.
    /// </summary>
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
    /// Marks the appointment as completed.
    /// </summary>
    /// <param name="notes">Optional completion notes.</param>
    /// <param name="completedAtUtc">Optional timestamp for when completion occurred. Defaults to DateTime.UtcNow if not provided.</param>
    public void Complete(string? notes = null, DateTime? completedAtUtc = null)
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

        if (!string.IsNullOrEmpty(notes) && notes.Length > SchedulingPolicies.MaxNotesLength)
        {
            throw new ArgumentException($"Notes cannot exceed {SchedulingPolicies.MaxNotesLength} characters", nameof(notes));
        }

        var timestamp = completedAtUtc ?? DateTime.UtcNow;
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must be in UTC", nameof(completedAtUtc));
        }

        // State transition
        Status = AppointmentStatus.Completed;
        CompletedUtc = timestamp;
        Notes = notes;

        // Raise domain event
        DomainEvents.Add(new AppointmentCompletedEvent(
            Id,
            PatientId,
            DoctorId,
            CompletedUtc.Value,
            Notes));
    }

    /// <summary>
    /// Cancels the appointment.
    /// </summary>
    /// <param name="reason">Required reason for cancellation.</param>
    /// <param name="cancelledAtUtc">Optional timestamp for when cancellation occurred. Defaults to DateTime.UtcNow if not provided.</param>
    public void Cancel(string reason, DateTime? cancelledAtUtc = null)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Cancellation reason is required", nameof(reason));
        }

        if (reason.Length > SchedulingPolicies.MaxCancellationReasonLength)
        {
            throw new ArgumentException($"Cancellation reason cannot exceed {SchedulingPolicies.MaxCancellationReasonLength} characters", nameof(reason));
        }

        if (Status == AppointmentStatus.Completed)
        {
            throw new InvalidOperationException("Cannot cancel a completed appointment");
        }

        if (Status == AppointmentStatus.Cancelled)
        {
            return; // Idempotent - already cancelled
        }

        var timestamp = cancelledAtUtc ?? DateTime.UtcNow;
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must be in UTC", nameof(cancelledAtUtc));
        }

        // State transition
        Status = AppointmentStatus.Cancelled;
        CancelledUtc = timestamp;
        CancellationReason = reason;
    }

    public void UpdateNotes(string? newNotes)
    {
        if (newNotes != null && newNotes.Length > SchedulingPolicies.MaxNotesLength)
        {
            throw new ArgumentException($"Notes cannot exceed {SchedulingPolicies.MaxNotesLength} characters", nameof(newNotes));
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
    Completed = 2,
    Cancelled = 3,
}