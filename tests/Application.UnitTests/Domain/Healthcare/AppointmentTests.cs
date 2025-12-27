using VerticalSliceArchitecture.Application.Domain;

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
    public void Complete_RescheduledAppointment_WorksCorrectly()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);
        var newStartUtc = DateTime.UtcNow.AddDays(2);
        var newEndUtc = DateTime.UtcNow.AddDays(2).AddHours(1);
        appointment.Reschedule(newStartUtc, newEndUtc);

        // Act
        appointment.Complete("Completed after reschedule");

        // Assert
        appointment.Status.Should().Be(AppointmentStatus.Completed);
        appointment.CompletedUtc.Should().NotBeNull();
        appointment.Notes.Should().Be("Completed after reschedule");
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
    public void Cancel_RescheduledAppointment_WorksCorrectly()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);
        var newStartUtc = DateTime.UtcNow.AddDays(2);
        var newEndUtc = DateTime.UtcNow.AddDays(2).AddHours(1);
        appointment.Reschedule(newStartUtc, newEndUtc);

        // Act
        appointment.Cancel("Cancelled after reschedule");

        // Assert
        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
        appointment.CancelledUtc.Should().NotBeNull();
        appointment.CancellationReason.Should().Be("Cancelled after reschedule");
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
    public void DomainEvents_ShouldAllowAddingAdditionalEvents()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);
        var additionalEvent = new AppointmentBookedEvent(appointment.Id, _patientId, _doctorId, _validStartUtc, _validEndUtc);

        // Act - Schedule already adds AppointmentBookedEvent, so we add another
        appointment.DomainEvents.Add(additionalEvent);

        // Assert - Should have 2 events: one from Schedule(), one manually added
        appointment.DomainEvents.Should().HaveCount(2);
        appointment.DomainEvents.Last().Should().Be(additionalEvent);
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
        appointment.Cancel("Cancelled");
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

        // Act - Schedule already adds AppointmentBookedEvent, then we add reschedule event
        appointment.DomainEvents.Add(domainEvent);

        // Assert - Should have 2 events: AppointmentBookedEvent from Schedule(), plus AppointmentRescheduledEvent
        appointment.DomainEvents.Should().HaveCount(2);
        appointment.DomainEvents.First().Should().BeOfType<AppointmentBookedEvent>();
        appointment.DomainEvents.Last().Should().Be(domainEvent);
    }
}