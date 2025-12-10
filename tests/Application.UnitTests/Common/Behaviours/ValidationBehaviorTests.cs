using FluentValidation;
using FluentValidation.Results;

using MediatR;

using VerticalSliceArchitecture.Application.Common.Behaviours;
using VerticalSliceArchitecture.Application.Features.Healthcare;

namespace VerticalSliceArchitecture.Application.UnitTests.Common.Behaviours;

public class ValidationBehaviorTests
{
    private readonly ValidationBehaviour<IssuePrescriptionCommand, ErrorOr<PrescriptionResponse>> _validationBehavior;
    private readonly IValidator<IssuePrescriptionCommand> _mockValidator;
    private readonly RequestHandlerDelegate<ErrorOr<PrescriptionResponse>> _mockNextBehavior;

    public ValidationBehaviorTests()
    {
        _mockNextBehavior = Substitute.For<RequestHandlerDelegate<ErrorOr<PrescriptionResponse>>>();
        _mockValidator = Substitute.For<IValidator<IssuePrescriptionCommand>>();

        _validationBehavior = new(_mockValidator);
    }

    [Fact]
    public async Task InvokeValidationBehavior_WhenValidatorResultIsValid_ShouldInvokeNextBehavior()
    {
        // Arrange
        var command = new IssuePrescriptionCommand(
            PatientId: Guid.NewGuid(),
            DoctorId: Guid.NewGuid(),
            MedicationName: "Amoxicillin",
            Dosage: "500mg",
            Directions: "Take one capsule three times daily",
            Quantity: 30,
            NumberOfRefills: 2,
            DurationInDays: 10);

        var expectedResponse = new PrescriptionResponse(
            Id: Guid.NewGuid(),
            PatientId: command.PatientId,
            PatientName: "John Doe",
            DoctorId: command.DoctorId,
            DoctorName: "Dr. Smith",
            MedicationName: command.MedicationName,
            Dosage: command.Dosage,
            Directions: command.Directions,
            Quantity: command.Quantity,
            NumberOfRefills: command.NumberOfRefills,
            RemainingRefills: command.NumberOfRefills,
            IssuedDateUtc: DateTime.UtcNow,
            ExpirationDateUtc: DateTime.UtcNow.AddDays(command.DurationInDays),
            Status: "Active",
            IsExpired: false,
            IsDepleted: false);

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
        var command = new IssuePrescriptionCommand(
            PatientId: Guid.Empty,
            DoctorId: Guid.NewGuid(),
            MedicationName: "Amoxicillin",
            Dosage: "500mg",
            Directions: "Take one capsule three times daily",
            Quantity: 30,
            NumberOfRefills: 2,
            DurationInDays: 10);

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
        var command = new IssuePrescriptionCommand(
            PatientId: Guid.NewGuid(),
            DoctorId: Guid.NewGuid(),
            MedicationName: "Amoxicillin",
            Dosage: "500mg",
            Directions: "Take one capsule three times daily",
            Quantity: 30,
            NumberOfRefills: 2,
            DurationInDays: 10);

        var validationBehavior = new ValidationBehaviour<IssuePrescriptionCommand, ErrorOr<PrescriptionResponse>>();

        var expectedResponse = new PrescriptionResponse(
            Id: Guid.NewGuid(),
            PatientId: command.PatientId,
            PatientName: "John Doe",
            DoctorId: command.DoctorId,
            DoctorName: "Dr. Smith",
            MedicationName: command.MedicationName,
            Dosage: command.Dosage,
            Directions: command.Directions,
            Quantity: command.Quantity,
            NumberOfRefills: command.NumberOfRefills,
            RemainingRefills: command.NumberOfRefills,
            IssuedDateUtc: DateTime.UtcNow,
            ExpirationDateUtc: DateTime.UtcNow.AddDays(command.DurationInDays),
            Status: "Active",
            IsExpired: false,
            IsDepleted: false);

        _mockNextBehavior.Invoke().Returns(expectedResponse);

        // Act
        var result = await validationBehavior.Handle(command, _mockNextBehavior, default);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(expectedResponse);
    }
}