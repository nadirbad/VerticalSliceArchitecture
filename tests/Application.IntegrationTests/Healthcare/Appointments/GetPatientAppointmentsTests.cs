using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using VerticalSliceArchitecture.Application.Common.Models;
using VerticalSliceArchitecture.Application.Domain;
using VerticalSliceArchitecture.Application.IntegrationTests.Helpers;
using VerticalSliceArchitecture.Application.IntegrationTests.Infrastructure;
using VerticalSliceArchitecture.Application.IntegrationTests.TestData;
using VerticalSliceArchitecture.Application.Scheduling;

namespace VerticalSliceArchitecture.Application.IntegrationTests.Healthcare.Appointments;

/// <summary>
/// Integration tests for the GetPatientAppointments endpoint.
/// Tests pagination, filtering by status and date range, and patient validation.
/// </summary>
public class GetPatientAppointmentsTests : IntegrationTestBase
{
    public GetPatientAppointmentsTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetPatientAppointments_WithValidPatientId_Returns200WithPaginatedAppointments()
    {
        // Arrange - Book 3 appointments for the same patient
        var builder = new BookAppointmentTestDataBuilder()
            .WithPatientId(TestSeedData.DefaultPatientId);

        for (int i = 0; i < 3; i++)
        {
            var command = builder
                .WithStartTime(DateTimeOffset.UtcNow.AddDays(7 + i).Date.AddHours(10))
                .Build();
            await Client.PostAsJsonAsync("/api/healthcare/appointments", command);
        }

        // Act
        var response = await Client.GetAsync($"/api/healthcare/appointments/patient/{TestSeedData.DefaultPatientId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<AppointmentDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.PageNumber.Should().Be(1);
        result.TotalPages.Should().Be(1);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeFalse();

        // Verify all appointments belong to the patient
        result.Items.Should().AllSatisfy(a => a.PatientId.Should().Be(TestSeedData.DefaultPatientId));

        // Verify appointments are sorted by StartUtc descending (most recent first)
        result.Items.Should().BeInDescendingOrder(a => a.StartUtc);
    }

    [Fact]
    public async Task GetPatientAppointments_WithNonExistentPatientId_Returns404NotFound()
    {
        // Arrange
        var nonExistentPatientId = TestSeedData.NonExistentId;

        // Act
        var response = await Client.GetAsync($"/api/healthcare/appointments/patient/{nonExistentPatientId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsNotFoundError(problemDetails).Should().BeTrue();
        problemDetails!.Title.Should().Contain("Patient");
        problemDetails.Title.Should().Contain(nonExistentPatientId.ToString());
    }

    [Fact]
    public async Task GetPatientAppointments_WithNoAppointments_Returns200WithEmptyList()
    {
        // Arrange - Patient exists but has no appointments
        var patientId = TestSeedData.ThirdPatientId;

        // Act
        var response = await Client.GetAsync($"/api/healthcare/appointments/patient/{patientId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<AppointmentDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task GetPatientAppointments_WithPagination_ReturnsCorrectPage()
    {
        // Arrange - Book 5 appointments
        var builder = new BookAppointmentTestDataBuilder()
            .WithPatientId(TestSeedData.DefaultPatientId);

        for (int i = 0; i < 5; i++)
        {
            var command = builder
                .WithStartTime(DateTimeOffset.UtcNow.AddDays(7 + i).Date.AddHours(10))
                .Build();
            await Client.PostAsJsonAsync("/api/healthcare/appointments", command);
        }

        // Act - Request page 2 with pageSize 2
        var response = await Client.GetAsync($"/api/healthcare/appointments/patient/{TestSeedData.DefaultPatientId}?pageNumber=2&pageSize=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<AppointmentDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.PageNumber.Should().Be(2);
        result.TotalPages.Should().Be(3);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task GetPatientAppointments_FilterByScheduledStatus_ReturnsOnlyScheduledAppointments()
    {
        // Arrange - Create appointments with different statuses
        var builder = new BookAppointmentTestDataBuilder()
            .WithPatientId(TestSeedData.DefaultPatientId);

        // Book 2 scheduled appointments
        for (int i = 0; i < 2; i++)
        {
            var command = builder
                .WithStartTime(DateTimeOffset.UtcNow.AddDays(7 + i).Date.AddHours(10))
                .Build();
            await Client.PostAsJsonAsync("/api/healthcare/appointments", command);
        }

        // Book and complete 1 appointment
        var completeCommand = builder
            .WithStartTime(DateTimeOffset.UtcNow.AddDays(9).Date.AddHours(10))
            .Build();
        var completeResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", completeCommand);
        var completeResult = await completeResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();
        await Client.PostAsJsonAsync(
            $"/api/healthcare/appointments/{completeResult!.Id}/complete",
            new CompleteAppointmentCommand(completeResult.Id, "Completed"));

        // Act - Filter by Scheduled status
        var response = await Client.GetAsync($"/api/healthcare/appointments/patient/{TestSeedData.DefaultPatientId}?status=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<AppointmentDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(a => a.Status.Should().Be(AppointmentStatus.Scheduled));
    }

    [Fact]
    public async Task GetPatientAppointments_FilterByCompletedStatus_ReturnsOnlyCompletedAppointments()
    {
        // Arrange - Book and complete 2 appointments
        var builder = new BookAppointmentTestDataBuilder()
            .WithPatientId(TestSeedData.DefaultPatientId);

        for (int i = 0; i < 2; i++)
        {
            var command = builder
                .WithStartTime(DateTimeOffset.UtcNow.AddDays(7 + i).Date.AddHours(10))
                .Build();
            var bookResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", command);
            var bookResult = await bookResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();
            await Client.PostAsJsonAsync(
                $"/api/healthcare/appointments/{bookResult!.Id}/complete",
                new CompleteAppointmentCommand(bookResult.Id, "Done"));
        }

        // Act - Filter by Completed status (3)
        var response = await Client.GetAsync($"/api/healthcare/appointments/patient/{TestSeedData.DefaultPatientId}?status=3");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<AppointmentDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(a =>
        {
            a.Status.Should().Be(AppointmentStatus.Completed);
            a.CompletedUtc.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task GetPatientAppointments_FilterByDateRange_ReturnsAppointmentsInRange()
    {
        // Arrange - Book appointments on different dates
        var builder = new BookAppointmentTestDataBuilder()
            .WithPatientId(TestSeedData.DefaultPatientId);

        var baseDate = DateTimeOffset.UtcNow.AddDays(20);
        var date1 = baseDate.AddDays(0).Date.AddHours(10);
        var date2 = baseDate.AddDays(5).Date.AddHours(10);
        var date3 = baseDate.AddDays(10).Date.AddHours(10);

        await Client.PostAsJsonAsync("/api/healthcare/appointments", builder.WithStartTime(new DateTimeOffset(date1, TimeSpan.Zero)).Build());
        await Client.PostAsJsonAsync("/api/healthcare/appointments", builder.WithStartTime(new DateTimeOffset(date2, TimeSpan.Zero)).Build());
        await Client.PostAsJsonAsync("/api/healthcare/appointments", builder.WithStartTime(new DateTimeOffset(date3, TimeSpan.Zero)).Build());

        // Act - Filter appointments in the middle date range (date2 should be included, date1 and date3 excluded)
        var filterStart = baseDate.AddDays(3).Date;
        var filterEnd = baseDate.AddDays(7).Date;
        var response = await Client.GetAsync($"/api/healthcare/appointments/patient/{TestSeedData.DefaultPatientId}?startDate={filterStart:yyyy-MM-dd}&endDate={filterEnd:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<AppointmentDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items[0].StartUtc.Should().BeCloseTo(date2, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetPatientAppointments_WithInvalidPageNumber_Returns400BadRequest()
    {
        // Arrange
        var patientId = TestSeedData.DefaultPatientId;

        // Act - PageNumber must be >= 1
        var response = await Client.GetAsync($"/api/healthcare/appointments/patient/{patientId}?pageNumber=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "PageNumber").Should().BeTrue();
    }

    [Fact]
    public async Task GetPatientAppointments_WithInvalidPageSize_Returns400BadRequest()
    {
        // Arrange
        var patientId = TestSeedData.DefaultPatientId;

        // Act - PageSize must be between 1 and 100
        var response = await Client.GetAsync($"/api/healthcare/appointments/patient/{patientId}?pageSize=150");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "PageSize").Should().BeTrue();
    }

    [Fact]
    public async Task GetPatientAppointments_WithEndDateBeforeStartDate_Returns400BadRequest()
    {
        // Arrange
        var patientId = TestSeedData.DefaultPatientId;

        // Act - EndDate before StartDate
        var response = await Client.GetAsync($"/api/healthcare/appointments/patient/{patientId}?startDate=2025-01-20&endDate=2025-01-10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
    }

    [Fact]
    public async Task GetPatientAppointments_WithEmptyPatientId_Returns400BadRequest()
    {
        // Act
        var response = await Client.GetAsync($"/api/healthcare/appointments/patient/{Guid.Empty}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "PatientId").Should().BeTrue();
    }

    [Fact]
    public async Task GetPatientAppointments_WithMultipleFilters_ReturnsFilteredResults()
    {
        // Arrange - Book multiple appointments with different statuses and dates
        var builder = new BookAppointmentTestDataBuilder()
            .WithPatientId(TestSeedData.DefaultPatientId);

        var baseDate = DateTimeOffset.UtcNow.AddDays(20);
        var date1 = baseDate.Date.AddHours(10);
        var date2 = baseDate.AddDays(5).Date.AddHours(10);

        // Scheduled appointment on date1
        await Client.PostAsJsonAsync("/api/healthcare/appointments", builder.WithStartTime(new DateTimeOffset(date1, TimeSpan.Zero)).Build());

        // Scheduled appointment on date2
        await Client.PostAsJsonAsync("/api/healthcare/appointments", builder.WithStartTime(new DateTimeOffset(date2, TimeSpan.Zero)).Build());

        // Completed appointment on date1
        var completeCommand = builder.WithStartTime(new DateTimeOffset(date1.AddHours(2), TimeSpan.Zero)).Build();
        var completeResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", completeCommand);
        var completeResult = await completeResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();
        await Client.PostAsJsonAsync(
            $"/api/healthcare/appointments/{completeResult!.Id}/complete",
            new CompleteAppointmentCommand(completeResult.Id, "Done"));

        // Act - Filter by Scheduled status and date1's date
        var filterStart = date1.Date;
        var filterEnd = date1.Date.AddDays(1);
        var response = await Client.GetAsync($"/api/healthcare/appointments/patient/{TestSeedData.DefaultPatientId}?status=1&startDate={filterStart:yyyy-MM-dd}&endDate={filterEnd:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<AppointmentDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items[0].Status.Should().Be(AppointmentStatus.Scheduled);
        result.Items[0].StartUtc.Should().BeCloseTo(date1, TimeSpan.FromSeconds(1));
    }
}