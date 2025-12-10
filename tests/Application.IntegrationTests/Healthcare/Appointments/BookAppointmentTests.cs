using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using VerticalSliceArchitecture.Application.Features.Healthcare.Appointments;
using VerticalSliceArchitecture.Application.IntegrationTests.Helpers;
using VerticalSliceArchitecture.Application.IntegrationTests.Infrastructure;
using VerticalSliceArchitecture.Application.IntegrationTests.TestData;

namespace VerticalSliceArchitecture.Application.IntegrationTests.Healthcare.Appointments;

/// <summary>
/// Integration tests for the BookAppointment endpoint.
/// Tests the full request/response cycle including validation, business logic, and database operations.
/// </summary>
public class BookAppointmentTests : IntegrationTestBase
{
    public BookAppointmentTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task BookAppointment_WithValidData_Returns201CreatedWithAppointmentDetails()
    {
        // Arrange
        var builder = new BookAppointmentTestDataBuilder();
        var command = builder.Build();
        var (_, _, expectedStart, expectedEnd, _) = builder.BuildValues();

        // Act
        var response = await Client.PostAsJsonAsync("/api/healthcare/appointments", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<BookAppointmentResult>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();

        // The API converts DateTimeOffset to UTC DateTime, so we need to compare with the UTC representation
        result.StartUtc.Should().BeCloseTo(expectedStart.UtcDateTime, TimeSpan.FromSeconds(1));
        result.EndUtc.Should().BeCloseTo(expectedEnd.UtcDateTime, TimeSpan.FromSeconds(1));

        // Verify Location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/api/healthcare/appointments/{result.Id}");

        // Verify appointment was saved to database
        var savedAppointment = await DbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == result.Id);

        savedAppointment.Should().NotBeNull();
        savedAppointment!.PatientId.Should().Be(TestSeedData.DefaultPatientId);
        savedAppointment.DoctorId.Should().Be(TestSeedData.DefaultDoctorId);
        savedAppointment.Status.Should().Be(Domain.Healthcare.AppointmentStatus.Scheduled);
    }

    [Fact]
    public async Task BookAppointment_WithStartTimeAfterEndTime_Returns400WithValidationErrors()
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
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "Start").Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "Start");
        errorMessage.Should().Contain("before end time");
    }

    [Fact]
    public async Task BookAppointment_WithDurationLessThan10Minutes_Returns400WithValidationError()
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
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "End").Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "End");
        errorMessage.Should().Contain("at least 10 minutes");
    }

    [Fact]
    public async Task BookAppointment_WithNonExistentPatientId_Returns404NotFound()
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
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsNotFoundError(problemDetails).Should().BeTrue();
        problemDetails!.Title.Should().Contain("Patient");
        problemDetails.Title.Should().Contain(TestSeedData.NonExistentId.ToString());
    }

    [Fact]
    public async Task BookAppointment_WithOverlappingDoctorAppointment_Returns409Conflict()
    {
        // Arrange
        // First, book an appointment
        var firstCommand = new BookAppointmentTestDataBuilder()
            .WithStartTime(DateTimeOffset.UtcNow.AddDays(7).Date.AddHours(14))
            .WithDuration(60)
            .Build();

        var firstResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", firstCommand);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Try to book an overlapping appointment for the same doctor
        var overlappingCommand = new BookAppointmentTestDataBuilder()
            .WithStartTime(DateTimeOffset.UtcNow.AddDays(7).Date.AddHours(14).AddMinutes(30)) // Starts 30 min into first appointment
            .WithDuration(30)
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/healthcare/appointments", overlappingCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsConflictError(problemDetails).Should().BeTrue();
        problemDetails!.Title.Should().Contain("conflicting appointment");
    }

    [Fact]
    public async Task BookAppointment_WithDurationGreaterThan8Hours_Returns400WithValidationError()
    {
        // Arrange
        var command = new BookAppointmentTestDataBuilder()
            .WithTooLongDuration()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/healthcare/appointments", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "End").Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "End");
        errorMessage.Should().Contain("cannot be longer than 8 hours");
    }

    [Fact]
    public async Task BookAppointment_WithNotesExceeding1024Characters_Returns400WithValidationError()
    {
        // Arrange
        var command = new BookAppointmentTestDataBuilder()
            .WithTooLongNotes()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/healthcare/appointments", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "Notes").Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "Notes");
        errorMessage.Should().Contain("cannot exceed 1024 characters");
    }

    [Fact]
    public async Task BookAppointment_WithNonExistentDoctorId_Returns404NotFound()
    {
        // Arrange
        var command = new BookAppointmentTestDataBuilder()
            .WithNonExistentDoctor()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/healthcare/appointments", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsNotFoundError(problemDetails).Should().BeTrue();
        problemDetails!.Title.Should().Contain("Doctor");
        problemDetails.Title.Should().Contain(TestSeedData.NonExistentId.ToString());
    }

    [Fact]
    public async Task BookAppointment_ScheduledLessThan15MinutesInAdvance_Returns400WithValidationError()
    {
        // Arrange
        var command = new BookAppointmentTestDataBuilder()
            .TooSoon()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/healthcare/appointments", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "Start").Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "Start");
        errorMessage.Should().Contain("at least 15 minutes in advance");
    }

    [Fact]
    public async Task BookAppointment_WithEmptyPatientId_Returns400WithValidationError()
    {
        // Arrange
        var command = new BookAppointmentTestDataBuilder()
            .WithPatientId(Guid.Empty)
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/healthcare/appointments", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "PatientId").Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "PatientId");
        errorMessage.Should().Contain("PatientId is required");
    }

    [Fact]
    public async Task BookAppointment_WithEmptyDoctorId_Returns400WithValidationError()
    {
        // Arrange
        var command = new BookAppointmentTestDataBuilder()
            .WithDoctorId(Guid.Empty)
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/healthcare/appointments", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "DoctorId").Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "DoctorId");
        errorMessage.Should().Contain("DoctorId is required");
    }

    [Fact]
    public async Task BookAppointment_WithDifferentPatients_SameDoctorAndTime_Returns409Conflict()
    {
        // Arrange
        // First patient books an appointment
        var firstCommand = new BookAppointmentTestDataBuilder()
            .WithPatientId(TestSeedData.DefaultPatientId)
            .WithStartTime(DateTimeOffset.UtcNow.AddDays(8).Date.AddHours(9))
            .WithDuration(45)
            .Build();

        var firstResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", firstCommand);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Second patient tries to book the same doctor at the same time
        var secondCommand = new BookAppointmentTestDataBuilder()
            .WithPatientId(TestSeedData.SecondPatientId)
            .WithStartTime(DateTimeOffset.UtcNow.AddDays(8).Date.AddHours(9))
            .WithDuration(45)
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/healthcare/appointments", secondCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsConflictError(problemDetails).Should().BeTrue();
        problemDetails!.Title.Should().Contain("conflicting appointment");
    }

    [Fact]
    public async Task BookAppointment_WithSamePatient_DifferentDoctors_SameTime_Returns201Created()
    {
        // Arrange
        // Book appointment with first doctor
        var firstCommand = new BookAppointmentTestDataBuilder()
            .WithDoctorId(TestSeedData.DefaultDoctorId)
            .WithStartTime(DateTimeOffset.UtcNow.AddDays(9).Date.AddHours(11))
            .WithDuration(30)
            .Build();

        var firstResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", firstCommand);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Book another appointment with different doctor at same time
        var secondCommand = new BookAppointmentTestDataBuilder()
            .WithDoctorId(TestSeedData.SecondDoctorId)
            .WithStartTime(DateTimeOffset.UtcNow.AddDays(9).Date.AddHours(11))
            .WithDuration(30)
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/healthcare/appointments", secondCommand);

        // Assert - This should succeed as different doctors can be booked at the same time
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<BookAppointmentResult>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task BookAppointment_WithBackToBackAppointments_SameDoctor_Returns201Created()
    {
        // Arrange
        // Book first appointment from 10:00 to 10:30
        var firstCommand = new BookAppointmentTestDataBuilder()
            .WithStartTime(DateTimeOffset.UtcNow.AddDays(10).Date.AddHours(10))
            .WithDuration(30)
            .Build();

        var firstResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", firstCommand);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Book second appointment from 10:30 to 11:00 (immediately after first)
        var secondCommand = new BookAppointmentTestDataBuilder()
            .WithPatientId(TestSeedData.SecondPatientId)
            .WithStartTime(DateTimeOffset.UtcNow.AddDays(10).Date.AddHours(10).AddMinutes(30))
            .WithDuration(30)
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/healthcare/appointments", secondCommand);

        // Assert - This should succeed as appointments are back-to-back, not overlapping
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<BookAppointmentResult>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task BookAppointment_WithNullNotes_Returns201Created()
    {
        // Arrange
        var command = new BookAppointmentTestDataBuilder()
            .WithNotes(null)
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/healthcare/appointments", command);

        // Assert - Notes are optional, so this should succeed
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<BookAppointmentResult>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();

        var savedAppointment = await DbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == result.Id);

        savedAppointment.Should().NotBeNull();
        savedAppointment!.Notes.Should().BeNull();
    }
}