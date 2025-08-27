using VerticalSliceArchitecture.Application.Domain.Healthcare;

namespace VerticalSliceArchitecture.Application.UnitTests.Domain.Healthcare;

public class AppointmentTests
{
    private readonly Guid _patientId = Guid.NewGuid();
    private readonly Guid _doctorId = Guid.NewGuid();
    private readonly DateTime _validStartUtc = DateTime.UtcNow.AddHours(1);
    private readonly DateTime _validEndUtc = DateTime.UtcNow.AddHours(2);

    [Fact]
    public void Schedule_WithValidParameters_ShouldCreateAppointment()
    {
        // Act
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc, "Test notes");

        // Assert
        appointment.Should().NotBeNull();
        appointment.PatientId.Should().Be(_patientId);
        appointment.DoctorId.Should().Be(_doctorId);
        appointment.StartUtc.Should().Be(_validStartUtc);
        appointment.EndUtc.Should().Be(_validEndUtc);
        appointment.Status.Should().Be(AppointmentStatus.Scheduled);
        appointment.Notes.Should().Be("Test notes");

        // Note: Id is set by Entity Framework when saved to database
    }

    [Fact]
    public void Schedule_WithNullNotes_ShouldCreateAppointment()
    {
        // Act
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc, null);

        // Assert
        appointment.Should().NotBeNull();
        appointment.Notes.Should().BeNull();
    }

    [Fact]
    public void Schedule_WithNonUtcStartTime_ShouldThrowArgumentException()
    {
        // Arrange
        var nonUtcStart = DateTime.Now; // Local time

        // Act & Assert
        var act = () => Appointment.Schedule(_patientId, _doctorId, nonUtcStart, _validEndUtc);
        act.Should().Throw<ArgumentException>()
            .WithMessage("DateTime must be in UTC*")
            .And.ParamName.Should().Be("startUtc");
    }

    [Fact]
    public void Schedule_WithNonUtcEndTime_ShouldThrowArgumentException()
    {
        // Arrange
        var nonUtcEnd = DateTime.Now; // Local time

        // Act & Assert
        var act = () => Appointment.Schedule(_patientId, _doctorId, _validStartUtc, nonUtcEnd);
        act.Should().Throw<ArgumentException>()
            .WithMessage("DateTime must be in UTC*")
            .And.ParamName.Should().Be("endUtc");
    }

    [Fact]
    public void Schedule_WithStartTimeAfterEndTime_ShouldThrowArgumentException()
    {
        // Arrange
        var startAfterEnd = _validEndUtc.AddHours(1);

        // Act & Assert
        var act = () => Appointment.Schedule(_patientId, _doctorId, startAfterEnd, _validEndUtc);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Start time must be before end time*")
            .And.ParamName.Should().Be("startUtc");
    }

    [Fact]
    public void Schedule_WithStartTimeEqualToEndTime_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validStartUtc);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Start time must be before end time*")
            .And.ParamName.Should().Be("startUtc");
    }

    [Fact]
    public void Schedule_ShouldSetStatusToScheduled()
    {
        // Act
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);

        // Assert
        appointment.Status.Should().Be(AppointmentStatus.Scheduled);
    }

    [Fact]
    public void Complete_ShouldChangeStatusToCompleted()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);

        // Act
        appointment.Complete("Patient checked in and seen");

        // Assert
        appointment.Status.Should().Be(AppointmentStatus.Completed);
    }

    [Fact]
    public void Cancel_ShouldChangeStatusToCancelled()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);

        // Act
        appointment.Cancel("Patient requested cancellation");

        // Assert
        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
    }

    [Fact]
    public void DomainEvents_ShouldBeInitializedAsEmptyList()
    {
        // Act
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);

        // Assert
        appointment.DomainEvents.Should().NotBeNull();
        appointment.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void DomainEvents_ShouldAllowAddingEvents()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);
        var domainEvent = new AppointmentBookedEvent(appointment.Id, _patientId, _doctorId, _validStartUtc, _validEndUtc);

        // Act
        appointment.DomainEvents.Add(domainEvent);

        // Assert
        appointment.DomainEvents.Should().HaveCount(1);
        appointment.DomainEvents.First().Should().Be(domainEvent);
    }
}
