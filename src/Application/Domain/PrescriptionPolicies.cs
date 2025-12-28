namespace VerticalSliceArchitecture.Application.Domain;

/// <summary>
/// Business rules and constraints for prescriptions.
/// Single source of truth used by validators, domain objects, and tests.
/// </summary>
public static class PrescriptionPolicies
{
    /// <summary>
    /// Maximum length of medication name.
    /// </summary>
    public const int MaxMedicationNameLength = 200;

    /// <summary>
    /// Maximum length of dosage instructions.
    /// </summary>
    public const int MaxDosageLength = 50;

    /// <summary>
    /// Maximum length of administration directions.
    /// </summary>
    public const int MaxDirectionsLength = 500;

    /// <summary>
    /// Minimum quantity that can be prescribed.
    /// </summary>
    public const int MinQuantity = 1;

    /// <summary>
    /// Maximum quantity that can be prescribed per prescription.
    /// </summary>
    public const int MaxQuantity = 999;

    /// <summary>
    /// Minimum number of refills (0 = no refills).
    /// </summary>
    public const int MinRefills = 0;

    /// <summary>
    /// Maximum number of refills allowed per prescription.
    /// </summary>
    public const int MaxRefills = 12;

    /// <summary>
    /// Minimum prescription duration (in days).
    /// </summary>
    public const int MinDurationDays = 1;

    /// <summary>
    /// Maximum prescription duration (in days).
    /// </summary>
    public const int MaxDurationDays = 365;
}