using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using VerticalSliceArchitecture.Application.Domain;
using VerticalSliceArchitecture.Application.IntegrationTests.Helpers;
using VerticalSliceArchitecture.Application.IntegrationTests.Infrastructure;
using VerticalSliceArchitecture.Application.IntegrationTests.TestData;
using VerticalSliceArchitecture.Application.Medications;

namespace VerticalSliceArchitecture.Application.IntegrationTests.Healthcare.Prescriptions;

/// <summary>
/// Integration tests for the IssuePrescription endpoint.
/// Tests the full request/response cycle including validation, business logic, and database operations.
/// </summary>
public class IssuePrescriptionTests : IntegrationTestBase
{
    public IssuePrescriptionTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task IssuePrescription_WithValidData_Returns201CreatedWithPrescriptionDetails()
    {
        // Arrange
        var command = new IssuePrescriptionTestDataBuilder().Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/prescriptions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<PrescriptionResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        result.PatientId.Should().Be(TestSeedData.DefaultPatientId);
        result.DoctorId.Should().Be(TestSeedData.DefaultDoctorId);
        result.MedicationName.Should().Be("Amoxicillin");
        result.Dosage.Should().Be("500mg");
        result.Directions.Should().Be("Take one capsule three times daily with food");
        result.Quantity.Should().Be(30);
        result.NumberOfRefills.Should().Be(2);
        result.RemainingRefills.Should().Be(2);
        result.Status.Should().Be("Active");
        result.IsExpired.Should().BeFalse();
        result.IsDepleted.Should().BeFalse();

        // Verify Location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/api/prescriptions/{result.Id}");

        // Verify prescription was saved to database
        var savedPrescription = await DbContext.Prescriptions
            .FirstOrDefaultAsync(p => p.Id == result.Id);

        savedPrescription.Should().NotBeNull();
        savedPrescription!.PatientId.Should().Be(TestSeedData.DefaultPatientId);
        savedPrescription.DoctorId.Should().Be(TestSeedData.DefaultDoctorId);
        savedPrescription.Status.Should().Be(PrescriptionStatus.Active);
    }

    [Fact]
    public async Task IssuePrescription_WithQuantityZero_Returns400WithValidationError()
    {
        // Arrange
        var command = new IssuePrescriptionTestDataBuilder()
            .WithInvalidQuantityTooLow()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/prescriptions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "Quantity").Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "Quantity");
        errorMessage.Should().Contain("between 1 and 999");
    }

    [Fact]
    public async Task IssuePrescription_WithQuantityGreaterThan999_Returns400WithValidationError()
    {
        // Arrange
        var command = new IssuePrescriptionTestDataBuilder()
            .WithInvalidQuantityTooHigh()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/prescriptions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "Quantity").Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "Quantity");
        errorMessage.Should().Contain("between 1 and 999");
    }

    [Fact]
    public async Task IssuePrescription_WithRefillsNegative_Returns400WithValidationError()
    {
        // Arrange
        var command = new IssuePrescriptionTestDataBuilder()
            .WithInvalidRefillsNegative()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/prescriptions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "NumberOfRefills").Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "NumberOfRefills");
        errorMessage.Should().Contain("between 0 and 12");
    }

    [Fact]
    public async Task IssuePrescription_WithRefillsGreaterThan12_Returns400WithValidationError()
    {
        // Arrange
        var command = new IssuePrescriptionTestDataBuilder()
            .WithInvalidRefillsTooHigh()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/prescriptions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "NumberOfRefills").Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "NumberOfRefills");
        errorMessage.Should().Contain("between 0 and 12");
    }

    [Fact]
    public async Task IssuePrescription_WithDurationZero_Returns400WithValidationError()
    {
        // Arrange
        var command = new IssuePrescriptionTestDataBuilder()
            .WithInvalidDurationTooLow()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/prescriptions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "DurationInDays").Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "DurationInDays");
        errorMessage.Should().Contain("between 1 and 365 days");
    }

    [Fact]
    public async Task IssuePrescription_WithDurationGreaterThan365_Returns400WithValidationError()
    {
        // Arrange
        var command = new IssuePrescriptionTestDataBuilder()
            .WithInvalidDurationTooHigh()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/prescriptions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "DurationInDays").Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "DurationInDays");
        errorMessage.Should().Contain("between 1 and 365 days");
    }

    [Fact]
    public async Task IssuePrescription_WithNonExistentPatientId_Returns404NotFound()
    {
        // Arrange
        var command = new IssuePrescriptionTestDataBuilder()
            .WithNonExistentPatient()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/prescriptions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsNotFoundError(problemDetails).Should().BeTrue();
        problemDetails!.Title.Should().Contain("Patient");
        problemDetails.Title.Should().Contain(TestSeedData.NonExistentId.ToString());
    }

    [Fact]
    public async Task IssuePrescription_WithNonExistentDoctorId_Returns404NotFound()
    {
        // Arrange
        var command = new IssuePrescriptionTestDataBuilder()
            .WithNonExistentDoctor()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/prescriptions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsNotFoundError(problemDetails).Should().BeTrue();
        problemDetails!.Title.Should().Contain("Doctor");
        problemDetails.Title.Should().Contain(TestSeedData.NonExistentId.ToString());
    }

    [Fact]
    public async Task IssuePrescription_WithEmptyMedicationName_Returns400WithValidationError()
    {
        // Arrange
        var command = new IssuePrescriptionTestDataBuilder()
            .WithEmptyMedicationName()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/prescriptions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "MedicationName").Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "MedicationName");
        errorMessage.Should().Contain("required");
    }

    [Fact]
    public async Task IssuePrescription_WithMedicationNameExceeding200Characters_Returns400WithValidationError()
    {
        // Arrange
        var command = new IssuePrescriptionTestDataBuilder()
            .WithTooLongMedicationName()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/prescriptions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "MedicationName").Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "MedicationName");
        errorMessage.Should().Contain("cannot exceed 200 characters");
    }

    [Fact]
    public async Task IssuePrescription_WithEmptyDosage_Returns400WithValidationError()
    {
        // Arrange
        var command = new IssuePrescriptionTestDataBuilder()
            .WithEmptyDosage()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/prescriptions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "Dosage").Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "Dosage");
        errorMessage.Should().Contain("required");
    }

    [Fact]
    public async Task IssuePrescription_WithDosageExceeding50Characters_Returns400WithValidationError()
    {
        // Arrange
        var command = new IssuePrescriptionTestDataBuilder()
            .WithTooLongDosage()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/prescriptions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "Dosage").Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "Dosage");
        errorMessage.Should().Contain("cannot exceed 50 characters");
    }

    [Fact]
    public async Task IssuePrescription_WithEmptyDirections_Returns400WithValidationError()
    {
        // Arrange
        var command = new IssuePrescriptionTestDataBuilder()
            .WithEmptyDirections()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/prescriptions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "Directions").Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "Directions");
        errorMessage.Should().Contain("required");
    }

    [Fact]
    public async Task IssuePrescription_WithDirectionsExceeding500Characters_Returns400WithValidationError()
    {
        // Arrange
        var command = new IssuePrescriptionTestDataBuilder()
            .WithTooLongDirections()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/prescriptions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "Directions").Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "Directions");
        errorMessage.Should().Contain("cannot exceed 500 characters");
    }

    [Fact]
    public async Task IssuePrescription_WithEmptyPatientId_Returns400WithValidationError()
    {
        // Arrange
        var command = new IssuePrescriptionTestDataBuilder()
            .WithPatientId(Guid.Empty)
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/prescriptions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "PatientId").Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "PatientId");
        errorMessage.Should().Contain("required");
    }

    [Fact]
    public async Task IssuePrescription_WithEmptyDoctorId_Returns400WithValidationError()
    {
        // Arrange
        var command = new IssuePrescriptionTestDataBuilder()
            .WithDoctorId(Guid.Empty)
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/prescriptions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "DoctorId").Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "DoctorId");
        errorMessage.Should().Contain("required");
    }

    [Fact]
    public async Task IssuePrescription_AsControlledSubstance_Returns201Created()
    {
        // Arrange - Controlled substances typically have 0 refills
        var command = new IssuePrescriptionTestDataBuilder()
            .AsControlledSubstance()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/prescriptions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<PrescriptionResponse>();
        result.Should().NotBeNull();
        result!.MedicationName.Should().Contain("Hydrocodone");
        result.NumberOfRefills.Should().Be(0);
        result.RemainingRefills.Should().Be(0);
        result.Status.Should().Be("Active");
    }

    [Fact]
    public async Task IssuePrescription_AsLongTermMedication_Returns201Created()
    {
        // Arrange - Long-term medications can have max refills (12) and duration (365 days)
        var command = new IssuePrescriptionTestDataBuilder()
            .AsLongTermMedication()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/prescriptions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<PrescriptionResponse>();
        result.Should().NotBeNull();
        result!.MedicationName.Should().Be("Lisinopril");
        result.NumberOfRefills.Should().Be(12);
        result.RemainingRefills.Should().Be(12);
        result.Quantity.Should().Be(90);
        result.Status.Should().Be("Active");

        // Verify expiration date is approximately 1 year from now
        var expectedExpiration = DateTime.UtcNow.AddDays(365);
        result.ExpirationDateUtc.Should().BeCloseTo(expectedExpiration, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task IssuePrescription_MultiplePrescriptionsForSamePatient_Returns201Created()
    {
        // Arrange - Issue first prescription
        var firstCommand = new IssuePrescriptionTestDataBuilder()
            .WithMedicationName("Aspirin")
            .Build();

        var firstResponse = await Client.PostAsJsonAsync("/api/prescriptions", firstCommand);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Issue second prescription for same patient
        var secondCommand = new IssuePrescriptionTestDataBuilder()
            .WithMedicationName("Ibuprofen")
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/prescriptions", secondCommand);

        // Assert - Should allow multiple prescriptions for same patient
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<PrescriptionResponse>();
        result.Should().NotBeNull();
        result!.MedicationName.Should().Be("Ibuprofen");

        // Verify both prescriptions exist in database
        var prescriptions = await DbContext.Prescriptions
            .Where(p => p.PatientId == TestSeedData.DefaultPatientId)
            .ToListAsync();

        prescriptions.Should().HaveCount(2);
        prescriptions.Select(p => p.MedicationName).Should().Contain("Aspirin").And.Contain("Ibuprofen");
    }

    [Fact]
    public async Task IssuePrescription_WithBoundaryQuantity1_Returns201Created()
    {
        // Arrange - Test minimum valid quantity
        var command = new IssuePrescriptionTestDataBuilder()
            .WithQuantity(1)
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/prescriptions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<PrescriptionResponse>();
        result.Should().NotBeNull();
        result!.Quantity.Should().Be(1);
    }

    [Fact]
    public async Task IssuePrescription_WithBoundaryQuantity999_Returns201Created()
    {
        // Arrange - Test maximum valid quantity
        var command = new IssuePrescriptionTestDataBuilder()
            .WithQuantity(999)
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/prescriptions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<PrescriptionResponse>();
        result.Should().NotBeNull();
        result!.Quantity.Should().Be(999);
    }

    [Fact]
    public async Task IssuePrescription_WithBoundaryRefills0_Returns201Created()
    {
        // Arrange - Test minimum valid refills (0 refills is valid)
        var command = new IssuePrescriptionTestDataBuilder()
            .WithNumberOfRefills(0)
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/prescriptions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<PrescriptionResponse>();
        result.Should().NotBeNull();
        result!.NumberOfRefills.Should().Be(0);
        result.RemainingRefills.Should().Be(0);
    }

    [Fact]
    public async Task IssuePrescription_WithBoundaryRefills12_Returns201Created()
    {
        // Arrange - Test maximum valid refills
        var command = new IssuePrescriptionTestDataBuilder()
            .WithNumberOfRefills(12)
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/prescriptions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<PrescriptionResponse>();
        result.Should().NotBeNull();
        result!.NumberOfRefills.Should().Be(12);
        result.RemainingRefills.Should().Be(12);
    }

    [Fact]
    public async Task IssuePrescription_WithBoundaryDuration1_Returns201Created()
    {
        // Arrange - Test minimum valid duration
        var command = new IssuePrescriptionTestDataBuilder()
            .WithDurationInDays(1)
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/prescriptions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<PrescriptionResponse>();
        result.Should().NotBeNull();

        // Verify expiration is approximately 1 day from now
        var expectedExpiration = DateTime.UtcNow.AddDays(1);
        result!.ExpirationDateUtc.Should().BeCloseTo(expectedExpiration, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task IssuePrescription_WithBoundaryDuration365_Returns201Created()
    {
        // Arrange - Test maximum valid duration
        var command = new IssuePrescriptionTestDataBuilder()
            .WithDurationInDays(365)
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/prescriptions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<PrescriptionResponse>();
        result.Should().NotBeNull();

        // Verify expiration is approximately 365 days from now
        var expectedExpiration = DateTime.UtcNow.AddDays(365);
        result!.ExpirationDateUtc.Should().BeCloseTo(expectedExpiration, TimeSpan.FromMinutes(1));
    }
}