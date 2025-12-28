using VerticalSliceArchitecture.Application.Common;

namespace VerticalSliceArchitecture.Application.Domain;

public class AppointmentCompletedEvent(
    Guid appointmentId,
    Guid patientId,
    Guid doctorId,
    DateTime completedUtc,
    string? notes) : DomainEvent
{
    public Guid AppointmentId { get; } = appointmentId;
    public Guid PatientId { get; } = patientId;
    public Guid DoctorId { get; } = doctorId;
    public DateTime CompletedUtc { get; } = completedUtc;
    public string? Notes { get; } = notes;
}