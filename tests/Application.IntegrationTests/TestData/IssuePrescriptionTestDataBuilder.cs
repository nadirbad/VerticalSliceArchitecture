namespace VerticalSliceArchitecture.Application.IntegrationTests.TestData;

/// <summary>
/// Test data builder for creating IssuePrescription command payloads with fluent API.
/// Provides sensible defaults so tests only need to specify what they care about.
/// Immutable - each With method returns a new instance.
/// </summary>
public class IssuePrescriptionTestDataBuilder
{
    private readonly Guid _patientId = TestSeedData.DefaultPatientId;
    private readonly Guid _doctorId = TestSeedData.DefaultDoctorId;
    private readonly string _medicationName = "Amoxicillin";
    private readonly string _dosage = "500mg";
    private readonly string _directions = "Take one capsule three times daily with food";
    private readonly int _quantity = 30;
    private readonly int _numberOfRefills = 2;
    private readonly int _durationInDays = 10;

    /// <summary>
    /// Initializes a new instance of the <see cref="IssuePrescriptionTestDataBuilder"/> class.
    /// Creates a new builder with default values.
    /// </summary>
    public IssuePrescriptionTestDataBuilder()
    {
    }

    private IssuePrescriptionTestDataBuilder(
        Guid patientId,
        Guid doctorId,
        string medicationName,
        string dosage,
        string directions,
        int quantity,
        int numberOfRefills,
        int durationInDays)
    {
        _patientId = patientId;
        _doctorId = doctorId;
        _medicationName = medicationName;
        _dosage = dosage;
        _directions = directions;
        _quantity = quantity;
        _numberOfRefills = numberOfRefills;
        _durationInDays = durationInDays;
    }

    /// <summary>
    /// Sets the patient ID.
    /// </summary>
    /// <param name="patientId">The patient ID to set.</param>
    /// <returns>A new builder instance with the updated patient ID.</returns>
    public IssuePrescriptionTestDataBuilder WithPatientId(Guid patientId)
    {
        return new IssuePrescriptionTestDataBuilder(patientId, _doctorId, _medicationName, _dosage, _directions, _quantity, _numberOfRefills, _durationInDays);
    }

    /// <summary>
    /// Sets the doctor ID.
    /// </summary>
    /// <param name="doctorId">The doctor ID to set.</param>
    /// <returns>A new builder instance with the updated doctor ID.</returns>
    public IssuePrescriptionTestDataBuilder WithDoctorId(Guid doctorId)
    {
        return new IssuePrescriptionTestDataBuilder(_patientId, doctorId, _medicationName, _dosage, _directions, _quantity, _numberOfRefills, _durationInDays);
    }

    /// <summary>
    /// Sets the medication name.
    /// </summary>
    /// <param name="medicationName">The medication name to set.</param>
    /// <returns>A new builder instance with the updated medication name.</returns>
    public IssuePrescriptionTestDataBuilder WithMedicationName(string medicationName)
    {
        return new IssuePrescriptionTestDataBuilder(_patientId, _doctorId, medicationName, _dosage, _directions, _quantity, _numberOfRefills, _durationInDays);
    }

    /// <summary>
    /// Sets the dosage.
    /// </summary>
    /// <param name="dosage">The dosage to set.</param>
    /// <returns>A new builder instance with the updated dosage.</returns>
    public IssuePrescriptionTestDataBuilder WithDosage(string dosage)
    {
        return new IssuePrescriptionTestDataBuilder(_patientId, _doctorId, _medicationName, dosage, _directions, _quantity, _numberOfRefills, _durationInDays);
    }

    /// <summary>
    /// Sets the directions.
    /// </summary>
    /// <param name="directions">The directions to set.</param>
    /// <returns>A new builder instance with the updated directions.</returns>
    public IssuePrescriptionTestDataBuilder WithDirections(string directions)
    {
        return new IssuePrescriptionTestDataBuilder(_patientId, _doctorId, _medicationName, _dosage, directions, _quantity, _numberOfRefills, _durationInDays);
    }

    /// <summary>
    /// Sets the quantity.
    /// </summary>
    /// <param name="quantity">The quantity to set.</param>
    /// <returns>A new builder instance with the updated quantity.</returns>
    public IssuePrescriptionTestDataBuilder WithQuantity(int quantity)
    {
        return new IssuePrescriptionTestDataBuilder(_patientId, _doctorId, _medicationName, _dosage, _directions, quantity, _numberOfRefills, _durationInDays);
    }

    /// <summary>
    /// Sets the number of refills.
    /// </summary>
    /// <param name="numberOfRefills">The number of refills to set.</param>
    /// <returns>A new builder instance with the updated number of refills.</returns>
    public IssuePrescriptionTestDataBuilder WithNumberOfRefills(int numberOfRefills)
    {
        return new IssuePrescriptionTestDataBuilder(_patientId, _doctorId, _medicationName, _dosage, _directions, _quantity, numberOfRefills, _durationInDays);
    }

    /// <summary>
    /// Sets the duration in days.
    /// </summary>
    /// <param name="durationInDays">The duration in days to set.</param>
    /// <returns>A new builder instance with the updated duration.</returns>
    public IssuePrescriptionTestDataBuilder WithDurationInDays(int durationInDays)
    {
        return new IssuePrescriptionTestDataBuilder(_patientId, _doctorId, _medicationName, _dosage, _directions, _quantity, _numberOfRefills, durationInDays);
    }

    /// <summary>
    /// Sets quantity to be invalid (0).
    /// </summary>
    public IssuePrescriptionTestDataBuilder WithInvalidQuantityTooLow()
    {
        return new IssuePrescriptionTestDataBuilder(_patientId, _doctorId, _medicationName, _dosage, _directions, 0, _numberOfRefills, _durationInDays);
    }

    /// <summary>
    /// Sets quantity to exceed maximum (> 999).
    /// </summary>
    public IssuePrescriptionTestDataBuilder WithInvalidQuantityTooHigh()
    {
        return new IssuePrescriptionTestDataBuilder(_patientId, _doctorId, _medicationName, _dosage, _directions, 1000, _numberOfRefills, _durationInDays);
    }

    /// <summary>
    /// Sets number of refills to be invalid (negative).
    /// </summary>
    public IssuePrescriptionTestDataBuilder WithInvalidRefillsNegative()
    {
        return new IssuePrescriptionTestDataBuilder(_patientId, _doctorId, _medicationName, _dosage, _directions, _quantity, -1, _durationInDays);
    }

    /// <summary>
    /// Sets number of refills to exceed maximum (> 12).
    /// </summary>
    public IssuePrescriptionTestDataBuilder WithInvalidRefillsTooHigh()
    {
        return new IssuePrescriptionTestDataBuilder(_patientId, _doctorId, _medicationName, _dosage, _directions, _quantity, 13, _durationInDays);
    }

    /// <summary>
    /// Sets duration to be invalid (0 days).
    /// </summary>
    public IssuePrescriptionTestDataBuilder WithInvalidDurationTooLow()
    {
        return new IssuePrescriptionTestDataBuilder(_patientId, _doctorId, _medicationName, _dosage, _directions, _quantity, _numberOfRefills, 0);
    }

    /// <summary>
    /// Sets duration to exceed maximum (> 365 days).
    /// </summary>
    public IssuePrescriptionTestDataBuilder WithInvalidDurationTooHigh()
    {
        return new IssuePrescriptionTestDataBuilder(_patientId, _doctorId, _medicationName, _dosage, _directions, _quantity, _numberOfRefills, 366);
    }

    /// <summary>
    /// Sets an empty medication name (invalid).
    /// </summary>
    public IssuePrescriptionTestDataBuilder WithEmptyMedicationName()
    {
        return new IssuePrescriptionTestDataBuilder(_patientId, _doctorId, string.Empty, _dosage, _directions, _quantity, _numberOfRefills, _durationInDays);
    }

    /// <summary>
    /// Sets medication name that exceeds maximum length (> 200 characters).
    /// </summary>
    public IssuePrescriptionTestDataBuilder WithTooLongMedicationName()
    {
        var longName = new string('A', 201);
        return new IssuePrescriptionTestDataBuilder(_patientId, _doctorId, longName, _dosage, _directions, _quantity, _numberOfRefills, _durationInDays);
    }

    /// <summary>
    /// Sets an empty dosage (invalid).
    /// </summary>
    public IssuePrescriptionTestDataBuilder WithEmptyDosage()
    {
        return new IssuePrescriptionTestDataBuilder(_patientId, _doctorId, _medicationName, string.Empty, _directions, _quantity, _numberOfRefills, _durationInDays);
    }

    /// <summary>
    /// Sets dosage that exceeds maximum length (> 50 characters).
    /// </summary>
    public IssuePrescriptionTestDataBuilder WithTooLongDosage()
    {
        var longDosage = new string('A', 51);
        return new IssuePrescriptionTestDataBuilder(_patientId, _doctorId, _medicationName, longDosage, _directions, _quantity, _numberOfRefills, _durationInDays);
    }

    /// <summary>
    /// Sets empty directions (invalid).
    /// </summary>
    public IssuePrescriptionTestDataBuilder WithEmptyDirections()
    {
        return new IssuePrescriptionTestDataBuilder(_patientId, _doctorId, _medicationName, _dosage, string.Empty, _quantity, _numberOfRefills, _durationInDays);
    }

    /// <summary>
    /// Sets directions that exceed maximum length (> 500 characters).
    /// </summary>
    public IssuePrescriptionTestDataBuilder WithTooLongDirections()
    {
        var longDirections = new string('A', 501);
        return new IssuePrescriptionTestDataBuilder(_patientId, _doctorId, _medicationName, _dosage, longDirections, _quantity, _numberOfRefills, _durationInDays);
    }

    /// <summary>
    /// Sets a non-existent patient ID.
    /// </summary>
    public IssuePrescriptionTestDataBuilder WithNonExistentPatient()
    {
        return new IssuePrescriptionTestDataBuilder(TestSeedData.NonExistentId, _doctorId, _medicationName, _dosage, _directions, _quantity, _numberOfRefills, _durationInDays);
    }

    /// <summary>
    /// Sets a non-existent doctor ID.
    /// </summary>
    public IssuePrescriptionTestDataBuilder WithNonExistentDoctor()
    {
        return new IssuePrescriptionTestDataBuilder(_patientId, TestSeedData.NonExistentId, _medicationName, _dosage, _directions, _quantity, _numberOfRefills, _durationInDays);
    }

    /// <summary>
    /// Creates a long-term medication prescription.
    /// </summary>
    public IssuePrescriptionTestDataBuilder AsLongTermMedication()
    {
        return new IssuePrescriptionTestDataBuilder(
            _patientId,
            _doctorId,
            "Lisinopril",
            "10mg",
            "Take one tablet once daily in the morning",
            90,
            12,
            365);
    }

    /// <summary>
    /// Creates a controlled substance prescription with no refills.
    /// </summary>
    public IssuePrescriptionTestDataBuilder AsControlledSubstance()
    {
        return new IssuePrescriptionTestDataBuilder(
            _patientId,
            _doctorId,
            "Hydrocodone/Acetaminophen",
            "5mg/325mg",
            "Take one tablet every 4-6 hours as needed for pain. Do not exceed 6 tablets in 24 hours.",
            20,
            0,
            7);
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
            medicationName = _medicationName,
            dosage = _dosage,
            directions = _directions,
            quantity = _quantity,
            numberOfRefills = _numberOfRefills,
            durationInDays = _durationInDays,
        };
    }

    /// <summary>
    /// Builds and returns individual properties for custom construction.
    /// </summary>
    public (Guid PatientId, Guid DoctorId, string MedicationName, string Dosage, string Directions, int Quantity, int NumberOfRefills, int DurationInDays) BuildValues()
    {
        return (_patientId, _doctorId, _medicationName, _dosage, _directions, _quantity, _numberOfRefills, _durationInDays);
    }
}