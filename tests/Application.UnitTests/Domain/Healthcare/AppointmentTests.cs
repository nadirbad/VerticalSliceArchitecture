using VerticalSliceArchitecture.Application.Domain.Healthcare;

namespace VerticalSliceArchitecture.Application.UnitTests.Domain.Healthcare;

public class AppointmentTests
{
    private static readonly DateTime _baseTimeUtc = DateTime.UtcNow;
    private readonly Guid _patientId = Guid.NewGuid();
    private readonly Guid _doctorId = Guid.NewGuid();
    private readonly DateTime _validStartUtc = _baseTimeUtc.AddHours(1);
    private readonly DateTime _validEndUtc = _baseTimeUtc.AddHours(2);

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

    [Fact]
    public void Reschedule_WithValidParameters_ShouldUpdateTimes()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);
        var newStartUtc = DateTime.UtcNow.AddDays(2);
        var newEndUtc = DateTime.UtcNow.AddDays(2).AddHours(1);

        // Act
        appointment.Reschedule(newStartUtc, newEndUtc);

        // Assert
        appointment.StartUtc.Should().Be(newStartUtc);
        appointment.EndUtc.Should().Be(newEndUtc);
        appointment.Status.Should().Be(AppointmentStatus.Rescheduled);
    }

    [Fact]
    public void Reschedule_WithReason_ShouldAppendToNotes()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc, "Initial notes");
        var newStartUtc = DateTime.UtcNow.AddDays(2);
        var newEndUtc = DateTime.UtcNow.AddDays(2).AddHours(1);

        // Act
        appointment.Reschedule(newStartUtc, newEndUtc, "Patient requested earlier time");

        // Assert
        appointment.Notes.Should().Be("Initial notes; Patient requested earlier time");
    }

    [Fact]
    public void Reschedule_WithReasonAndEmptyNotes_ShouldSetNotesToReason()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);
        var newStartUtc = DateTime.UtcNow.AddDays(2);
        var newEndUtc = DateTime.UtcNow.AddDays(2).AddHours(1);

        // Act
        appointment.Reschedule(newStartUtc, newEndUtc, "Patient requested earlier time");

        // Assert
        appointment.Notes.Should().Be("Patient requested earlier time");
    }

    [Fact]
    public void Reschedule_WithoutReason_ShouldNotChangeNotes()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc, "Initial notes");
        var newStartUtc = DateTime.UtcNow.AddDays(2);
        var newEndUtc = DateTime.UtcNow.AddDays(2).AddHours(1);

        // Act
        appointment.Reschedule(newStartUtc, newEndUtc);

        // Assert
        appointment.Notes.Should().Be("Initial notes");
    }

    [Fact]
    public void Reschedule_WhenCancelled_ShouldStillAllowMutation()
    {
        // Arrange
        // Note: Status validation moved to Handler - domain only mutates state
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);
        appointment.Cancel();
        var newStartUtc = DateTime.UtcNow.AddDays(2);
        var newEndUtc = DateTime.UtcNow.AddDays(2).AddHours(1);

        // Act
        appointment.Reschedule(newStartUtc, newEndUtc);

        // Assert
        appointment.StartUtc.Should().Be(newStartUtc);
        appointment.EndUtc.Should().Be(newEndUtc);
        appointment.Status.Should().Be(AppointmentStatus.Rescheduled);
    }

    [Fact]
    public void Reschedule_WhenCompleted_ShouldStillAllowMutation()
    {
        // Arrange
        // Note: Status validation moved to Handler - domain only mutates state
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);
        appointment.Complete();
        var newStartUtc = DateTime.UtcNow.AddDays(2);
        var newEndUtc = DateTime.UtcNow.AddDays(2).AddHours(1);

        // Act
        appointment.Reschedule(newStartUtc, newEndUtc);

        // Assert
        appointment.StartUtc.Should().Be(newStartUtc);
        appointment.EndUtc.Should().Be(newEndUtc);
        appointment.Status.Should().Be(AppointmentStatus.Rescheduled);
    }

    [Fact]
    public void Reschedule_WithNonUtcStartTime_ShouldThrowArgumentException()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);
        var nonUtcStart = DateTime.Now; // Local time
        var newEndUtc = DateTime.UtcNow.AddDays(2).AddHours(1);

        // Act & Assert
        var act = () => appointment.Reschedule(nonUtcStart, newEndUtc);
        act.Should().Throw<ArgumentException>()
            .WithMessage("DateTime must be in UTC*")
            .And.ParamName.Should().Be("newStartUtc");
    }

    [Fact]
    public void Reschedule_WithNonUtcEndTime_ShouldThrowArgumentException()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);
        var newStartUtc = DateTime.UtcNow.AddDays(2);
        var nonUtcEnd = DateTime.Now; // Local time

        // Act & Assert
        var act = () => appointment.Reschedule(newStartUtc, nonUtcEnd);
        act.Should().Throw<ArgumentException>()
            .WithMessage("DateTime must be in UTC*")
            .And.ParamName.Should().Be("newEndUtc");
    }

    [Fact]
    public void Reschedule_WithStartTimeAfterEndTime_ShouldAllowMutation()
    {
        // Arrange
        // Note: Time ordering validation moved to FluentValidation - domain only mutates state
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);
        var newStartUtc = DateTime.UtcNow.AddDays(2);
        var newEndUtc = DateTime.UtcNow.AddDays(1); // Before start (invalid, but domain doesn't check)

        // Act
        appointment.Reschedule(newStartUtc, newEndUtc);

        // Assert - domain accepts whatever it's given
        appointment.StartUtc.Should().Be(newStartUtc);
        appointment.EndUtc.Should().Be(newEndUtc);
    }

    [Fact]
    public void Reschedule_WithStartTimeEqualToEndTime_ShouldAllowMutation()
    {
        // Arrange
        // Note: Time ordering validation moved to FluentValidation - domain only mutates state
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);
        var newStartUtc = DateTime.UtcNow.AddDays(2);

        // Act
        appointment.Reschedule(newStartUtc, newStartUtc);

        // Assert - domain accepts whatever it's given
        appointment.StartUtc.Should().Be(newStartUtc);
        appointment.EndUtc.Should().Be(newStartUtc);
    }

    [Fact]
    public void AppointmentRescheduledEvent_ShouldBeCreatedWithCorrectProperties()
    {
        // Arrange
        var appointmentId = Guid.NewGuid();
        var previousStartUtc = DateTime.UtcNow.AddDays(1);
        var previousEndUtc = DateTime.UtcNow.AddDays(1).AddHours(1);
        var newStartUtc = DateTime.UtcNow.AddDays(2);
        var newEndUtc = DateTime.UtcNow.AddDays(2).AddHours(1);

        // Act
        var domainEvent = new AppointmentRescheduledEvent(appointmentId, previousStartUtc, previousEndUtc, newStartUtc, newEndUtc);

        // Assert
        domainEvent.AppointmentId.Should().Be(appointmentId);
        domainEvent.PreviousStartUtc.Should().Be(previousStartUtc);
        domainEvent.PreviousEndUtc.Should().Be(previousEndUtc);
        domainEvent.NewStartUtc.Should().Be(newStartUtc);
        domainEvent.NewEndUtc.Should().Be(newEndUtc);
        domainEvent.DateOccurred.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AppointmentRescheduledEvent_ShouldBeAddableToDomainEvents()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);
        var newStartUtc = DateTime.UtcNow.AddDays(2);
        var newEndUtc = DateTime.UtcNow.AddDays(2).AddHours(1);
        var domainEvent = new AppointmentRescheduledEvent(appointment.Id, _validStartUtc, _validEndUtc, newStartUtc, newEndUtc);

        // Act
        appointment.DomainEvents.Add(domainEvent);

        // Assert
        appointment.DomainEvents.Should().HaveCount(1);
        appointment.DomainEvents.First().Should().Be(domainEvent);
    }
}
