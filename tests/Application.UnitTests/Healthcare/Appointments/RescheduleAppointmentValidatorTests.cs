using FluentValidation.TestHelper;

using VerticalSliceArchitecture.Application.Features.Healthcare.Appointments;

namespace VerticalSliceArchitecture.Application.UnitTests.Healthcare.Appointments;

public class RescheduleAppointmentValidatorTests
{
    private readonly RescheduleAppointmentCommandValidator _validator;

    public RescheduleAppointmentValidatorTests()
    {
        _validator = new RescheduleAppointmentCommandValidator();
    }

    [Fact]
    public void Should_Have_Error_When_AppointmentId_Is_Empty()
    {
        // Arrange
        var command = new RescheduleAppointmentCommand(
            Guid.Empty,
            DateTimeOffset.UtcNow.AddHours(3),
            DateTimeOffset.UtcNow.AddHours(4),
            null);

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AppointmentId)
            .WithErrorMessage("AppointmentId is required");
    }

    [Fact]
    public void Should_Have_Error_When_NewStart_Is_After_NewEnd()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow.AddHours(4);
        var end = DateTimeOffset.UtcNow.AddHours(3);
        var command = new RescheduleAppointmentCommand(
            Guid.NewGuid(),
            start,
            end,
            null);

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.NewStart)
            .WithErrorMessage("New start time must be before new end time");
    }

    [Fact]
    public void Should_Have_Error_When_Duration_Is_Less_Than_10_Minutes()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow.AddHours(3);
        var end = start.AddMinutes(5);
        var command = new RescheduleAppointmentCommand(
            Guid.NewGuid(),
            start,
            end,
            null);

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.NewEnd)
            .WithErrorMessage("Appointment must be at least 10 minutes long");
    }

    [Fact]
    public void Should_Have_Error_When_Duration_Is_Longer_Than_8_Hours()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow.AddHours(3);
        var end = start.AddHours(9);
        var command = new RescheduleAppointmentCommand(
            Guid.NewGuid(),
            start,
            end,
            null);

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.NewEnd)
            .WithErrorMessage("Appointment cannot be longer than 8 hours");
    }

    [Fact]
    public void Should_Have_Error_When_NewStart_Is_Not_2_Hours_In_Advance()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow.AddMinutes(90); // 1.5 hours
        var end = start.AddMinutes(30);
        var command = new RescheduleAppointmentCommand(
            Guid.NewGuid(),
            start,
            end,
            null);

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.NewStart)
            .WithErrorMessage("Appointment must be rescheduled at least 2 hours in advance");
    }

    [Fact]
    public void Should_Have_Error_When_Reason_Exceeds_512_Characters()
    {
        // Arrange
        var longReason = new string('A', 513);
        var command = new RescheduleAppointmentCommand(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(3),
            DateTimeOffset.UtcNow.AddHours(4),
            longReason);

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("Reason cannot exceed 512 characters");
    }

    [Fact]
    public void Should_Not_Have_Error_When_All_Fields_Are_Valid()
    {
        // Arrange
        var command = new RescheduleAppointmentCommand(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(3),
            DateTimeOffset.UtcNow.AddHours(4),
            "Patient requested earlier time");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Not_Have_Error_When_Reason_Is_Null()
    {
        // Arrange
        var command = new RescheduleAppointmentCommand(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(3),
            DateTimeOffset.UtcNow.AddHours(4),
            null);

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Reason_Is_Exactly_512_Characters()
    {
        // Arrange
        var maxLengthReason = new string('A', 512);
        var command = new RescheduleAppointmentCommand(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(3),
            DateTimeOffset.UtcNow.AddHours(4),
            maxLengthReason);

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Duration_Is_Exactly_10_Minutes()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow.AddHours(3);
        var end = start.AddMinutes(10);
        var command = new RescheduleAppointmentCommand(
            Guid.NewGuid(),
            start,
            end,
            null);

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.NewEnd);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Duration_Is_Exactly_8_Hours()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow.AddHours(3);
        var end = start.AddHours(8);
        var command = new RescheduleAppointmentCommand(
            Guid.NewGuid(),
            start,
            end,
            null);

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.NewEnd);
    }

    [Fact]
    public void Should_Not_Have_Error_When_NewStart_Is_Exactly_2_Hours_In_Advance()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow.AddHours(2).AddSeconds(1); // Just over 2 hours
        var end = start.AddMinutes(30);
        var command = new RescheduleAppointmentCommand(
            Guid.NewGuid(),
            start,
            end,
            null);

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.NewStart);
    }
}
