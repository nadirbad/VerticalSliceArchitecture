using VerticalSliceArchitecture.Application.Common;

namespace VerticalSliceArchitecture.Application.Domain.Healthcare;

public class AppointmentBookedEvent : DomainEvent
{
    public AppointmentBookedEvent(int appointmentId, int patientId, int doctorId, DateTime startUtc, DateTime endUtc)
    {
        AppointmentId = appointmentId;
        PatientId = patientId;
        DoctorId = doctorId;
        StartUtc = startUtc;
        EndUtc = endUtc;
    }

    public int AppointmentId { get; }
    public int PatientId { get; }
    public int DoctorId { get; }
    public DateTime StartUtc { get; }
    public DateTime EndUtc { get; }
}
