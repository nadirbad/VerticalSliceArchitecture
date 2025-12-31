using VerticalSliceArchitecture.Application.Scheduling;

namespace VerticalSliceArchitecture.Application.IntegrationTests.TestData;

/// <summary>
/// Test data builder for CompleteAppointmentCommand with fluent API.
/// </summary>
public class CompleteAppointmentTestDataBuilder
{
    private Guid _appointmentId = Guid.NewGuid();
    private string? _notes = "Patient arrived on time. Routine checkup completed.";

    public CompleteAppointmentTestDataBuilder WithAppointmentId(Guid appointmentId)
    {
        _appointmentId = appointmentId;
        return this;
    }

    public CompleteAppointmentTestDataBuilder WithNotes(string? notes)
    {
        _notes = notes;
        return this;
    }

    public CompleteAppointmentTestDataBuilder WithNullNotes()
    {
        _notes = null;
        return this;
    }

    public CompleteAppointmentTestDataBuilder WithTooLongNotes()
    {
        _notes = new string('x', 1025);
        return this;
    }

    public CompleteAppointmentTestDataBuilder WithNonExistentAppointment()
    {
        _appointmentId = TestSeedData.NonExistentId;
        return this;
    }

    public CompleteAppointmentCommand Build()
    {
        return new CompleteAppointmentCommand(_appointmentId, _notes);
    }

    public (Guid AppointmentId, string? Notes) BuildValues()
    {
        return (_appointmentId, _notes);
    }
}