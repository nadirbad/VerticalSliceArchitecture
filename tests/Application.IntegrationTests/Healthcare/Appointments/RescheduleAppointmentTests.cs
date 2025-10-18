using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using VerticalSliceArchitecture.Application.Domain.Healthcare;
using VerticalSliceArchitecture.Application.Features.Healthcare.Appointments;
using VerticalSliceArchitecture.Application.IntegrationTests.Helpers;
using VerticalSliceArchitecture.Application.IntegrationTests.Infrastructure;
using VerticalSliceArchitecture.Application.IntegrationTests.TestData;

namespace VerticalSliceArchitecture.Application.IntegrationTests.Healthcare.Appointments;

/// <summary>
/// Integration tests for the RescheduleAppointment endpoint.
/// Tests the full request/response cycle including validation, business logic, and database operations.
/// </summary>
public class RescheduleAppointmentTests : IntegrationTestBase
{
    public RescheduleAppointmentTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task RescheduleAppointment_WithValidData_Returns200OkWithUpdatedDetails()
    {
        // Arrange - First book an appointment
        var bookCommand = new BookAppointmentTestDataBuilder()
            .WithStartTime(DateTimeOffset.UtcNow.AddDays(7).Date.AddHours(10))
            .WithDuration(30)
            .Build();

        var bookResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", bookCommand);
        var bookResult = await bookResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();
        var appointmentId = bookResult!.Id;

        // Now reschedule it
        var rescheduleBuilder = new RescheduleAppointmentTestDataBuilder()
            .WithAppointmentId(appointmentId)
            .WithNewStartTime(DateTimeOffset.UtcNow.AddDays(10).Date.AddHours(14))
            .WithNewDuration(45)
            .WithReason("Need to change appointment time");

        var rescheduleCommand = rescheduleBuilder.Build();
        var (_, newStart, newEnd, _) = rescheduleBuilder.BuildValues();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/healthcare/appointments/{appointmentId}/reschedule", rescheduleCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<RescheduleAppointmentResult>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(appointmentId);
        result.StartUtc.Should().BeCloseTo(newStart.UtcDateTime, TimeSpan.FromSeconds(1));
        result.EndUtc.Should().BeCloseTo(newEnd.UtcDateTime, TimeSpan.FromSeconds(1));
        result.PreviousStartUtc.Should().BeCloseTo(bookResult.StartUtc, TimeSpan.FromSeconds(1));
        result.PreviousEndUtc.Should().BeCloseTo(bookResult.EndUtc, TimeSpan.FromSeconds(1));

        // Verify appointment was updated in database
        var savedAppointment = await DbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        savedAppointment.Should().NotBeNull();
        savedAppointment!.Status.Should().Be(AppointmentStatus.Rescheduled);
        savedAppointment.StartUtc.Should().BeCloseTo(newStart.UtcDateTime, TimeSpan.FromSeconds(1));
        savedAppointment.EndUtc.Should().BeCloseTo(newEnd.UtcDateTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task RescheduleAppointment_Within24Hours_Returns400WithValidationError()
    {
        // Arrange - Book appointment starting in 23 hours (within the 24-hour window)
        // The validation is "DateTime.UtcNow >= appointment.StartUtc.AddHours(-24)"
        // So an appointment starting in 23 hours cannot be rescheduled
        var nearFutureStart = DateTimeOffset.UtcNow.AddHours(23);
        var bookCommand = new BookAppointmentTestDataBuilder()
            .WithStartTime(nearFutureStart)
            .WithDuration(30)
            .Build();

        var bookResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", bookCommand);
        var bookResult = await bookResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();
        var appointmentId = bookResult!.Id;

        // Try to reschedule it
        var rescheduleCommand = new RescheduleAppointmentTestDataBuilder()
            .WithAppointmentId(appointmentId)
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/healthcare/appointments/{appointmentId}/reschedule", rescheduleCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "Appointment.RescheduleWindowClosed");
        errorMessage.Should().Contain("within 24 hours");
    }

    [Fact]
    public async Task RescheduleAppointment_WithInvalidTimeRange_Returns400WithValidationError()
    {
        // Arrange - Book an appointment first
        var bookCommand = new BookAppointmentTestDataBuilder()
            .WithStartTime(DateTimeOffset.UtcNow.AddDays(7).Date.AddHours(10))
            .Build();

        var bookResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", bookCommand);
        var bookResult = await bookResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();
        var appointmentId = bookResult!.Id;

        // Try to reschedule with invalid time range (start >= end)
        var rescheduleCommand = new RescheduleAppointmentTestDataBuilder()
            .WithAppointmentId(appointmentId)
            .WithInvalidTimeRange()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/healthcare/appointments/{appointmentId}/reschedule", rescheduleCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "NewStart").Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "NewStart");
        errorMessage.Should().Contain("before new end time");
    }

    [Fact]
    public async Task RescheduleAppointment_WithDurationLessThan10Minutes_Returns400WithValidationError()
    {
        // Arrange - Book an appointment first
        var bookCommand = new BookAppointmentTestDataBuilder()
            .WithStartTime(DateTimeOffset.UtcNow.AddDays(7).Date.AddHours(10))
            .Build();

        var bookResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", bookCommand);
        var bookResult = await bookResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();
        var appointmentId = bookResult!.Id;

        // Try to reschedule with duration < 10 minutes
        var rescheduleCommand = new RescheduleAppointmentTestDataBuilder()
            .WithAppointmentId(appointmentId)
            .WithTooShortDuration()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/healthcare/appointments/{appointmentId}/reschedule", rescheduleCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "NewEnd").Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "NewEnd");
        errorMessage.Should().Contain("at least 10 minutes");
    }

    [Fact]
    public async Task RescheduleAppointment_WithNonExistentAppointmentId_Returns404NotFound()
    {
        // Arrange
        var rescheduleCommand = new RescheduleAppointmentTestDataBuilder()
            .WithNonExistentAppointment()
            .Build();

        var appointmentId = TestSeedData.NonExistentId;

        // Act
        var response = await Client.PostAsJsonAsync($"/api/healthcare/appointments/{appointmentId}/reschedule", rescheduleCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsNotFoundError(problemDetails).Should().BeTrue();
        problemDetails!.Title.Should().Contain("Appointment");
        problemDetails.Title.Should().Contain(TestSeedData.NonExistentId.ToString());
    }

    [Fact]
    public async Task RescheduleAppointment_WithConflictingDoctorAppointment_Returns409Conflict()
    {
        // Arrange - Book two appointments for the same doctor at different times
        var firstBookCommand = new BookAppointmentTestDataBuilder()
            .WithStartTime(DateTimeOffset.UtcNow.AddDays(7).Date.AddHours(10))
            .WithDuration(30)
            .Build();

        var firstBookResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", firstBookCommand);
        var firstBookResult = await firstBookResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();
        var firstAppointmentId = firstBookResult!.Id;

        var secondBookCommand = new BookAppointmentTestDataBuilder()
            .WithPatientId(TestSeedData.SecondPatientId)
            .WithStartTime(DateTimeOffset.UtcNow.AddDays(7).Date.AddHours(14))
            .WithDuration(30)
            .Build();

        var secondBookResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", secondBookCommand);
        secondBookResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Try to reschedule the first appointment to overlap with the second
        var rescheduleCommand = new RescheduleAppointmentTestDataBuilder()
            .WithAppointmentId(firstAppointmentId)
            .WithNewStartTime(DateTimeOffset.UtcNow.AddDays(7).Date.AddHours(14).AddMinutes(15)) // Overlaps with second appointment
            .WithNewDuration(30)
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/healthcare/appointments/{firstAppointmentId}/reschedule", rescheduleCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsConflictError(problemDetails).Should().BeTrue();
        problemDetails!.Title.Should().Contain("conflicting appointment");
    }

    [Fact]
    public async Task RescheduleAppointment_CancelledAppointment_Returns400WithValidationError()
    {
        // Arrange - Book an appointment and then cancel it
        var bookCommand = new BookAppointmentTestDataBuilder()
            .WithStartTime(DateTimeOffset.UtcNow.AddDays(7).Date.AddHours(10))
            .Build();

        var bookResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", bookCommand);
        var bookResult = await bookResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();
        var appointmentId = bookResult!.Id;

        // Cancel the appointment directly in the database
        var appointment = await DbContext.Appointments.FindAsync(appointmentId);
        appointment!.Cancel("Test cancellation");
        await DbContext.SaveChangesAsync();

        // Try to reschedule the cancelled appointment
        var rescheduleCommand = new RescheduleAppointmentTestDataBuilder()
            .WithAppointmentId(appointmentId)
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/healthcare/appointments/{appointmentId}/reschedule", rescheduleCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "Appointment.CannotRescheduleCancelled");
        errorMessage.Should().Contain("Cannot reschedule a cancelled appointment");
    }

    [Fact]
    public async Task RescheduleAppointment_CompletedAppointment_Returns400WithValidationError()
    {
        // Arrange - Book an appointment and mark it as completed
        var bookCommand = new BookAppointmentTestDataBuilder()
            .WithStartTime(DateTimeOffset.UtcNow.AddDays(7).Date.AddHours(10))
            .Build();

        var bookResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", bookCommand);
        var bookResult = await bookResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();
        var appointmentId = bookResult!.Id;

        // Mark the appointment as completed directly in the database
        var appointment = await DbContext.Appointments.FindAsync(appointmentId);
        appointment!.Complete("Test completion");
        await DbContext.SaveChangesAsync();

        // Try to reschedule the completed appointment
        var rescheduleCommand = new RescheduleAppointmentTestDataBuilder()
            .WithAppointmentId(appointmentId)
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/healthcare/appointments/{appointmentId}/reschedule", rescheduleCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "Appointment.CannotRescheduleCompleted");
        errorMessage.Should().Contain("Cannot reschedule a completed appointment");
    }

    [Fact]
    public async Task RescheduleAppointment_WithDurationGreaterThan8Hours_Returns400WithValidationError()
    {
        // Arrange - Book an appointment first
        var bookCommand = new BookAppointmentTestDataBuilder()
            .WithStartTime(DateTimeOffset.UtcNow.AddDays(7).Date.AddHours(10))
            .Build();

        var bookResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", bookCommand);
        var bookResult = await bookResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();
        var appointmentId = bookResult!.Id;

        // Try to reschedule with duration > 8 hours
        var rescheduleCommand = new RescheduleAppointmentTestDataBuilder()
            .WithAppointmentId(appointmentId)
            .WithTooLongDuration()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/healthcare/appointments/{appointmentId}/reschedule", rescheduleCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "NewEnd").Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "NewEnd");
        errorMessage.Should().Contain("cannot be longer than 8 hours");
    }

    [Fact]
    public async Task RescheduleAppointment_WithReasonExceeding512Characters_Returns400WithValidationError()
    {
        // Arrange - Book an appointment first
        var bookCommand = new BookAppointmentTestDataBuilder()
            .WithStartTime(DateTimeOffset.UtcNow.AddDays(7).Date.AddHours(10))
            .Build();

        var bookResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", bookCommand);
        var bookResult = await bookResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();
        var appointmentId = bookResult!.Id;

        // Try to reschedule with reason > 512 characters
        var rescheduleCommand = new RescheduleAppointmentTestDataBuilder()
            .WithAppointmentId(appointmentId)
            .WithTooLongReason()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/healthcare/appointments/{appointmentId}/reschedule", rescheduleCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "Reason").Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "Reason");
        errorMessage.Should().Contain("cannot exceed 512 characters");
    }

    [Fact]
    public async Task RescheduleAppointment_ScheduledLessThan2HoursInAdvance_Returns400WithValidationError()
    {
        // Arrange - Book an appointment first
        var bookCommand = new BookAppointmentTestDataBuilder()
            .WithStartTime(DateTimeOffset.UtcNow.AddDays(7).Date.AddHours(10))
            .Build();

        var bookResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", bookCommand);
        var bookResult = await bookResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();
        var appointmentId = bookResult!.Id;

        // Try to reschedule to a time less than 2 hours from now
        var rescheduleCommand = new RescheduleAppointmentTestDataBuilder()
            .WithAppointmentId(appointmentId)
            .TooSoon()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/healthcare/appointments/{appointmentId}/reschedule", rescheduleCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "NewStart").Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "NewStart");
        errorMessage.Should().Contain("at least 2 hours in advance");
    }

    [Fact]
    public async Task RescheduleAppointment_WithEmptyAppointmentId_Returns400WithValidationError()
    {
        // Arrange
        var rescheduleCommand = new RescheduleAppointmentTestDataBuilder()
            .WithAppointmentId(Guid.Empty)
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/healthcare/appointments/{Guid.Empty}/reschedule", rescheduleCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "AppointmentId").Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "AppointmentId");
        errorMessage.Should().Contain("AppointmentId is required");
    }

    [Fact]
    public async Task RescheduleAppointment_WithNullReason_Returns200OK()
    {
        // Arrange - Book an appointment first
        var bookCommand = new BookAppointmentTestDataBuilder()
            .WithStartTime(DateTimeOffset.UtcNow.AddDays(7).Date.AddHours(10))
            .Build();

        var bookResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", bookCommand);
        var bookResult = await bookResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();
        var appointmentId = bookResult!.Id;

        // Reschedule with null reason (optional field)
        var rescheduleCommand = new RescheduleAppointmentTestDataBuilder()
            .WithAppointmentId(appointmentId)
            .WithReason(null)
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/healthcare/appointments/{appointmentId}/reschedule", rescheduleCommand);

        // Assert - Reason is optional, so this should succeed
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<RescheduleAppointmentResult>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(appointmentId);
    }

    [Fact]
    public async Task RescheduleAppointment_MultipleReschedules_Returns200OK()
    {
        // Arrange - Book an appointment
        var bookCommand = new BookAppointmentTestDataBuilder()
            .WithStartTime(DateTimeOffset.UtcNow.AddDays(7).Date.AddHours(10))
            .Build();

        var bookResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", bookCommand);
        var bookResult = await bookResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();
        var appointmentId = bookResult!.Id;

        // First reschedule
        var firstRescheduleCommand = new RescheduleAppointmentTestDataBuilder()
            .WithAppointmentId(appointmentId)
            .WithNewStartTime(DateTimeOffset.UtcNow.AddDays(8).Date.AddHours(11))
            .Build();

        var firstRescheduleResponse = await Client.PostAsJsonAsync($"/api/healthcare/appointments/{appointmentId}/reschedule", firstRescheduleCommand);
        firstRescheduleResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second reschedule
        var secondRescheduleCommand = new RescheduleAppointmentTestDataBuilder()
            .WithAppointmentId(appointmentId)
            .WithNewStartTime(DateTimeOffset.UtcNow.AddDays(9).Date.AddHours(15))
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/healthcare/appointments/{appointmentId}/reschedule", secondRescheduleCommand);

        // Assert - Should allow multiple reschedules
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<RescheduleAppointmentResult>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(appointmentId);

        // Verify the appointment is still in Rescheduled status
        var savedAppointment = await DbContext.Appointments.FindAsync(appointmentId);
        savedAppointment!.Status.Should().Be(AppointmentStatus.Rescheduled);
    }

    [Fact]
    public async Task RescheduleAppointment_ToSameTimeSlot_Returns200OK()
    {
        // Arrange - Book an appointment
        var startTime = DateTimeOffset.UtcNow.AddDays(7).Date.AddHours(10);
        var bookCommand = new BookAppointmentTestDataBuilder()
            .WithStartTime(startTime)
            .WithDuration(30)
            .Build();

        var bookResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", bookCommand);
        var bookResult = await bookResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();
        var appointmentId = bookResult!.Id;

        // Reschedule to the same time (should be allowed, doesn't conflict with itself)
        var rescheduleCommand = new RescheduleAppointmentTestDataBuilder()
            .WithAppointmentId(appointmentId)
            .WithNewStartTime(startTime)
            .WithNewDuration(30)
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/healthcare/appointments/{appointmentId}/reschedule", rescheduleCommand);

        // Assert - Should succeed as it doesn't conflict with itself
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<RescheduleAppointmentResult>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(appointmentId);
    }
}
