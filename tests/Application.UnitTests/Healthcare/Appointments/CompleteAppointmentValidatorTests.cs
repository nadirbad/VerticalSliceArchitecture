using FluentValidation.TestHelper;

using VerticalSliceArchitecture.Application.Features.Healthcare.Appointments;

namespace VerticalSliceArchitecture.Application.UnitTests.Healthcare.Appointments;

public class CompleteAppointmentValidatorTests
{
    private readonly CompleteAppointmentCommandValidator _validator;

    public CompleteAppointmentValidatorTests()
    {
        _validator = new CompleteAppointmentCommandValidator();
    }

    [Fact]
    public void Should_Have_Error_When_AppointmentId_Is_Empty()
    {
        // Arrange
        var command = new CompleteAppointmentCommand(
            Guid.Empty,
            "Test notes");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AppointmentId)
            .WithErrorMessage("AppointmentId is required");
    }

    [Fact]
    public void Should_Have_Error_When_Notes_Exceed_1024_Characters()
    {
        // Arrange
        var longNotes = new string('A', 1025);
        var command = new CompleteAppointmentCommand(
            Guid.NewGuid(),
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
        var command = new CompleteAppointmentCommand(
            Guid.NewGuid(),
            "Valid completion notes");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Not_Have_Error_When_Notes_Are_Null()
    {
        // Arrange
        var command = new CompleteAppointmentCommand(
            Guid.NewGuid(),
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
        var command = new CompleteAppointmentCommand(
            Guid.NewGuid(),
            maxLengthNotes);

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Notes);
    }
}