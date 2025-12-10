namespace VerticalSliceArchitecture.Application.IntegrationTests.TestData;

/// <summary>
/// Provides deterministic test data with well-known GUIDs for integration tests.
/// These GUIDs match the seed data in HTTP request files for consistency.
/// </summary>
public static class TestSeedData
{
    /// <summary>
    /// Default patient ID for happy path tests.
    /// Corresponds to "Alice Johnson" in the HTTP request files.
    /// </summary>
    public static readonly Guid DefaultPatientId = new("11111111-1111-1111-1111-111111111111");

    /// <summary>
    /// Alternative patient ID for tests requiring multiple patients.
    /// Corresponds to "Bob Smith" in the HTTP request files.
    /// </summary>
    public static readonly Guid SecondPatientId = new("22222222-2222-2222-2222-222222222222");

    /// <summary>
    /// Third patient ID for tests requiring multiple patients.
    /// Corresponds to "Charlie Davis" in the HTTP request files.
    /// </summary>
    public static readonly Guid ThirdPatientId = new("33333333-3333-3333-3333-333333333333");

    /// <summary>
    /// Patient names for reference in tests.
    /// </summary>
    public static class PatientNames
    {
        public const string Default = "Alice Johnson";
        public const string Second = "Bob Smith";
        public const string Third = "Charlie Davis";
    }

    /// <summary>
    /// Default doctor ID for happy path tests.
    /// Corresponds to "Dr. Emily White" in the HTTP request files.
    /// </summary>
    public static readonly Guid DefaultDoctorId = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    /// <summary>
    /// Alternative doctor ID for tests requiring multiple doctors.
    /// Corresponds to "Dr. Michael Green" in the HTTP request files.
    /// </summary>
    public static readonly Guid SecondDoctorId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    /// <summary>
    /// Third doctor ID for tests requiring multiple doctors.
    /// Corresponds to "Dr. Sarah Brown" in the HTTP request files.
    /// </summary>
    public static readonly Guid ThirdDoctorId = new("cccccccc-cccc-cccc-cccc-cccccccccccc");

    /// <summary>
    /// Doctor names for reference in tests.
    /// </summary>
    public static class DoctorNames
    {
        public const string Default = "Dr. Emily White";
        public const string Second = "Dr. Michael Green";
        public const string Third = "Dr. Sarah Brown";
    }

    /// <summary>
    /// A GUID that does not correspond to any entity in the test database.
    /// Used for testing 404 Not Found scenarios.
    /// </summary>
    public static readonly Guid NonExistentId = new("99999999-9999-9999-9999-999999999999");

    /// <summary>
    /// Gets a valid future appointment start time (7 days from now at 10:00 AM UTC).
    /// </summary>
    public static DateTimeOffset GetValidAppointmentStartTime()
    {
        return DateTimeOffset.UtcNow.AddDays(7).Date.AddHours(10);
    }

    /// <summary>
    /// Gets a valid future appointment end time (7 days from now at 10:30 AM UTC).
    /// </summary>
    public static DateTimeOffset GetValidAppointmentEndTime()
    {
        return GetValidAppointmentStartTime().AddMinutes(30);
    }

    /// <summary>
    /// Gets a valid future reschedule start time (10 days from now at 2:00 PM UTC).
    /// This is sufficiently far in the future to avoid the 24-hour reschedule window restriction.
    /// </summary>
    public static DateTimeOffset GetValidRescheduleStartTime()
    {
        return DateTimeOffset.UtcNow.AddDays(10).Date.AddHours(14);
    }

    /// <summary>
    /// Gets a valid future reschedule end time (10 days from now at 2:30 PM UTC).
    /// </summary>
    public static DateTimeOffset GetValidRescheduleEndTime()
    {
        return GetValidRescheduleStartTime().AddMinutes(30);
    }

    /// <summary>
    /// Returns a summary of all available test patients for documentation purposes.
    /// </summary>
    public static IEnumerable<(Guid Id, string Name)> GetAllTestPatients()
    {
        yield return (DefaultPatientId, PatientNames.Default);
        yield return (SecondPatientId, PatientNames.Second);
        yield return (ThirdPatientId, PatientNames.Third);
    }

    /// <summary>
    /// Returns a summary of all available test doctors for documentation purposes.
    /// </summary>
    public static IEnumerable<(Guid Id, string Name)> GetAllTestDoctors()
    {
        yield return (DefaultDoctorId, DoctorNames.Default);
        yield return (SecondDoctorId, DoctorNames.Second);
        yield return (ThirdDoctorId, DoctorNames.Third);
    }
}