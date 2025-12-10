using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using VerticalSliceArchitecture.Application.Common.Models;
using VerticalSliceArchitecture.Application.Domain.Healthcare;
using VerticalSliceArchitecture.Application.Features.Healthcare.Appointments;
using VerticalSliceArchitecture.Application.IntegrationTests.Helpers;
using VerticalSliceArchitecture.Application.IntegrationTests.Infrastructure;
using VerticalSliceArchitecture.Application.IntegrationTests.TestData;

namespace VerticalSliceArchitecture.Application.IntegrationTests.Healthcare.Appointments;

/// <summary>
/// Integration tests for the GetDoctorAppointments endpoint.
/// Tests pagination, filtering by status and date range, sorting, and doctor validation.
/// </summary>
public class GetDoctorAppointmentsTests : IntegrationTestBase
{
    public GetDoctorAppointmentsTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetDoctorAppointments_WithValidDoctorId_Returns200WithPaginatedAppointments()
    {
        // Arrange - Book 3 appointments for the same doctor with different patients
        var builder = new BookAppointmentTestDataBuilder()
            .WithDoctorId(TestSeedData.DefaultDoctorId);

        var patientIds = new[] { TestSeedData.DefaultPatientId, TestSeedData.SecondPatientId, TestSeedData.ThirdPatientId };

        for (int i = 0; i < 3; i++)
        {
            var command = builder
                .WithPatientId(patientIds[i])
                .WithStartTime(DateTimeOffset.UtcNow.AddDays(7 + i).Date.AddHours(10))
                .Build();
            await Client.PostAsJsonAsync("/api/healthcare/appointments", command);
        }

        // Act
        var response = await Client.GetAsync($"/api/healthcare/appointments/doctor/{TestSeedData.DefaultDoctorId}");

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

        // Verify all appointments belong to the doctor
        result.Items.Should().AllSatisfy(a => a.DoctorId.Should().Be(TestSeedData.DefaultDoctorId));

        // Verify appointments are sorted by StartUtc ascending (chronological schedule)
        result.Items.Should().BeInAscendingOrder(a => a.StartUtc);
    }

    [Fact]
    public async Task GetDoctorAppointments_WithNonExistentDoctorId_Returns404NotFound()
    {
        // Arrange
        var nonExistentDoctorId = TestSeedData.NonExistentId;

        // Act
        var response = await Client.GetAsync($"/api/healthcare/appointments/doctor/{nonExistentDoctorId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsNotFoundError(problemDetails).Should().BeTrue();
        problemDetails!.Title.Should().Contain("Doctor");
        problemDetails.Title.Should().Contain(nonExistentDoctorId.ToString());
    }

    [Fact]
    public async Task GetDoctorAppointments_WithNoAppointments_Returns200WithEmptyList()
    {
        // Arrange - Doctor exists but has no appointments
        var doctorId = TestSeedData.ThirdDoctorId;

        // Act
        var response = await Client.GetAsync($"/api/healthcare/appointments/doctor/{doctorId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<AppointmentDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task GetDoctorAppointments_WithPagination_ReturnsCorrectPage()
    {
        // Arrange - Book 5 appointments with different patients
        var builder = new BookAppointmentTestDataBuilder()
            .WithDoctorId(TestSeedData.DefaultDoctorId);

        var patientIds = new[] { TestSeedData.DefaultPatientId, TestSeedData.SecondPatientId, TestSeedData.ThirdPatientId };

        for (int i = 0; i < 5; i++)
        {
            var command = builder
                .WithPatientId(patientIds[i % 3])
                .WithStartTime(DateTimeOffset.UtcNow.AddDays(7 + i).Date.AddHours(10 + i))
                .Build();
            await Client.PostAsJsonAsync("/api/healthcare/appointments", command);
        }

        // Act - Request page 2 with pageSize 2
        var response = await Client.GetAsync($"/api/healthcare/appointments/doctor/{TestSeedData.DefaultDoctorId}?pageNumber=2&pageSize=2");

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
    public async Task GetDoctorAppointments_FilterByScheduledStatus_ReturnsOnlyScheduledAppointments()
    {
        // Arrange - Create appointments with different statuses
        var builder = new BookAppointmentTestDataBuilder()
            .WithDoctorId(TestSeedData.DefaultDoctorId)
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
        var response = await Client.GetAsync($"/api/healthcare/appointments/doctor/{TestSeedData.DefaultDoctorId}?status=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<AppointmentDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(a => a.Status.Should().Be(AppointmentStatus.Scheduled));
    }

    [Fact]
    public async Task GetDoctorAppointments_FilterByCompletedStatus_ReturnsOnlyCompletedAppointments()
    {
        // Arrange - Book and complete 2 appointments
        var builder = new BookAppointmentTestDataBuilder()
            .WithDoctorId(TestSeedData.DefaultDoctorId)
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
        var response = await Client.GetAsync($"/api/healthcare/appointments/doctor/{TestSeedData.DefaultDoctorId}?status=3");

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
    public async Task GetDoctorAppointments_FilterByDateRange_ReturnsAppointmentsInRange()
    {
        // Arrange - Book appointments on different dates
        var builder = new BookAppointmentTestDataBuilder()
            .WithDoctorId(TestSeedData.DefaultDoctorId)
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
        var response = await Client.GetAsync($"/api/healthcare/appointments/doctor/{TestSeedData.DefaultDoctorId}?startDate={filterStart:yyyy-MM-dd}&endDate={filterEnd:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<AppointmentDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items[0].StartUtc.Should().BeCloseTo(date2, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetDoctorAppointments_WithInvalidPageNumber_Returns400BadRequest()
    {
        // Arrange
        var doctorId = TestSeedData.DefaultDoctorId;

        // Act - PageNumber must be >= 1
        var response = await Client.GetAsync($"/api/healthcare/appointments/doctor/{doctorId}?pageNumber=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "PageNumber").Should().BeTrue();
    }

    [Fact]
    public async Task GetDoctorAppointments_WithInvalidPageSize_Returns400BadRequest()
    {
        // Arrange
        var doctorId = TestSeedData.DefaultDoctorId;

        // Act - PageSize must be between 1 and 100
        var response = await Client.GetAsync($"/api/healthcare/appointments/doctor/{doctorId}?pageSize=150");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "PageSize").Should().BeTrue();
    }

    [Fact]
    public async Task GetDoctorAppointments_WithEndDateBeforeStartDate_Returns400BadRequest()
    {
        // Arrange
        var doctorId = TestSeedData.DefaultDoctorId;

        // Act - EndDate before StartDate
        var response = await Client.GetAsync($"/api/healthcare/appointments/doctor/{doctorId}?startDate=2025-01-20&endDate=2025-01-10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
    }

    [Fact]
    public async Task GetDoctorAppointments_WithEmptyDoctorId_Returns400BadRequest()
    {
        // Act
        var response = await Client.GetAsync($"/api/healthcare/appointments/doctor/{Guid.Empty}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "DoctorId").Should().BeTrue();
    }

    [Fact]
    public async Task GetDoctorAppointments_WithMultipleFilters_ReturnsFilteredResults()
    {
        // Arrange - Book multiple appointments with different statuses and dates
        var builder = new BookAppointmentTestDataBuilder()
            .WithDoctorId(TestSeedData.DefaultDoctorId)
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
        var response = await Client.GetAsync($"/api/healthcare/appointments/doctor/{TestSeedData.DefaultDoctorId}?status=1&startDate={filterStart:yyyy-MM-dd}&endDate={filterEnd:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<AppointmentDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items[0].Status.Should().Be(AppointmentStatus.Scheduled);
        result.Items[0].StartUtc.Should().BeCloseTo(date1, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetDoctorAppointments_SortOrder_IsAscendingByStartTime()
    {
        // Arrange - Book appointments in non-chronological order
        var builder = new BookAppointmentTestDataBuilder()
            .WithDoctorId(TestSeedData.DefaultDoctorId)
            .WithPatientId(TestSeedData.DefaultPatientId);

        var baseDate = DateTimeOffset.UtcNow.AddDays(20);
        var time3 = baseDate.AddDays(10).Date.AddHours(10);
        var time1 = baseDate.AddDays(0).Date.AddHours(10);
        var time2 = baseDate.AddDays(5).Date.AddHours(10);

        // Book in random order: 3, 1, 2
        await Client.PostAsJsonAsync("/api/healthcare/appointments", builder.WithStartTime(new DateTimeOffset(time3, TimeSpan.Zero)).Build());
        await Client.PostAsJsonAsync("/api/healthcare/appointments", builder.WithStartTime(new DateTimeOffset(time1, TimeSpan.Zero)).Build());
        await Client.PostAsJsonAsync("/api/healthcare/appointments", builder.WithStartTime(new DateTimeOffset(time2, TimeSpan.Zero)).Build());

        // Act
        var response = await Client.GetAsync($"/api/healthcare/appointments/doctor/{TestSeedData.DefaultDoctorId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<AppointmentDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);

        // Verify ascending chronological order (earliest first)
        result.Items.Should().BeInAscendingOrder(a => a.StartUtc);
        result.Items[0].StartUtc.Should().BeCloseTo(time1, TimeSpan.FromSeconds(1));
        result.Items[1].StartUtc.Should().BeCloseTo(time2, TimeSpan.FromSeconds(1));
        result.Items[2].StartUtc.Should().BeCloseTo(time3, TimeSpan.FromSeconds(1));
    }
}