namespace VerticalSliceArchitecture.Application.IntegrationTests.TestData;

/// <summary>
/// Test data builder for creating BookAppointment command payloads with fluent API.
/// Provides sensible defaults so tests only need to specify what they care about.
/// Immutable - each With method returns a new instance.
/// </summary>
public class BookAppointmentTestDataBuilder
{
    private readonly Guid _patientId = TestSeedData.DefaultPatientId;
    private readonly Guid _doctorId = TestSeedData.DefaultDoctorId;
    private readonly DateTimeOffset _start = DateTimeOffset.UtcNow.AddDays(7).Date.AddHours(10); // 7 days from now at 10:00 AM
    private readonly DateTimeOffset _end = DateTimeOffset.UtcNow.AddDays(7).Date.AddHours(10).AddMinutes(30); // 30 minute appointment
    private readonly string? _notes = "Test appointment";

    /// <summary>
    /// Initializes a new instance of the <see cref="BookAppointmentTestDataBuilder"/> class.
    /// Creates a new builder with default values.
    /// </summary>
    public BookAppointmentTestDataBuilder()
    {
    }

    private BookAppointmentTestDataBuilder(
        Guid patientId,
        Guid doctorId,
        DateTimeOffset start,
        DateTimeOffset end,
        string? notes)
    {
        _patientId = patientId;
        _doctorId = doctorId;
        _start = start;
        _end = end;
        _notes = notes;
    }

    /// <summary>
    /// Sets the patient ID.
    /// </summary>
    /// <param name="patientId">The patient ID to set.</param>
    /// <returns>A new builder instance with the updated patient ID.</returns>
    public BookAppointmentTestDataBuilder WithPatientId(Guid patientId)
    {
        return new BookAppointmentTestDataBuilder(patientId, _doctorId, _start, _end, _notes);
    }

    /// <summary>
    /// Sets the doctor ID.
    /// </summary>
    /// <param name="doctorId">The doctor ID to set.</param>
    /// <returns>A new builder instance with the updated doctor ID.</returns>
    public BookAppointmentTestDataBuilder WithDoctorId(Guid doctorId)
    {
        return new BookAppointmentTestDataBuilder(_patientId, doctorId, _start, _end, _notes);
    }

    /// <summary>
    /// Sets the start time. End time will be adjusted to maintain duration if preserveDuration is true.
    /// </summary>
    /// <param name="start">The start time to set.</param>
    /// <param name="preserveDuration">If true, maintains the appointment duration by adjusting the end time.</param>
    /// <returns>A new builder instance with the updated start time.</returns>
    public BookAppointmentTestDataBuilder WithStartTime(DateTimeOffset start, bool preserveDuration = true)
    {
        var end = preserveDuration ? start.Add(_end - _start) : _end;
        return new BookAppointmentTestDataBuilder(_patientId, _doctorId, start, end, _notes);
    }

    /// <summary>
    /// Sets the end time.
    /// </summary>
    /// <param name="end">The end time to set.</param>
    /// <returns>A new builder instance with the updated end time.</returns>
    public BookAppointmentTestDataBuilder WithEndTime(DateTimeOffset end)
    {
        return new BookAppointmentTestDataBuilder(_patientId, _doctorId, _start, end, _notes);
    }

    /// <summary>
    /// Sets both start and end times.
    /// </summary>
    /// <param name="start">The start time to set.</param>
    /// <param name="end">The end time to set.</param>
    /// <returns>A new builder instance with the updated time range.</returns>
    public BookAppointmentTestDataBuilder WithTimeRange(DateTimeOffset start, DateTimeOffset end)
    {
        return new BookAppointmentTestDataBuilder(_patientId, _doctorId, start, end, _notes);
    }

    /// <summary>
    /// Sets the appointment duration in minutes from the start time.
    /// </summary>
    /// <param name="minutes">The duration in minutes.</param>
    /// <returns>A new builder instance with the updated duration.</returns>
    public BookAppointmentTestDataBuilder WithDuration(int minutes)
    {
        return new BookAppointmentTestDataBuilder(_patientId, _doctorId, _start, _start.AddMinutes(minutes), _notes);
    }

    /// <summary>
    /// Sets the notes.
    /// </summary>
    /// <param name="notes">The notes to set.</param>
    /// <returns>A new builder instance with the updated notes.</returns>
    public BookAppointmentTestDataBuilder WithNotes(string? notes)
    {
        return new BookAppointmentTestDataBuilder(_patientId, _doctorId, _start, _end, notes);
    }

    /// <summary>
    /// Sets start time to be in the past (invalid for booking).
    /// </summary>
    public BookAppointmentTestDataBuilder InThePast()
    {
        var start = DateTimeOffset.UtcNow.AddDays(-1);
        var end = start.AddMinutes(30);
        return new BookAppointmentTestDataBuilder(_patientId, _doctorId, start, end, _notes);
    }

    /// <summary>
    /// Sets start time to be less than 15 minutes from now (invalid for booking).
    /// </summary>
    public BookAppointmentTestDataBuilder TooSoon()
    {
        var start = DateTimeOffset.UtcNow.AddMinutes(10);
        var end = start.AddMinutes(30);
        return new BookAppointmentTestDataBuilder(_patientId, _doctorId, start, end, _notes);
    }

    /// <summary>
    /// Sets invalid time range where start >= end.
    /// </summary>
    public BookAppointmentTestDataBuilder WithInvalidTimeRange()
    {
        var start = _start;
        var end = start.AddMinutes(-10); // End before start
        return new BookAppointmentTestDataBuilder(_patientId, _doctorId, start, end, _notes);
    }

    /// <summary>
    /// Sets duration to be too short (less than 10 minutes).
    /// </summary>
    public BookAppointmentTestDataBuilder WithTooShortDuration()
    {
        return new BookAppointmentTestDataBuilder(_patientId, _doctorId, _start, _start.AddMinutes(5), _notes);
    }

    /// <summary>
    /// Sets duration to be too long (more than 8 hours).
    /// </summary>
    public BookAppointmentTestDataBuilder WithTooLongDuration()
    {
        return new BookAppointmentTestDataBuilder(_patientId, _doctorId, _start, _start.AddHours(9), _notes);
    }

    /// <summary>
    /// Sets notes that exceed the maximum length (1024 characters).
    /// </summary>
    public BookAppointmentTestDataBuilder WithTooLongNotes()
    {
        var longNotes = new string('A', 1025);
        return new BookAppointmentTestDataBuilder(_patientId, _doctorId, _start, _end, longNotes);
    }

    /// <summary>
    /// Sets a non-existent patient ID.
    /// </summary>
    public BookAppointmentTestDataBuilder WithNonExistentPatient()
    {
        return new BookAppointmentTestDataBuilder(TestSeedData.NonExistentId, _doctorId, _start, _end, _notes);
    }

    /// <summary>
    /// Sets a non-existent doctor ID.
    /// </summary>
    public BookAppointmentTestDataBuilder WithNonExistentDoctor()
    {
        return new BookAppointmentTestDataBuilder(_patientId, TestSeedData.NonExistentId, _start, _end, _notes);
    }

    /// <summary>
    /// Builds the command object as an anonymous object suitable for JSON serialization.
    /// </summary>
    public object Build()
    {
        return new
        {
            patientId = _patientId,
            doctorId = _doctorId,
            start = _start,
            end = _end,
            notes = _notes,
        };
    }

    /// <summary>
    /// Builds and returns individual properties for custom construction.
    /// </summary>
    public (Guid PatientId, Guid DoctorId, DateTimeOffset Start, DateTimeOffset End, string? Notes) BuildValues()
    {
        return (_patientId, _doctorId, _start, _end, _notes);
    }
}