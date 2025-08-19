using VerticalSliceArchitecture.Application.Common;

namespace VerticalSliceArchitecture.Application.Domain.Healthcare;

public class Appointment : AuditableEntity, IHasDomainEvent
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public AppointmentStatus Status { get; set; }
    public string? Notes { get; set; }
    public byte[]? RowVersion { get; set; }

    public Patient Patient { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;

    public List<DomainEvent> DomainEvents { get; } = new List<DomainEvent>();
}

public enum AppointmentStatus
{
    Scheduled = 1,
    Rescheduled = 2,
    Completed = 3,
    Cancelled = 4,
}
