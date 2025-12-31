using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using VerticalSliceArchitecture.Application.IntegrationTests.Helpers;
using VerticalSliceArchitecture.Application.IntegrationTests.Infrastructure;

using Xunit;

namespace VerticalSliceArchitecture.Application.IntegrationTests.TestData;

/// <summary>
/// Tests to verify that test data builders work correctly and produce valid data.
/// These tests serve as both verification and documentation for the builder APIs.
/// </summary>
public class TestDataBuilderVerificationTests : IntegrationTestBase
{
    public TestDataBuilderVerificationTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task BookAppointmentBuilder_DefaultValues_ShouldProduceValidRequest()
    {
        // Arrange
        var builder = new BookAppointmentTestDataBuilder();
        var command = builder.Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/healthcare/appointments", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created, "default builder values should create a valid appointment");
    }

    [Fact]
    public async Task BookAppointmentBuilder_WithCustomValues_ShouldOverrideDefaults()
    {
        // Arrange
        var customStart = DateTimeOffset.UtcNow.AddDays(5).Date.AddHours(15);
        var customEnd = customStart.AddHours(1);

        var command = new BookAppointmentTestDataBuilder()
            .WithPatientId(TestSeedData.SecondPatientId)
            .WithDoctorId(TestSeedData.SecondDoctorId)
            .WithTimeRange(customStart, customEnd)
            .WithNotes("Custom test appointment")
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/healthcare/appointments", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task BookAppointmentBuilder_WithNonExistentPatient_ShouldReturn404()
    {
        // Arrange
        var command = new BookAppointmentTestDataBuilder()
            .WithNonExistentPatient()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/healthcare/appointments", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        ResponseHelper.IsNotFoundError(problemDetails).Should().BeTrue();
    }

    [Fact]
    public async Task BookAppointmentBuilder_WithInvalidTimeRange_ShouldReturn400()
    {
        // Arrange
        var command = new BookAppointmentTestDataBuilder()
            .WithInvalidTimeRange()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/healthcare/appointments", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasAnyValidationErrors(problemDetails).Should().BeTrue();
    }

    [Fact]
    public async Task BookAppointmentBuilder_WithTooShortDuration_ShouldReturn400()
    {
        // Arrange
        var command = new BookAppointmentTestDataBuilder()
            .WithTooShortDuration()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/healthcare/appointments", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        ResponseHelper.HasValidationError(problemDetails, "End").Should().BeTrue();
    }

    [Fact]
    public void TestSeedData_ShouldProvideConsistentGUIDs()
    {
        // Arrange & Act
        var patientId1 = TestSeedData.DefaultPatientId;
        var patientId2 = TestSeedData.DefaultPatientId;
        var doctorId1 = TestSeedData.DefaultDoctorId;
        var doctorId2 = TestSeedData.DefaultDoctorId;

        // Assert
        patientId1.Should().Be(patientId2, "default patient ID should be deterministic");
        doctorId1.Should().Be(doctorId2, "default doctor ID should be deterministic");
        patientId1.Should().NotBe(doctorId1, "patient and doctor IDs should be different");
    }

    [Fact]
    public void TestSeedData_ShouldProvideMultiplePatients()
    {
        // Act
        var patients = TestSeedData.GetAllTestPatients().ToList();

        // Assert
        patients.Should().HaveCountGreaterOrEqualTo(3, "should provide at least 3 test patients");
        patients.Select(p => p.Id).Should().OnlyHaveUniqueItems("all patient IDs should be unique");
        patients.Select(p => p.Name).Should().OnlyHaveUniqueItems("all patient names should be unique");
    }

    [Fact]
    public void TestSeedData_ShouldProvideMultipleDoctors()
    {
        // Act
        var doctors = TestSeedData.GetAllTestDoctors().ToList();

        // Assert
        doctors.Should().HaveCountGreaterOrEqualTo(3, "should provide at least 3 test doctors");
        doctors.Select(d => d.Id).Should().OnlyHaveUniqueItems("all doctor IDs should be unique");
        doctors.Select(d => d.Name).Should().OnlyHaveUniqueItems("all doctor names should be unique");
    }

    [Fact]
    public void ResponseHelper_ShouldCorrectlyIdentifyErrorTypes()
    {
        // Arrange
        var validationError = new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = 400 };
        var notFoundError = new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = 404 };
        var conflictError = new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = 409 };
        var unprocessableError = new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = 422 };

        // Assert
        ResponseHelper.IsValidationError(validationError).Should().BeTrue();
        ResponseHelper.IsNotFoundError(notFoundError).Should().BeTrue();
        ResponseHelper.IsConflictError(conflictError).Should().BeTrue();
        ResponseHelper.IsUnprocessableEntityError(unprocessableError).Should().BeTrue();

        // Cross-check
        ResponseHelper.IsValidationError(notFoundError).Should().BeFalse();
        ResponseHelper.IsNotFoundError(validationError).Should().BeFalse();
    }
}