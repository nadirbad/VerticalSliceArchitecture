using VerticalSliceArchitecture.Application.Common;

namespace VerticalSliceArchitecture.Application.Domain.Events;

public class AppointmentBookedEvent(Guid appointmentId, Guid patientId, Guid doctorId, DateTime startUtc, DateTime endUtc) : DomainEvent
{
    public Guid AppointmentId { get; } = appointmentId;
    public Guid PatientId { get; } = patientId;
    public Guid DoctorId { get; } = doctorId;
    public DateTime StartUtc { get; } = startUtc;
    public DateTime EndUtc { get; } = endUtc;
}