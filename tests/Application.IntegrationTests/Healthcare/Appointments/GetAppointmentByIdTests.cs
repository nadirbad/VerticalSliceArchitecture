using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using VerticalSliceArchitecture.Application.IntegrationTests.Helpers;
using VerticalSliceArchitecture.Application.IntegrationTests.Infrastructure;
using VerticalSliceArchitecture.Application.IntegrationTests.TestData;
using VerticalSliceArchitecture.Application.Scheduling;

namespace VerticalSliceArchitecture.Application.IntegrationTests.Healthcare.Appointments;

/// <summary>
/// Integration tests for the GetAppointmentById endpoint.
/// Tests the retrieval of a single appointment by ID with full patient and doctor details.
/// </summary>
public class GetAppointmentByIdTests : IntegrationTestBase
{
    public GetAppointmentByIdTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetAppointmentById_WithValidId_Returns200WithAppointmentDetails()
    {
        // Arrange - First book an appointment
        var bookCommand = new BookAppointmentTestDataBuilder().Build();
        var bookResponse = await Client.PostAsJsonAsync("/api/appointments", bookCommand);
        var bookResult = await bookResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();

        // Act - Retrieve the appointment
        var response = await Client.GetAsync($"/api/appointments/{bookResult!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<AppointmentDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(bookResult.Id);
        result.PatientId.Should().Be(TestSeedData.DefaultPatientId);
        result.PatientFullName.Should().Be("John Smith"); // From IntegrationTestBase seed data
        result.DoctorId.Should().Be(TestSeedData.DefaultDoctorId);
        result.DoctorFullName.Should().Be("Dr. Michael Chen"); // From IntegrationTestBase seed data
        result.Status.Should().Be(Domain.AppointmentStatus.Scheduled);
        result.StartUtc.Should().BeCloseTo(bookResult.StartUtc, TimeSpan.FromSeconds(1));
        result.EndUtc.Should().BeCloseTo(bookResult.EndUtc, TimeSpan.FromSeconds(1));
        result.CompletedUtc.Should().BeNull();
        result.CancelledUtc.Should().BeNull();
        result.CancellationReason.Should().BeNull();
        result.Created.Should().NotBe(default);
    }

    [Fact]
    public async Task GetAppointmentById_WithNonExistentId_Returns404NotFound()
    {
        // Arrange
        var nonExistentId = TestSeedData.NonExistentId;

        // Act
        var response = await Client.GetAsync($"/api/appointments/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsNotFoundError(problemDetails).Should().BeTrue();
        problemDetails!.Title.Should().Contain("not found");
        problemDetails.Title.Should().Contain(nonExistentId.ToString());
    }

    [Fact]
    public async Task GetAppointmentById_WithEmptyGuid_Returns400BadRequest()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        // Act
        var response = await Client.GetAsync($"/api/appointments/{emptyGuid}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
        problemDetails.Should().NotBeNull();
        ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
        ResponseHelper.HasValidationError(problemDetails, "Id").Should().BeTrue();

        var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "Id");
        errorMessage.Should().Contain("required");
    }

    [Fact]
    public async Task GetAppointmentById_WithCompletedAppointment_Returns200WithCompletionDetails()
    {
        // Arrange - Book and complete an appointment
        var bookCommand = new BookAppointmentTestDataBuilder().Build();
        var bookResponse = await Client.PostAsJsonAsync("/api/appointments", bookCommand);
        var bookResult = await bookResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();

        var completeCommand = new CompleteAppointmentCommand(bookResult!.Id, "Patient seen and treated");
        await Client.PostAsJsonAsync($"/api/appointments/{bookResult.Id}/complete", completeCommand);

        // Act - Retrieve the completed appointment
        var response = await Client.GetAsync($"/api/appointments/{bookResult.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<AppointmentDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(bookResult.Id);
        result.Status.Should().Be(Domain.AppointmentStatus.Completed);
        result.CompletedUtc.Should().NotBeNull();
        result.CompletedUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        result.Notes.Should().Be("Patient seen and treated");
        result.CancelledUtc.Should().BeNull();
        result.CancellationReason.Should().BeNull();
    }

    [Fact]
    public async Task GetAppointmentById_WithCancelledAppointment_Returns200WithCancellationDetails()
    {
        // Arrange - Book and cancel an appointment
        var bookCommand = new BookAppointmentTestDataBuilder().Build();
        var bookResponse = await Client.PostAsJsonAsync("/api/appointments", bookCommand);
        var bookResult = await bookResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();

        var cancelCommand = new CancelAppointmentCommand(bookResult!.Id, "Patient requested cancellation");
        await Client.PostAsJsonAsync($"/api/appointments/{bookResult.Id}/cancel", cancelCommand);

        // Act - Retrieve the cancelled appointment
        var response = await Client.GetAsync($"/api/appointments/{bookResult.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<AppointmentDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(bookResult.Id);
        result.Status.Should().Be(Domain.AppointmentStatus.Cancelled);
        result.CancelledUtc.Should().NotBeNull();
        result.CancelledUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        result.CancellationReason.Should().Be("Patient requested cancellation");
        result.CompletedUtc.Should().BeNull();
    }

    [Fact]
    public async Task GetAppointmentById_IncludesPatientAndDoctorDetails()
    {
        // Arrange - Book appointment with specific patient and doctor
        var bookCommand = new BookAppointmentTestDataBuilder()
            .WithPatientId(TestSeedData.SecondPatientId)
            .WithDoctorId(TestSeedData.SecondDoctorId)
            .Build();

        var bookResponse = await Client.PostAsJsonAsync("/api/appointments", bookCommand);
        var bookResult = await bookResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();

        // Act
        var response = await Client.GetAsync($"/api/appointments/{bookResult!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<AppointmentDto>();
        result.Should().NotBeNull();
        result!.PatientId.Should().Be(TestSeedData.SecondPatientId);
        result.PatientFullName.Should().Be("Jane Doe"); // From IntegrationTestBase seed data
        result.DoctorId.Should().Be(TestSeedData.SecondDoctorId);
        result.DoctorFullName.Should().Be("Dr. Sarah Wilson"); // From IntegrationTestBase seed data (aaaaaaaa is doctor1, which is Second in TestSeedData but maps to doctor1)
    }
}