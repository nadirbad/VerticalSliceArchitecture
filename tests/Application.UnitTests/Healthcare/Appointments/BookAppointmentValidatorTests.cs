using FluentValidation.TestHelper;

using VerticalSliceArchitecture.Application.Features.Healthcare.Appointments;

namespace VerticalSliceArchitecture.Application.UnitTests.Healthcare.Appointments;

public class BookAppointmentValidatorTests
{
    private readonly BookAppointmentCommandValidator _validator;

    public BookAppointmentValidatorTests()
    {
        _validator = new BookAppointmentCommandValidator();
    }

    [Fact]
    public void Should_Have_Error_When_PatientId_Is_Empty()
    {
        // Arrange
        var command = new BookAppointmentCommand(
            Guid.Empty,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(1),
            DateTimeOffset.UtcNow.AddHours(2),
            "Test");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.PatientId)
            .WithErrorMessage("PatientId is required");
    }

    [Fact]
    public void Should_Have_Error_When_DoctorId_Is_Empty()
    {
        // Arrange
        var command = new BookAppointmentCommand(
            Guid.NewGuid(),
            Guid.Empty,
            DateTimeOffset.UtcNow.AddHours(1),
            DateTimeOffset.UtcNow.AddHours(2),
            "Test");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.DoctorId)
            .WithErrorMessage("DoctorId is required");
    }

    [Fact]
    public void Should_Have_Error_When_Start_Is_After_End()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow.AddHours(2);
        var end = DateTimeOffset.UtcNow.AddHours(1);
        var command = new BookAppointmentCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            start,
            end,
            "Test");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Start)
            .WithErrorMessage("Start time must be before end time");
    }

    [Fact]
    public void Should_Have_Error_When_Appointment_Is_Less_Than_10_Minutes()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow.AddHours(1);
        var end = start.AddMinutes(5);
        var command = new BookAppointmentCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            start,
            end,
            "Test");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.End)
            .WithErrorMessage("Appointment must be at least 10 minutes long");
    }

    [Fact]
    public void Should_Have_Error_When_Appointment_Is_Longer_Than_8_Hours()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow.AddHours(1);
        var end = start.AddHours(9);
        var command = new BookAppointmentCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            start,
            end,
            "Test");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.End)
            .WithErrorMessage("Appointment cannot be longer than 8 hours");
    }

    [Fact]
    public void Should_Have_Error_When_Appointment_Is_Not_15_Minutes_In_Advance()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow.AddMinutes(10);
        var end = start.AddMinutes(30);
        var command = new BookAppointmentCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            start,
            end,
            "Test");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Start)
            .WithErrorMessage("Appointment must be scheduled at least 15 minutes in advance");
    }

    [Fact]
    public void Should_Have_Error_When_Notes_Exceed_1024_Characters()
    {
        // Arrange
        var longNotes = new string('A', 1025);
        var command = new BookAppointmentCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(1),
            DateTimeOffset.UtcNow.AddHours(2),
            longNotes);

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Notes)
            .WithErrorMessage("Notes cannot exceed 1024 characters");
    }

    [Fact]
    public void Should_Not_Have_Error_When_All_Fields_Are_Valid()
    {
        // Arrange
        var command = new BookAppointmentCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(1),
            DateTimeOffset.UtcNow.AddHours(2),
            "Valid notes");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Not_Have_Error_When_Notes_Are_Null()
    {
        // Arrange
        var command = new BookAppointmentCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(1),
            DateTimeOffset.UtcNow.AddHours(2),
            null);

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Notes_Are_Exactly_1024_Characters()
    {
        // Arrange
        var maxLengthNotes = new string('A', 1024);
        var command = new BookAppointmentCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(1),
            DateTimeOffset.UtcNow.AddHours(2),
            maxLengthNotes);

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Appointment_Is_Exactly_10_Minutes()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow.AddHours(1);
        var end = start.AddMinutes(10);
        var command = new BookAppointmentCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            start,
            end,
            "Test");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.End);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Appointment_Is_Exactly_8_Hours()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow.AddHours(1);
        var end = start.AddHours(8);
        var command = new BookAppointmentCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            start,
            end,
            "Test");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.End);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Appointment_Is_Exactly_15_Minutes_In_Advance()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow.AddMinutes(15).AddSeconds(1); // Just over 15 minutes
        var end = start.AddMinutes(30);
        var command = new BookAppointmentCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            start,
            end,
            "Test");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Start);
    }
}
