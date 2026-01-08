using VerticalSliceArchitecture.Application.Domain;
using VerticalSliceArchitecture.Application.Domain.Events;

namespace VerticalSliceArchitecture.Application.UnitTests.Domain.Healthcare;

public class AppointmentTests
{
    private readonly Guid _patientId = Guid.NewGuid();
    private readonly Guid _doctorId = Guid.NewGuid();
    private readonly DateTime _startUtc = DateTime.UtcNow.AddHours(1);
    private readonly DateTime _endUtc = DateTime.UtcNow.AddHours(2);

    [Fact]
    public void Schedule_WithValidParameters_CreatesAppointment()
    {
        // Act
        var appointment = Appointment.Schedule(_patientId, _doctorId, _startUtc, _endUtc, "Test notes");

        // Assert
        appointment.Should().NotBeNull();
        appointment.PatientId.Should().Be(_patientId);
        appointment.DoctorId.Should().Be(_doctorId);
        appointment.StartUtc.Should().Be(_startUtc);
        appointment.EndUtc.Should().Be(_endUtc);
        appointment.Status.Should().Be(AppointmentStatus.Scheduled);
        appointment.Notes.Should().Be("Test notes");
    }

    [Fact]
    public void Schedule_WithNonUtcTime_ThrowsArgumentException()
    {
        // Arrange
        var localTime = DateTime.Now;

        // Act & Assert
        var act = () => Appointment.Schedule(_patientId, _doctorId, localTime, _endUtc);
        act.Should().Throw<ArgumentException>()
            .WithMessage("DateTime must be in UTC*");
    }

    [Fact]
    public void Schedule_WithStartAfterEnd_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => Appointment.Schedule(_patientId, _doctorId, _endUtc, _startUtc);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Start time must be before end time*");
    }

    [Fact]
    public void Schedule_RaisesAppointmentBookedEvent()
    {
        // Act
        var appointment = Appointment.Schedule(_patientId, _doctorId, _startUtc, _endUtc);

        // Assert
        appointment.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AppointmentBookedEvent>();
    }

    [Fact]
    public void Complete_ScheduledAppointment_SetsStatusAndTimestamp()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _startUtc, _endUtc);

        // Act
        appointment.Complete("Patient seen");

        // Assert
        appointment.Status.Should().Be(AppointmentStatus.Completed);
        appointment.CompletedUtc.Should().NotBeNull();
        appointment.Notes.Should().Be("Patient seen");
    }

    [Fact]
    public void Complete_AlreadyCompleted_IsIdempotent()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _startUtc, _endUtc);
        appointment.Complete("First");
        var firstCompletedUtc = appointment.CompletedUtc;

        // Act
        appointment.Complete("Second");

        // Assert
        appointment.CompletedUtc.Should().Be(firstCompletedUtc);
        appointment.Notes.Should().Be("First");
    }

    [Fact]
    public void Complete_CancelledAppointment_ThrowsInvalidOperationException()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _startUtc, _endUtc);
        appointment.Cancel("Cancelled");

        // Act & Assert
        var act = () => appointment.Complete("Notes");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot complete a cancelled appointment");
    }

    [Fact]
    public void Complete_NotesTooLong_ThrowsArgumentException()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _startUtc, _endUtc);

        // Act & Assert
        var act = () => appointment.Complete(new string('x', 1025));
        act.Should().Throw<ArgumentException>()
            .WithMessage("Notes cannot exceed 1024 characters*");
    }

    [Fact]
    public void Complete_RaisesAppointmentCompletedEvent()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _startUtc, _endUtc);

        // Act
        appointment.Complete("Done");

        // Assert
        appointment.DomainEvents.Should().HaveCount(2);
        appointment.DomainEvents.Last().Should().BeOfType<AppointmentCompletedEvent>();
    }

    [Fact]
    public void Cancel_ScheduledAppointment_SetsStatusAndReason()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _startUtc, _endUtc);

        // Act
        appointment.Cancel("Patient requested");

        // Assert
        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
        appointment.CancelledUtc.Should().NotBeNull();
        appointment.CancellationReason.Should().Be("Patient requested");
    }

    [Fact]
    public void Cancel_AlreadyCancelled_IsIdempotent()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _startUtc, _endUtc);
        appointment.Cancel("First");
        var firstCancelledUtc = appointment.CancelledUtc;

        // Act
        appointment.Cancel("Second");

        // Assert
        appointment.CancelledUtc.Should().Be(firstCancelledUtc);
        appointment.CancellationReason.Should().Be("First");
    }

    [Fact]
    public void Cancel_CompletedAppointment_ThrowsInvalidOperationException()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _startUtc, _endUtc);
        appointment.Complete("Done");

        // Act & Assert
        var act = () => appointment.Cancel("Too late");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot cancel a completed appointment");
    }

    [Fact]
    public void Cancel_EmptyReason_ThrowsArgumentException()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _startUtc, _endUtc);

        // Act & Assert
        var act = () => appointment.Cancel(string.Empty);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Cancellation reason is required*");
    }

    [Fact]
    public void Cancel_ReasonTooLong_ThrowsArgumentException()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _startUtc, _endUtc);

        // Act & Assert
        var act = () => appointment.Cancel(new string('x', 513));
        act.Should().Throw<ArgumentException>()
            .WithMessage("Cancellation reason cannot exceed 512 characters*");
    }
}
