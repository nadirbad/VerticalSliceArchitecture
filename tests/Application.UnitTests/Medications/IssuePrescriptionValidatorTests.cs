using FluentValidation.TestHelper;

using VerticalSliceArchitecture.Application.Medications;

namespace VerticalSliceArchitecture.Application.UnitTests.Medications;

public class IssuePrescriptionValidatorTests
{
    private readonly IssuePrescriptionCommandValidator _validator = new();

    [Fact]
    public void Should_have_error_when_PatientId_is_empty()
    {
        var command = new IssuePrescriptionCommand(
            Guid.Empty,
            Guid.NewGuid(),
            "Medication",
            "10mg",
            "Take once daily",
            30,
            2,
            90);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.PatientId)
            .WithErrorMessage("Patient ID is required");
    }

    [Fact]
    public void Should_have_error_when_DoctorId_is_empty()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(),
            Guid.Empty,
            "Medication",
            "10mg",
            "Take once daily",
            30,
            2,
            90);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.DoctorId)
            .WithErrorMessage("Doctor ID is required");
    }

    [Fact]
    public void Should_have_error_when_MedicationName_is_empty()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            string.Empty,
            "10mg",
            "Take once daily",
            30,
            2,
            90);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.MedicationName)
            .WithErrorMessage("Medication name is required");
    }

    [Fact]
    public void Should_have_error_when_MedicationName_exceeds_200_characters()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new string('A', 201),
            "10mg",
            "Take once daily",
            30,
            2,
            90);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.MedicationName)
            .WithErrorMessage("Medication name cannot exceed 200 characters");
    }

    [Fact]
    public void Should_have_error_when_Dosage_is_empty()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Medication",
            string.Empty,
            "Take once daily",
            30,
            2,
            90);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Dosage)
            .WithErrorMessage("Dosage is required");
    }

    [Fact]
    public void Should_have_error_when_Dosage_exceeds_50_characters()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Medication",
            new string('A', 51),
            "Take once daily",
            30,
            2,
            90);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Dosage)
            .WithErrorMessage("Dosage cannot exceed 50 characters");
    }

    [Fact]
    public void Should_have_error_when_Directions_is_empty()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Medication",
            "10mg",
            string.Empty,
            30,
            2,
            90);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Directions)
            .WithErrorMessage("Directions are required");
    }

    [Fact]
    public void Should_have_error_when_Directions_exceeds_500_characters()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Medication",
            "10mg",
            new string('A', 501),
            30,
            2,
            90);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Directions)
            .WithErrorMessage("Directions cannot exceed 500 characters");
    }

    [Fact]
    public void Should_have_error_when_Quantity_is_less_than_1()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Medication",
            "10mg",
            "Take once daily",
            0,
            2,
            90);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Quantity)
            .WithErrorMessage("Quantity must be between 1 and 999");
    }

    [Fact]
    public void Should_have_error_when_Quantity_is_greater_than_999()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Medication",
            "10mg",
            "Take once daily",
            1000,
            2,
            90);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Quantity)
            .WithErrorMessage("Quantity must be between 1 and 999");
    }

    [Fact]
    public void Should_have_error_when_NumberOfRefills_is_less_than_0()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Medication",
            "10mg",
            "Take once daily",
            30,
            -1,
            90);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.NumberOfRefills)
            .WithErrorMessage("Number of refills must be between 0 and 12");
    }

    [Fact]
    public void Should_have_error_when_NumberOfRefills_is_greater_than_12()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Medication",
            "10mg",
            "Take once daily",
            30,
            13,
            90);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.NumberOfRefills)
            .WithErrorMessage("Number of refills must be between 0 and 12");
    }

    [Fact]
    public void Should_have_error_when_DurationInDays_is_less_than_1()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Medication",
            "10mg",
            "Take once daily",
            30,
            2,
            0);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.DurationInDays)
            .WithErrorMessage("Duration must be between 1 and 365 days");
    }

    [Fact]
    public void Should_have_error_when_DurationInDays_is_greater_than_365()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Medication",
            "10mg",
            "Take once daily",
            30,
            2,
            366);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.DurationInDays)
            .WithErrorMessage("Duration must be between 1 and 365 days");
    }

    [Fact]
    public void Should_not_have_error_when_command_is_valid()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Amoxicillin",
            "500mg",
            "Take one capsule three times daily with food",
            30,
            2,
            90);

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_not_have_error_when_MedicationName_is_exactly_200_characters()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new string('A', 200),
            "10mg",
            "Take once daily",
            30,
            2,
            90);

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.MedicationName);
    }

    [Fact]
    public void Should_not_have_error_when_Dosage_is_exactly_50_characters()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Medication",
            new string('A', 50),
            "Take once daily",
            30,
            2,
            90);

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Dosage);
    }

    [Fact]
    public void Should_not_have_error_when_Directions_is_exactly_500_characters()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Medication",
            "10mg",
            new string('A', 500),
            30,
            2,
            90);

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Directions);
    }

    [Fact]
    public void Should_not_have_error_when_Quantity_is_1()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Medication",
            "10mg",
            "Take once daily",
            1,
            2,
            90);

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Should_not_have_error_when_Quantity_is_999()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Medication",
            "10mg",
            "Take once daily",
            999,
            2,
            90);

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Should_not_have_error_when_NumberOfRefills_is_0()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Medication",
            "10mg",
            "Take once daily",
            30,
            0,
            90);

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.NumberOfRefills);
    }

    [Fact]
    public void Should_not_have_error_when_NumberOfRefills_is_12()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Medication",
            "10mg",
            "Take once daily",
            30,
            12,
            90);

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.NumberOfRefills);
    }

    [Fact]
    public void Should_not_have_error_when_DurationInDays_is_1()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Medication",
            "10mg",
            "Take once daily",
            30,
            2,
            1);

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.DurationInDays);
    }

    [Fact]
    public void Should_not_have_error_when_DurationInDays_is_365()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Medication",
            "10mg",
            "Take once daily",
            30,
            2,
            365);

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.DurationInDays);
    }
}