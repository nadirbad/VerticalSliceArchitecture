using FluentValidation.TestHelper;

using VerticalSliceArchitecture.Application.Scheduling;

namespace VerticalSliceArchitecture.Application.UnitTests.Scheduling;

public class CompleteAppointmentValidatorTests
{
    private readonly CompleteAppointmentCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_AppointmentId_Is_Empty()
    {
        // Arrange
        var command = new CompleteAppointmentCommand(Guid.Empty, "Notes");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AppointmentId);
    }

    [Fact]
    public void Should_Have_Error_When_Notes_Too_Long()
    {
        // Arrange
        var command = new CompleteAppointmentCommand(Guid.NewGuid(), new string('A', 1025));

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Valid()
    {
        // Arrange
        var command = new CompleteAppointmentCommand(Guid.NewGuid(), "Valid notes");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Not_Have_Error_When_Notes_Are_Null()
    {
        // Arrange
        var command = new CompleteAppointmentCommand(Guid.NewGuid(), null);

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Notes);
    }
}
