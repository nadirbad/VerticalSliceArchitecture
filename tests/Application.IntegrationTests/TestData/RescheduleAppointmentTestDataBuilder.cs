namespace VerticalSliceArchitecture.Application.IntegrationTests.TestData;

/// <summary>
/// Test data builder for creating RescheduleAppointment command payloads with fluent API.
/// Provides sensible defaults so tests only need to specify what they care about.
/// Immutable - each With method returns a new instance.
/// </summary>
public class RescheduleAppointmentTestDataBuilder
{
    private readonly Guid _appointmentId = Guid.NewGuid();
    private readonly DateTimeOffset _newStart = DateTimeOffset.UtcNow.AddDays(10).Date.AddHours(14); // 10 days from now at 2:00 PM
    private readonly DateTimeOffset _newEnd = DateTimeOffset.UtcNow.AddDays(10).Date.AddHours(14).AddMinutes(30); // 30 minute appointment
    private readonly string? _reason = "Rescheduling for test";

    /// <summary>
    /// Initializes a new instance of the <see cref="RescheduleAppointmentTestDataBuilder"/> class.
    /// Creates a new builder with default values.
    /// </summary>
    public RescheduleAppointmentTestDataBuilder()
    {
    }

    private RescheduleAppointmentTestDataBuilder(
        Guid appointmentId,
        DateTimeOffset newStart,
        DateTimeOffset newEnd,
        string? reason)
    {
        _appointmentId = appointmentId;
        _newStart = newStart;
        _newEnd = newEnd;
        _reason = reason;
    }

    /// <summary>
    /// Sets the appointment ID to reschedule.
    /// </summary>
    /// <param name="appointmentId">The appointment ID to set.</param>
    /// <returns>A new builder instance with the updated appointment ID.</returns>
    public RescheduleAppointmentTestDataBuilder WithAppointmentId(Guid appointmentId)
    {
        return new RescheduleAppointmentTestDataBuilder(appointmentId, _newStart, _newEnd, _reason);
    }

    /// <summary>
    /// Sets the new start time. End time will be adjusted to maintain duration if preserveDuration is true.
    /// </summary>
    /// <param name="newStart">The new start time to set.</param>
    /// <param name="preserveDuration">If true, maintains the appointment duration by adjusting the end time.</param>
    /// <returns>A new builder instance with the updated start time.</returns>
    public RescheduleAppointmentTestDataBuilder WithNewStartTime(DateTimeOffset newStart, bool preserveDuration = true)
    {
        var newEnd = preserveDuration ? newStart.Add(_newEnd - _newStart) : _newEnd;
        return new RescheduleAppointmentTestDataBuilder(_appointmentId, newStart, newEnd, _reason);
    }

    /// <summary>
    /// Sets the new end time.
    /// </summary>
    /// <param name="newEnd">The new end time to set.</param>
    /// <returns>A new builder instance with the updated end time.</returns>
    public RescheduleAppointmentTestDataBuilder WithNewEndTime(DateTimeOffset newEnd)
    {
        return new RescheduleAppointmentTestDataBuilder(_appointmentId, _newStart, newEnd, _reason);
    }

    /// <summary>
    /// Sets both new start and end times.
    /// </summary>
    /// <param name="newStart">The new start time to set.</param>
    /// <param name="newEnd">The new end time to set.</param>
    /// <returns>A new builder instance with the updated time range.</returns>
    public RescheduleAppointmentTestDataBuilder WithNewTimeRange(DateTimeOffset newStart, DateTimeOffset newEnd)
    {
        return new RescheduleAppointmentTestDataBuilder(_appointmentId, newStart, newEnd, _reason);
    }

    /// <summary>
    /// Sets the new appointment duration in minutes from the new start time.
    /// </summary>
    /// <param name="minutes">The duration in minutes.</param>
    /// <returns>A new builder instance with the updated duration.</returns>
    public RescheduleAppointmentTestDataBuilder WithNewDuration(int minutes)
    {
        return new RescheduleAppointmentTestDataBuilder(_appointmentId, _newStart, _newStart.AddMinutes(minutes), _reason);
    }

    /// <summary>
    /// Sets the reason for rescheduling.
    /// </summary>
    /// <param name="reason">The reason for rescheduling.</param>
    /// <returns>A new builder instance with the updated reason.</returns>
    public RescheduleAppointmentTestDataBuilder WithReason(string? reason)
    {
        return new RescheduleAppointmentTestDataBuilder(_appointmentId, _newStart, _newEnd, reason);
    }

    /// <summary>
    /// Sets new start time to be in the past (invalid).
    /// </summary>
    public RescheduleAppointmentTestDataBuilder InThePast()
    {
        var newStart = DateTimeOffset.UtcNow.AddDays(-1);
        var newEnd = newStart.AddMinutes(30);
        return new RescheduleAppointmentTestDataBuilder(_appointmentId, newStart, newEnd, _reason);
    }

    /// <summary>
    /// Sets new start time to be less than 2 hours from now (invalid for rescheduling).
    /// </summary>
    public RescheduleAppointmentTestDataBuilder TooSoon()
    {
        var newStart = DateTimeOffset.UtcNow.AddHours(1);
        var newEnd = newStart.AddMinutes(30);
        return new RescheduleAppointmentTestDataBuilder(_appointmentId, newStart, newEnd, _reason);
    }

    /// <summary>
    /// Sets invalid time range where newStart >= newEnd.
    /// </summary>
    public RescheduleAppointmentTestDataBuilder WithInvalidTimeRange()
    {
        var newStart = _newStart;
        var newEnd = newStart.AddMinutes(-10); // End before start
        return new RescheduleAppointmentTestDataBuilder(_appointmentId, newStart, newEnd, _reason);
    }

    /// <summary>
    /// Sets new duration to be too short (less than 10 minutes).
    /// </summary>
    public RescheduleAppointmentTestDataBuilder WithTooShortDuration()
    {
        return new RescheduleAppointmentTestDataBuilder(_appointmentId, _newStart, _newStart.AddMinutes(5), _reason);
    }

    /// <summary>
    /// Sets new duration to be too long (more than 8 hours).
    /// </summary>
    public RescheduleAppointmentTestDataBuilder WithTooLongDuration()
    {
        return new RescheduleAppointmentTestDataBuilder(_appointmentId, _newStart, _newStart.AddHours(9), _reason);
    }

    /// <summary>
    /// Sets reason that exceeds the maximum length (512 characters).
    /// </summary>
    public RescheduleAppointmentTestDataBuilder WithTooLongReason()
    {
        var longReason = new string('A', 513);
        return new RescheduleAppointmentTestDataBuilder(_appointmentId, _newStart, _newEnd, longReason);
    }

    /// <summary>
    /// Sets a non-existent appointment ID.
    /// </summary>
    public RescheduleAppointmentTestDataBuilder WithNonExistentAppointment()
    {
        return new RescheduleAppointmentTestDataBuilder(TestSeedData.NonExistentId, _newStart, _newEnd, _reason);
    }

    /// <summary>
    /// Builds the command object as an anonymous object suitable for JSON serialization.
    /// </summary>
    public object Build()
    {
        return new
        {
            appointmentId = _appointmentId,
            newStart = _newStart,
            newEnd = _newEnd,
            reason = _reason,
        };
    }

    /// <summary>
    /// Builds and returns individual properties for custom construction.
    /// </summary>
    public (Guid AppointmentId, DateTimeOffset NewStart, DateTimeOffset NewEnd, string? Reason) BuildValues()
    {
        return (_appointmentId, _newStart, _newEnd, _reason);
    }
}