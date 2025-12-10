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

public class CompleteAppointmentTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CompleteAppointment_WithValidData_Returns200OkWithCompletedDetails()
    {
        // Arrange - Create an appointment to complete
        var bookBuilder = new BookAppointmentTestDataBuilder();
        var bookCommand = bookBuilder.Build();
        var bookResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", bookCommand);
        var bookResult = await bookResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();

        var completeCommand = new CompleteAppointmentTestDataBuilder()
            .WithAppointmentId(bookResult!.Id)
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/healthcare/appointments/{bookResult.Id}/complete", completeCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CompleteAppointmentResult>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(bookResult.Id);
        result.Status.Should().Be(AppointmentStatus.Completed);
        result.CompletedUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        result.Notes.Should().Be(completeCommand.Notes);
    }

    [Fact]
    public async Task CompleteAppointment_WithNotes_StoresNotes()
    {
        // Arrange - Create an appointment
        var bookBuilder = new BookAppointmentTestDataBuilder();
        var bookCommand = bookBuilder.Build();
        var bookResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", bookCommand);
        var bookResult = await bookResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();

        var completeCommand = new CompleteAppointmentTestDataBuilder()
            .WithAppointmentId(bookResult!.Id)
            .WithNotes("Patient arrived early. Thorough examination completed. No issues found.")
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/healthcare/appointments/{bookResult.Id}/complete", completeCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CompleteAppointmentResult>();
        result!.Notes.Should().Be("Patient arrived early. Thorough examination completed. No issues found.");

        // Verify in database
        var appointment = await DbContext.Appointments.FindAsync(bookResult.Id);
        appointment!.Notes.Should().Be("Patient arrived early. Thorough examination completed. No issues found.");
    }

    [Fact]
    public async Task CompleteAppointment_WithoutNotes_AllowsNullNotes()
    {
        // Arrange - Create an appointment
        var bookBuilder = new BookAppointmentTestDataBuilder();
        var bookCommand = bookBuilder.Build();
        var bookResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", bookCommand);
        var bookResult = await bookResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();

        var completeCommand = new CompleteAppointmentTestDataBuilder()
            .WithAppointmentId(bookResult!.Id)
            .WithNullNotes()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/healthcare/appointments/{bookResult.Id}/complete", completeCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CompleteAppointmentResult>();
        result!.Notes.Should().BeNull();
    }

    [Fact]
    public async Task CompleteAppointment_IdempotentCompletion_Returns200AndDoesNotChangeTimestamp()
    {
        // Arrange - Create and complete an appointment
        var bookBuilder = new BookAppointmentTestDataBuilder();
        var bookCommand = bookBuilder.Build();
        var bookResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", bookCommand);
        var bookResult = await bookResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();

        var firstCompleteCommand = new CompleteAppointmentTestDataBuilder()
            .WithAppointmentId(bookResult!.Id)
            .WithNotes("First completion")
            .Build();
        var firstResponse = await Client.PostAsJsonAsync($"/api/healthcare/appointments/{bookResult.Id}/complete", firstCompleteCommand);
        var firstResult = await firstResponse.Content.ReadFromJsonAsync<CompleteAppointmentResult>();

        var secondCompleteCommand = new CompleteAppointmentTestDataBuilder()
            .WithAppointmentId(bookResult.Id)
            .WithNotes("Second completion attempt")
            .Build();

        // Act - complete again
        var response = await Client.PostAsJsonAsync($"/api/healthcare/appointments/{bookResult.Id}/complete", secondCompleteCommand);

        // Assert - Should succeed but not change timestamp or notes
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CompleteAppointmentResult>();
        result!.CompletedUtc.Should().Be(firstResult!.CompletedUtc); // Timestamp unchanged
        result.Notes.Should().Be("First completion"); // Notes unchanged
    }

    [Fact]
    public async Task CompleteAppointment_NonExistentAppointment_Returns404()
    {
        // Arrange
        var command = new CompleteAppointmentTestDataBuilder()
            .WithNonExistentAppointment()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/healthcare/appointments/{command.AppointmentId}/complete", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails!.Title.Should().Contain("not found");
    }

    [Fact]
    public async Task CompleteAppointment_CancelledAppointment_Returns400WithValidationError()
    {
        // Arrange - Create and cancel an appointment
        var bookBuilder = new BookAppointmentTestDataBuilder();
        var bookCommand = bookBuilder.Build();
        var bookResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", bookCommand);
        var bookResult = await bookResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();

        // Cancel the appointment first
        var cancelCommand = new CancelAppointmentTestDataBuilder()
            .WithAppointmentId(bookResult!.Id)
            .Build();
        await Client.PostAsJsonAsync($"/api/healthcare/appointments/{bookResult.Id}/cancel", cancelCommand);

        // Try to complete the cancelled appointment
        var completeCommand = new CompleteAppointmentTestDataBuilder()
            .WithAppointmentId(bookResult.Id)
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/healthcare/appointments/{bookResult.Id}/complete", completeCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();

        // The error should indicate it's a validation error for completing cancelled appointment
        ResponseHelper.HasValidationError(problemDetails, "Appointment.CannotComplete").Should().BeTrue();
    }

    [Fact]
    public async Task CompleteAppointment_NotesTooLong_Returns400WithValidationError()
    {
        // Arrange - Create an appointment
        var bookBuilder = new BookAppointmentTestDataBuilder();
        var bookCommand = bookBuilder.Build();
        var bookResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", bookCommand);
        var bookResult = await bookResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();

        var completeCommand = new CompleteAppointmentTestDataBuilder()
            .WithAppointmentId(bookResult!.Id)
            .WithTooLongNotes()
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/healthcare/appointments/{bookResult.Id}/complete", completeCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        ResponseHelper.HasValidationError(problemDetails, "Notes").Should().BeTrue();
        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "Notes");
        errorMessage.Should().Contain("cannot exceed 1024 characters");
    }

    [Fact]
    public async Task CompleteAppointment_UpdatesDatabaseCorrectly()
    {
        // Arrange - Create an appointment
        var bookBuilder = new BookAppointmentTestDataBuilder();
        var bookCommand = bookBuilder.Build();
        var bookResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", bookCommand);
        var bookResult = await bookResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();

        var completeCommand = new CompleteAppointmentTestDataBuilder()
            .WithAppointmentId(bookResult!.Id)
            .Build();

        // Act
        await Client.PostAsJsonAsync($"/api/healthcare/appointments/{bookResult.Id}/complete", completeCommand);

        // Assert - Verify database state
        var appointment = await DbContext.Appointments.FindAsync(bookResult.Id);
        appointment.Should().NotBeNull();
        appointment!.Status.Should().Be(AppointmentStatus.Completed);
        appointment.CompletedUtc.Should().NotBeNull();
        appointment.CompletedUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        appointment.Notes.Should().Be(completeCommand.Notes);
    }

    [Fact]
    public async Task CompleteAppointment_RescheduledAppointment_CanBeCompleted()
    {
        // Arrange - Create and reschedule an appointment
        var bookBuilder = new BookAppointmentTestDataBuilder();
        var bookCommand = bookBuilder.Build();
        var bookResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", bookCommand);
        var bookResult = await bookResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();

        var rescheduleCommand = new RescheduleAppointmentTestDataBuilder()
            .WithAppointmentId(bookResult!.Id)
            .Build();
        await Client.PostAsJsonAsync($"/api/healthcare/appointments/{bookResult.Id}/reschedule", rescheduleCommand);

        var completeCommand = new CompleteAppointmentTestDataBuilder()
            .WithAppointmentId(bookResult.Id)
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/healthcare/appointments/{bookResult.Id}/complete", completeCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CompleteAppointmentResult>();
        result!.Status.Should().Be(AppointmentStatus.Completed);

        // Verify in database
        var appointment = await DbContext.Appointments.FindAsync(bookResult.Id);
        appointment!.Status.Should().Be(AppointmentStatus.Completed);
    }

    [Fact]
    public async Task CompleteAppointment_TimestampSetCorrectly()
    {
        // Arrange - Create an appointment
        var bookBuilder = new BookAppointmentTestDataBuilder();
        var bookCommand = bookBuilder.Build();
        var bookResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", bookCommand);
        var bookResult = await bookResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();

        var completeCommand = new CompleteAppointmentTestDataBuilder()
            .WithAppointmentId(bookResult!.Id)
            .Build();

        var beforeComplete = DateTime.UtcNow;

        // Act
        var response = await Client.PostAsJsonAsync($"/api/healthcare/appointments/{bookResult.Id}/complete", completeCommand);

        var afterComplete = DateTime.UtcNow;

        // Assert
        var result = await response.Content.ReadFromJsonAsync<CompleteAppointmentResult>();
        result!.CompletedUtc.Should().BeOnOrAfter(beforeComplete);
        result.CompletedUtc.Should().BeOnOrBefore(afterComplete);
    }
}