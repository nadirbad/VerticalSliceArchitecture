using FluentValidation;
using FluentValidation.Results;

using MediatR;

using VerticalSliceArchitecture.Application.Common.Behaviours;
using VerticalSliceArchitecture.Application.Scheduling;

namespace VerticalSliceArchitecture.Application.UnitTests.Common.Behaviours;

public class ValidationBehaviorTests
{
    private readonly ValidationBehaviour<BookAppointmentCommand, ErrorOr<BookAppointmentResult>> _validationBehavior;
    private readonly IValidator<BookAppointmentCommand> _mockValidator;
    private readonly RequestHandlerDelegate<ErrorOr<BookAppointmentResult>> _mockNextBehavior;

    public ValidationBehaviorTests()
    {
        _mockNextBehavior = Substitute.For<RequestHandlerDelegate<ErrorOr<BookAppointmentResult>>>();
        _mockValidator = Substitute.For<IValidator<BookAppointmentCommand>>();

        _validationBehavior = new(_mockValidator);
    }

    [Fact]
    public async Task InvokeValidationBehavior_WhenValidatorResultIsValid_ShouldInvokeNextBehavior()
    {
        // Arrange
        var command = new BookAppointmentCommand(
            PatientId: Guid.NewGuid(),
            DoctorId: Guid.NewGuid(),
            Start: DateTimeOffset.UtcNow.AddDays(1),
            End: DateTimeOffset.UtcNow.AddDays(1).AddHours(1),
            Notes: "Test appointment");

        var expectedResponse = new BookAppointmentResult(
            Id: Guid.NewGuid(),
            StartUtc: command.Start.UtcDateTime,
            EndUtc: command.End.UtcDateTime);

        _mockValidator
            .ValidateAsync(command, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mockNextBehavior.Invoke().Returns(expectedResponse);

        // Act
        var result = await _validationBehavior.Handle(command, _mockNextBehavior, default);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task InvokeValidationBehavior_WhenValidatorResultIsNotValid_ShouldReturnListOfErrors()
    {
        // Arrange
        var command = new BookAppointmentCommand(
            PatientId: Guid.Empty,
            DoctorId: Guid.NewGuid(),
            Start: DateTimeOffset.UtcNow.AddDays(1),
            End: DateTimeOffset.UtcNow.AddDays(1).AddHours(1),
            Notes: "Test appointment");

        List<ValidationFailure> validationFailures = [new(propertyName: "PatientId", errorMessage: "Patient ID is required")];

        _mockValidator
            .ValidateAsync(command, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(validationFailures));

        // Act
        var result = await _validationBehavior.Handle(command, _mockNextBehavior, default);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("PatientId");
        result.FirstError.Description.Should().Be("Patient ID is required");
    }

    [Fact]
    public async Task InvokeValidationBehavior_WhenNoValidator_ShouldInvokeNextBehavior()
    {
        // Arrange
        var command = new BookAppointmentCommand(
            PatientId: Guid.NewGuid(),
            DoctorId: Guid.NewGuid(),
            Start: DateTimeOffset.UtcNow.AddDays(1),
            End: DateTimeOffset.UtcNow.AddDays(1).AddHours(1),
            Notes: "Test appointment");

        var validationBehavior = new ValidationBehaviour<BookAppointmentCommand, ErrorOr<BookAppointmentResult>>();

        var expectedResponse = new BookAppointmentResult(
            Id: Guid.NewGuid(),
            StartUtc: command.Start.UtcDateTime,
            EndUtc: command.End.UtcDateTime);

        _mockNextBehavior.Invoke().Returns(expectedResponse);

        // Act
        var result = await validationBehavior.Handle(command, _mockNextBehavior, default);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(expectedResponse);
    }
}