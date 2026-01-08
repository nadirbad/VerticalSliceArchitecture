using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using VerticalSliceArchitecture.Application.Scheduling;

namespace VerticalSliceArchitecture.Application.IntegrationTests;

public class AppointmentApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AppointmentApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAppointments_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/appointments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task BookAppointment_WithValidData_ReturnsCreated()
    {
        // Arrange
        var command = new BookAppointment.Command(
            PatientId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            DoctorId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Start: DateTimeOffset.UtcNow.AddDays(1),
            End: DateTimeOffset.UtcNow.AddDays(1).AddMinutes(30),
            Notes: "Checkup");

        // Act
        var response = await _client.PostAsJsonAsync("/api/appointments", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task BookAppointment_WithInvalidPatient_ReturnsNotFound()
    {
        // Arrange
        var command = new BookAppointment.Command(
            PatientId: Guid.NewGuid(),
            DoctorId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Start: DateTimeOffset.UtcNow.AddDays(1),
            End: DateTimeOffset.UtcNow.AddDays(1).AddMinutes(30),
            Notes: null);

        // Act
        var response = await _client.PostAsJsonAsync("/api/appointments", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task BookAppointment_WithEmptyPatientId_ReturnsBadRequest()
    {
        // Arrange
        var command = new BookAppointment.Command(
            PatientId: Guid.Empty,
            DoctorId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Start: DateTimeOffset.UtcNow.AddDays(1),
            End: DateTimeOffset.UtcNow.AddDays(1).AddMinutes(30),
            Notes: null);

        // Act
        var response = await _client.PostAsJsonAsync("/api/appointments", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}