using FluentValidation.TestHelper;

using VerticalSliceArchitecture.Application.Scheduling;

namespace VerticalSliceArchitecture.Application.UnitTests.Scheduling;

public class BookAppointmentValidatorTests
{
    private readonly BookAppointment.Validator _validator = new();

    [Fact]
    public void Should_Have_Error_When_PatientId_Is_Empty()
    {
        // Arrange
        var command = CreateCommand(patientId: Guid.Empty);

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.PatientId);
    }

    [Fact]
    public void Should_Have_Error_When_DoctorId_Is_Empty()
    {
        // Arrange
        var command = CreateCommand(doctorId: Guid.Empty);

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.DoctorId);
    }

    [Fact]
    public void Should_Have_Error_When_Start_Is_After_End()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow.AddHours(2);
        var end = DateTimeOffset.UtcNow.AddHours(1);
        var command = CreateCommand(start: start, end: end);

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Start);
    }

    [Fact]
    public void Should_Have_Error_When_Appointment_Is_Too_Short()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow.AddHours(1);
        var command = CreateCommand(start: start, end: start.AddMinutes(5));

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.End);
    }

    [Fact]
    public void Should_Have_Error_When_Appointment_Is_Too_Long()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow.AddHours(1);
        var command = CreateCommand(start: start, end: start.AddHours(9));

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.End);
    }

    [Fact]
    public void Should_Have_Error_When_Not_Booked_In_Advance()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow.AddMinutes(5);
        var command = CreateCommand(start: start, end: start.AddMinutes(30));

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Start);
    }

    [Fact]
    public void Should_Have_Error_When_Notes_Too_Long()
    {
        // Arrange
        var command = CreateCommand(notes: new string('A', 1025));

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Valid()
    {
        // Arrange
        var command = CreateCommand();

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    private static BookAppointment.Command CreateCommand(
        Guid? patientId = null,
        Guid? doctorId = null,
        DateTimeOffset? start = null,
        DateTimeOffset? end = null,
        string? notes = "Test")
    {
        var defaultStart = DateTimeOffset.UtcNow.AddHours(1);
        return new BookAppointment.Command(
            patientId ?? Guid.NewGuid(),
            doctorId ?? Guid.NewGuid(),
            start ?? defaultStart,
            end ?? defaultStart.AddHours(1),
            notes);
    }
}