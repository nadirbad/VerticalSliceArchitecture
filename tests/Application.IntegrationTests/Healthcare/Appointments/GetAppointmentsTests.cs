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
/// Integration tests for the GetAppointments (general query) endpoint.
/// Tests comprehensive filtering by patient, doctor, status, and date range with pagination.
/// This endpoint is designed for admin/reporting purposes with flexible filtering.
/// </summary>
public class GetAppointmentsTests : IntegrationTestBase
{
    public GetAppointmentsTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetAppointments_WithNoFilters_Returns200WithAllAppointments()
    {
        // Arrange - Book appointments for different patients and doctors
        var builder1 = new BookAppointmentTestDataBuilder()
            .WithPatientId(TestSeedData.DefaultPatientId)
            .WithDoctorId(TestSeedData.DefaultDoctorId)
            .WithStartTime(DateTimeOffset.UtcNow.AddDays(7).Date.AddHours(10));

        var builder2 = new BookAppointmentTestDataBuilder()
            .WithPatientId(TestSeedData.SecondPatientId)
            .WithDoctorId(TestSeedData.SecondDoctorId)
            .WithStartTime(DateTimeOffset.UtcNow.AddDays(8).Date.AddHours(14));

        await Client.PostAsJsonAsync("/api/healthcare/appointments", builder1.Build());
        await Client.PostAsJsonAsync("/api/healthcare/appointments", builder2.Build());

        // Act
        var response = await Client.GetAsync("/api/healthcare/appointments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<AppointmentDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCountGreaterOrEqualTo(2);
        result.TotalCount.Should().BeGreaterOrEqualTo(2);
        result.PageNumber.Should().Be(1);

        // Verify appointments are sorted by StartUtc ascending (upcoming first)
        result.Items.Should().BeInAscendingOrder(a => a.StartUtc);
    }

    [Fact]
    public async Task GetAppointments_WithNoAppointments_Returns200WithEmptyList()
    {
        // Arrange - No appointments booked (using clean database from IntegrationTestBase)

        // Act
        var response = await Client.GetAsync("/api/healthcare/appointments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<AppointmentDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task GetAppointments_FilterByPatientId_ReturnsOnlyPatientAppointments()
    {
        // Arrange - Book appointments for different patients
        var patient1Builder = new BookAppointmentTestDataBuilder()
            .WithPatientId(TestSeedData.DefaultPatientId)
            .WithStartTime(DateTimeOffset.UtcNow.AddDays(7).Date.AddHours(10));

        var patient2Builder = new BookAppointmentTestDataBuilder()
            .WithPatientId(TestSeedData.SecondPatientId)
            .WithStartTime(DateTimeOffset.UtcNow.AddDays(8).Date.AddHours(10));

        await Client.PostAsJsonAsync("/api/healthcare/appointments", patient1Builder.Build());
        await Client.PostAsJsonAsync("/api/healthcare/appointments", patient1Builder.WithStartTime(DateTimeOffset.UtcNow.AddDays(9).Date.AddHours(10)).Build());
        await Client.PostAsJsonAsync("/api/healthcare/appointments", patient2Builder.Build());

        // Act - Filter by patient1
        var response = await Client.GetAsync($"/api/healthcare/appointments?patientId={TestSeedData.DefaultPatientId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<AppointmentDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(a => a.PatientId.Should().Be(TestSeedData.DefaultPatientId));
    }

    [Fact]
    public async Task GetAppointments_FilterByDoctorId_ReturnsOnlyDoctorAppointments()
    {
        // Arrange - Book appointments for different doctors
        var doctor1Builder = new BookAppointmentTestDataBuilder()
            .WithDoctorId(TestSeedData.DefaultDoctorId)
            .WithPatientId(TestSeedData.DefaultPatientId)
            .WithStartTime(DateTimeOffset.UtcNow.AddDays(7).Date.AddHours(10));

        var doctor2Builder = new BookAppointmentTestDataBuilder()
            .WithDoctorId(TestSeedData.SecondDoctorId)
            .WithPatientId(TestSeedData.SecondPatientId)
            .WithStartTime(DateTimeOffset.UtcNow.AddDays(8).Date.AddHours(10));

        await Client.PostAsJsonAsync("/api/healthcare/appointments", doctor1Builder.Build());
        await Client.PostAsJsonAsync("/api/healthcare/appointments", doctor1Builder.WithPatientId(TestSeedData.SecondPatientId).WithStartTime(DateTimeOffset.UtcNow.AddDays(9).Date.AddHours(10)).Build());
        await Client.PostAsJsonAsync("/api/healthcare/appointments", doctor2Builder.Build());

        // Act - Filter by doctor1
        var response = await Client.GetAsync($"/api/healthcare/appointments?doctorId={TestSeedData.DefaultDoctorId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<AppointmentDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(a => a.DoctorId.Should().Be(TestSeedData.DefaultDoctorId));
    }

    [Fact]
    public async Task GetAppointments_FilterByStatus_ReturnsOnlyMatchingStatus()
    {
        // Arrange - Create appointments with different statuses
        var builder = new BookAppointmentTestDataBuilder()
            .WithPatientId(TestSeedData.DefaultPatientId)
            .WithDoctorId(TestSeedData.DefaultDoctorId);

        // Book 2 scheduled appointments
        await Client.PostAsJsonAsync("/api/healthcare/appointments", builder.WithStartTime(DateTimeOffset.UtcNow.AddDays(7).Date.AddHours(10)).Build());
        await Client.PostAsJsonAsync("/api/healthcare/appointments", builder.WithStartTime(DateTimeOffset.UtcNow.AddDays(8).Date.AddHours(10)).Build());

        // Book and complete 1 appointment
        var completeCommand = builder.WithStartTime(DateTimeOffset.UtcNow.AddDays(9).Date.AddHours(10)).Build();
        var completeResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", completeCommand);
        var completeResult = await completeResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();
        await Client.PostAsJsonAsync(
            $"/api/healthcare/appointments/{completeResult!.Id}/complete",
            new CompleteAppointmentCommand(completeResult.Id, "Completed"));

        // Act - Filter by Scheduled status
        var response = await Client.GetAsync("/api/healthcare/appointments?status=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<AppointmentDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(a => a.Status.Should().Be(AppointmentStatus.Scheduled));
    }

    [Fact]
    public async Task GetAppointments_FilterByDateRange_ReturnsAppointmentsInRange()
    {
        // Arrange - Book appointments on different dates
        var builder = new BookAppointmentTestDataBuilder()
            .WithPatientId(TestSeedData.DefaultPatientId)
            .WithDoctorId(TestSeedData.DefaultDoctorId);

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
        var response = await Client.GetAsync($"/api/healthcare/appointments?startDate={filterStart:yyyy-MM-dd}&endDate={filterEnd:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<AppointmentDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items[0].StartUtc.Should().BeCloseTo(date2, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetAppointments_WithMultipleFilters_ReturnsFilteredResults()
    {
        // Arrange - Complex scenario with multiple appointments
        var baseDate = DateTimeOffset.UtcNow.AddDays(20);

        // Patient1 + Doctor1 + Scheduled + date1
        var command1 = new BookAppointmentTestDataBuilder()
            .WithPatientId(TestSeedData.DefaultPatientId)
            .WithDoctorId(TestSeedData.DefaultDoctorId)
            .WithStartTime(new DateTimeOffset(baseDate.Date.AddHours(10), TimeSpan.Zero))
            .Build();
        await Client.PostAsJsonAsync("/api/healthcare/appointments", command1);

        // Patient1 + Doctor1 + Scheduled + date2 (outside date range)
        var command2 = new BookAppointmentTestDataBuilder()
            .WithPatientId(TestSeedData.DefaultPatientId)
            .WithDoctorId(TestSeedData.DefaultDoctorId)
            .WithStartTime(new DateTimeOffset(baseDate.AddDays(10).Date.AddHours(10), TimeSpan.Zero))
            .Build();
        await Client.PostAsJsonAsync("/api/healthcare/appointments", command2);

        // Patient2 + Doctor1 + Scheduled + date1 (different patient)
        var command3 = new BookAppointmentTestDataBuilder()
            .WithPatientId(TestSeedData.SecondPatientId)
            .WithDoctorId(TestSeedData.DefaultDoctorId)
            .WithStartTime(new DateTimeOffset(baseDate.Date.AddHours(14), TimeSpan.Zero))
            .Build();
        await Client.PostAsJsonAsync("/api/healthcare/appointments", command3);

        // Patient1 + Doctor1 + Completed + date1 (wrong status)
        var command4 = new BookAppointmentTestDataBuilder()
            .WithPatientId(TestSeedData.DefaultPatientId)
            .WithDoctorId(TestSeedData.DefaultDoctorId)
            .WithStartTime(new DateTimeOffset(baseDate.Date.AddHours(16), TimeSpan.Zero))
            .Build();
        var response4 = await Client.PostAsJsonAsync("/api/healthcare/appointments", command4);
        var result4 = await response4.Content.ReadFromJsonAsync<BookAppointmentResult>();
        await Client.PostAsJsonAsync(
            $"/api/healthcare/appointments/{result4!.Id}/complete",
            new CompleteAppointmentCommand(result4.Id, "Done"));

        // Act - Filter by Patient1 + Doctor1 + Scheduled + date range (should return only command1)
        var filterStart = baseDate.Date;
        var filterEnd = baseDate.AddDays(5).Date;
        var response = await Client.GetAsync(
            $"/api/healthcare/appointments?patientId={TestSeedData.DefaultPatientId}&doctorId={TestSeedData.DefaultDoctorId}&status=1&startDate={filterStart:yyyy-MM-dd}&endDate={filterEnd:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<AppointmentDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items[0].PatientId.Should().Be(TestSeedData.DefaultPatientId);
        result.Items[0].DoctorId.Should().Be(TestSeedData.DefaultDoctorId);
        result.Items[0].Status.Should().Be(AppointmentStatus.Scheduled);
        result.Items[0].StartUtc.Should().BeCloseTo(baseDate.Date.AddHours(10), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetAppointments_WithPagination_ReturnsCorrectPage()
    {
        // Arrange - Book 5 appointments
        var builder = new BookAppointmentTestDataBuilder()
            .WithPatientId(TestSeedData.DefaultPatientId)
            .WithDoctorId(TestSeedData.DefaultDoctorId);

        for (int i = 0; i < 5; i++)
        {
            var command = builder
                .WithStartTime(DateTimeOffset.UtcNow.AddDays(7 + i).Date.AddHours(10))
                .Build();
            await Client.PostAsJsonAsync("/api/healthcare/appointments", command);
        }

        // Act - Request page 2 with pageSize 2
        var response = await Client.GetAsync("/api/healthcare/appointments?pageNumber=2&pageSize=2");

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
    public async Task GetAppointments_WithInvalidPageNumber_Returns400BadRequest()
    {
        // Act - PageNumber must be >= 1
        var response = await Client.GetAsync("/api/healthcare/appointments?pageNumber=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "PageNumber").Should().BeTrue();
    }

    [Fact]
    public async Task GetAppointments_WithInvalidPageSize_Returns400BadRequest()
    {
        // Act - PageSize must be between 1 and 100
        var response = await Client.GetAsync("/api/healthcare/appointments?pageSize=150");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "PageSize").Should().BeTrue();
    }

    [Fact]
    public async Task GetAppointments_WithEndDateBeforeStartDate_Returns400BadRequest()
    {
        // Act - EndDate before StartDate
        var response = await Client.GetAsync("/api/healthcare/appointments?startDate=2025-01-20&endDate=2025-01-10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
    }

    [Fact]
    public async Task GetAppointments_FilterByNonExistentPatient_Returns200WithEmptyList()
    {
        // Arrange - Book some appointments
        var builder = new BookAppointmentTestDataBuilder()
            .WithPatientId(TestSeedData.DefaultPatientId)
            .WithStartTime(DateTimeOffset.UtcNow.AddDays(7).Date.AddHours(10));

        await Client.PostAsJsonAsync("/api/healthcare/appointments", builder.Build());

        // Act - Filter by non-existent patient
        var response = await Client.GetAsync($"/api/healthcare/appointments?patientId={TestSeedData.NonExistentId}");

        // Assert - Returns empty list, not 404 (general query doesn't validate patient/doctor existence)
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<AppointmentDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAppointments_FilterByNonExistentDoctor_Returns200WithEmptyList()
    {
        // Arrange - Book some appointments
        var builder = new BookAppointmentTestDataBuilder()
            .WithDoctorId(TestSeedData.DefaultDoctorId)
            .WithStartTime(DateTimeOffset.UtcNow.AddDays(7).Date.AddHours(10));

        await Client.PostAsJsonAsync("/api/healthcare/appointments", builder.Build());

        // Act - Filter by non-existent doctor
        var response = await Client.GetAsync($"/api/healthcare/appointments?doctorId={TestSeedData.NonExistentId}");

        // Assert - Returns empty list, not 404 (general query doesn't validate patient/doctor existence)
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<AppointmentDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAppointments_SortOrder_IsAscendingByStartTime()
    {
        // Arrange - Book appointments in non-chronological order
        var builder = new BookAppointmentTestDataBuilder()
            .WithPatientId(TestSeedData.DefaultPatientId)
            .WithDoctorId(TestSeedData.DefaultDoctorId);

        var baseDate = DateTimeOffset.UtcNow.AddDays(20);
        var time3 = baseDate.AddDays(10).Date.AddHours(10);
        var time1 = baseDate.AddDays(0).Date.AddHours(10);
        var time2 = baseDate.AddDays(5).Date.AddHours(10);

        // Book in random order: 3, 1, 2
        await Client.PostAsJsonAsync("/api/healthcare/appointments", builder.WithStartTime(new DateTimeOffset(time3, TimeSpan.Zero)).Build());
        await Client.PostAsJsonAsync("/api/healthcare/appointments", builder.WithStartTime(new DateTimeOffset(time1, TimeSpan.Zero)).Build());
        await Client.PostAsJsonAsync("/api/healthcare/appointments", builder.WithStartTime(new DateTimeOffset(time2, TimeSpan.Zero)).Build());

        // Act
        var response = await Client.GetAsync("/api/healthcare/appointments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<AppointmentDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);

        // Verify ascending order (upcoming first)
        result.Items.Should().BeInAscendingOrder(a => a.StartUtc);
        result.Items[0].StartUtc.Should().BeCloseTo(time1, TimeSpan.FromSeconds(1));
        result.Items[1].StartUtc.Should().BeCloseTo(time2, TimeSpan.FromSeconds(1));
        result.Items[2].StartUtc.Should().BeCloseTo(time3, TimeSpan.FromSeconds(1));
    }
}