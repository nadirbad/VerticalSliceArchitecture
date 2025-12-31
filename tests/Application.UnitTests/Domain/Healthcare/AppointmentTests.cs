using VerticalSliceArchitecture.Application.Domain;
using VerticalSliceArchitecture.Application.Domain.Events;

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
    public void Complete_ScheduledAppointment_SetsStatusAndTimestamp()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);
        var beforeComplete = DateTime.UtcNow;

        // Act
        appointment.Complete("Patient checked in and seen");

        // Assert
        appointment.Status.Should().Be(AppointmentStatus.Completed);
        appointment.CompletedUtc.Should().NotBeNull();
        appointment.CompletedUtc.Should().BeCloseTo(beforeComplete, TimeSpan.FromSeconds(1));
        appointment.Notes.Should().Be("Patient checked in and seen");
    }

    [Fact]
    public void Complete_WithNotes_StoresNotes()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);

        // Act
        appointment.Complete("Patient arrived on time. Routine checkup completed.");

        // Assert
        appointment.Notes.Should().Be("Patient arrived on time. Routine checkup completed.");
    }

    [Fact]
    public void Complete_WithNullNotes_AllowsNull()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);

        // Act
        appointment.Complete(null);

        // Assert
        appointment.Status.Should().Be(AppointmentStatus.Completed);
        appointment.Notes.Should().BeNull();
    }

    [Fact]
    public void Complete_AlreadyCompleted_IsIdempotent()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);
        appointment.Complete("First completion");
        var firstCompletedUtc = appointment.CompletedUtc;

        // Act - complete again
        appointment.Complete("Second completion");

        // Assert - status remains completed, timestamp unchanged
        appointment.Status.Should().Be(AppointmentStatus.Completed);
        appointment.CompletedUtc.Should().Be(firstCompletedUtc);
        appointment.Notes.Should().Be("First completion"); // Notes not updated
    }

    [Fact]
    public void Complete_CancelledAppointment_ThrowsInvalidOperationException()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);
        appointment.Cancel("Patient cancelled");

        // Act & Assert
        var act = () => appointment.Complete("Trying to complete cancelled");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot complete a cancelled appointment");
    }

    [Fact]
    public void Complete_NotesExceed1024Characters_ThrowsArgumentException()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);
        var longNotes = new string('x', 1025);

        // Act & Assert
        var act = () => appointment.Complete(longNotes);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Notes cannot exceed 1024 characters*")
            .And.ParamName.Should().Be("notes");
    }

    [Fact]
    public void Cancel_ScheduledAppointment_SetsStatusTimestampAndReason()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);
        var beforeCancel = DateTime.UtcNow;

        // Act
        appointment.Cancel("Patient requested cancellation");

        // Assert
        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
        appointment.CancelledUtc.Should().NotBeNull();
        appointment.CancelledUtc.Should().BeCloseTo(beforeCancel, TimeSpan.FromSeconds(1));
        appointment.CancellationReason.Should().Be("Patient requested cancellation");
    }

    [Fact]
    public void Cancel_WithReason_StoresReason()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);

        // Act
        appointment.Cancel("Doctor unavailable");

        // Assert
        appointment.CancellationReason.Should().Be("Doctor unavailable");
    }

    [Fact]
    public void Cancel_AlreadyCancelled_IsIdempotent()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);
        appointment.Cancel("First cancellation");
        var firstCancelledUtc = appointment.CancelledUtc;
        var firstReason = appointment.CancellationReason;

        // Act - cancel again
        appointment.Cancel("Second cancellation");

        // Assert - status remains cancelled, timestamp and reason unchanged
        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
        appointment.CancelledUtc.Should().Be(firstCancelledUtc);
        appointment.CancellationReason.Should().Be(firstReason);
    }

    [Fact]
    public void Cancel_CompletedAppointment_ThrowsInvalidOperationException()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);
        appointment.Complete("Completed");

        // Act & Assert
        var act = () => appointment.Cancel("Trying to cancel completed");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot cancel a completed appointment");
    }

    [Fact]
    public void Cancel_EmptyReason_ThrowsArgumentException()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);

        // Act & Assert
        var act = () => appointment.Cancel(string.Empty);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Cancellation reason is required*")
            .And.ParamName.Should().Be("reason");
    }

    [Fact]
    public void Cancel_WhitespaceReason_ThrowsArgumentException()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);

        // Act & Assert
        var act = () => appointment.Cancel("   ");
        act.Should().Throw<ArgumentException>()
            .WithMessage("Cancellation reason is required*")
            .And.ParamName.Should().Be("reason");
    }

    [Fact]
    public void Cancel_ReasonExceed512Characters_ThrowsArgumentException()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);
        var longReason = new string('x', 513);

        // Act & Assert
        var act = () => appointment.Cancel(longReason);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Cancellation reason cannot exceed 512 characters*")
            .And.ParamName.Should().Be("reason");
    }

    [Fact]
    public void Schedule_ShouldRaiseAppointmentBookedEvent()
    {
        // Act
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);

        // Assert
        appointment.DomainEvents.Should().NotBeNull();
        appointment.DomainEvents.Should().ContainSingle();
        appointment.DomainEvents.First().Should().BeOfType<AppointmentBookedEvent>();

        var domainEvent = (AppointmentBookedEvent)appointment.DomainEvents.First();
        domainEvent.PatientId.Should().Be(_patientId);
        domainEvent.DoctorId.Should().Be(_doctorId);
        domainEvent.StartUtc.Should().Be(_validStartUtc);
        domainEvent.EndUtc.Should().Be(_validEndUtc);
    }

    [Fact]
    public void Complete_ShouldRaiseAppointmentCompletedEvent()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);

        // Act
        appointment.Complete("Completed successfully");

        // Assert - Should have 2 events: AppointmentBookedEvent from Schedule(), plus AppointmentCompletedEvent
        appointment.DomainEvents.Should().HaveCount(2);
        appointment.DomainEvents.First().Should().BeOfType<AppointmentBookedEvent>();
        appointment.DomainEvents.Last().Should().BeOfType<AppointmentCompletedEvent>();

        var completedEvent = (AppointmentCompletedEvent)appointment.DomainEvents.Last();
        completedEvent.PatientId.Should().Be(_patientId);
        completedEvent.DoctorId.Should().Be(_doctorId);
        completedEvent.Notes.Should().Be("Completed successfully");
    }
}