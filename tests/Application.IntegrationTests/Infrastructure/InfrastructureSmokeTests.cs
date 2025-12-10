using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using VerticalSliceArchitecture.Application.Domain.Healthcare;

namespace VerticalSliceArchitecture.Application.IntegrationTests.Infrastructure;

/// <summary>
/// Smoke tests to verify integration test infrastructure is working correctly.
/// </summary>
public class InfrastructureSmokeTests : IntegrationTestBase
{
    public InfrastructureSmokeTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public void Factory_ShouldCreateHttpClient()
    {
        // Assert
        Client.Should().NotBeNull();
        Client.BaseAddress.Should().NotBeNull();
    }

    [Fact]
    public async Task Database_ShouldBeSeededWithTestData()
    {
        // Assert - Verify patients are seeded
        var patients = await DbContext.Patients.ToListAsync();
        patients.Should().HaveCount(3);
        patients.Should().Contain(p => p.Id == Guid.Parse("11111111-1111-1111-1111-111111111111"));

        // Assert - Verify doctors are seeded
        var doctors = await DbContext.Doctors.ToListAsync();
        doctors.Should().HaveCount(3);
        doctors.Should().Contain(d => d.Id == Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    }

    [Fact]
    public async Task Database_CanAddAndRetrieveAppointment()
    {
        // Arrange
        var patient = await DbContext.Patients.FirstAsync();
        var doctor = await DbContext.Doctors.FirstAsync();

        var appointment = Appointment.Schedule(
            patient.Id,
            doctor.Id,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(1),
            "Test appointment");

        // Act
        DbContext.Appointments.Add(appointment);
        await DbContext.SaveChangesAsync();

        // Assert
        var savedAppointment = await DbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointment.Id);

        savedAppointment.Should().NotBeNull();
        savedAppointment!.PatientId.Should().Be(patient.Id);
        savedAppointment.DoctorId.Should().Be(doctor.Id);
    }
}