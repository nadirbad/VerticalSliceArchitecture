using VerticalSliceArchitecture.Application.Common;

namespace VerticalSliceArchitecture.Application.Domain.Healthcare;

public class AppointmentRescheduledEvent(
    Guid appointmentId,
    DateTime previousStartUtc,
    DateTime previousEndUtc,
    DateTime newStartUtc,
    DateTime newEndUtc) : DomainEvent
{
    public Guid AppointmentId { get; } = appointmentId;
    public DateTime PreviousStartUtc { get; } = previousStartUtc;
    public DateTime PreviousEndUtc { get; } = previousEndUtc;
    public DateTime NewStartUtc { get; } = newStartUtc;
    public DateTime NewEndUtc { get; } = newEndUtc;
}