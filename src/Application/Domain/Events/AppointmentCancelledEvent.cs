using VerticalSliceArchitecture.Application.Common;

namespace VerticalSliceArchitecture.Application.Domain.Events;

public class AppointmentCancelledEvent(
    Guid appointmentId,
    Guid patientId,
    Guid doctorId,
    DateTime cancelledUtc,
    string cancellationReason) : DomainEvent
{
    public Guid AppointmentId { get; } = appointmentId;
    public Guid PatientId { get; } = patientId;
    public Guid DoctorId { get; } = doctorId;
    public DateTime CancelledUtc { get; } = cancelledUtc;
    public string CancellationReason { get; } = cancellationReason;
}