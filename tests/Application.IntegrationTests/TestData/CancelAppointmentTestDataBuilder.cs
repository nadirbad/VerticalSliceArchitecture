using VerticalSliceArchitecture.Application.Scheduling;

namespace VerticalSliceArchitecture.Application.IntegrationTests.TestData;

/// <summary>
/// Test data builder for CancelAppointmentCommand with fluent API.
/// </summary>
public class CancelAppointmentTestDataBuilder
{
    private Guid _appointmentId = Guid.NewGuid();
    private string _reason = "Patient requested cancellation";

    public CancelAppointmentTestDataBuilder WithAppointmentId(Guid appointmentId)
    {
        _appointmentId = appointmentId;
        return this;
    }

    public CancelAppointmentTestDataBuilder WithReason(string reason)
    {
        _reason = reason;
        return this;
    }

    public CancelAppointmentTestDataBuilder WithEmptyReason()
    {
        _reason = string.Empty;
        return this;
    }

    public CancelAppointmentTestDataBuilder WithTooLongReason()
    {
        _reason = new string('x', 513);
        return this;
    }

    public CancelAppointmentTestDataBuilder WithNonExistentAppointment()
    {
        _appointmentId = TestSeedData.NonExistentId;
        return this;
    }

    public CancelAppointmentCommand Build()
    {
        return new CancelAppointmentCommand(_appointmentId, _reason);
    }

    public (Guid AppointmentId, string Reason) BuildValues()
    {
        return (_appointmentId, _reason);
    }
}