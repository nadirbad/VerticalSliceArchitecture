using MediatR;

using Microsoft.Extensions.Logging;

using VerticalSliceArchitecture.Application.Common.Models;
using VerticalSliceArchitecture.Application.Domain.Events;

namespace VerticalSliceArchitecture.Application.Scheduling;

/// <summary>
/// Handles the AppointmentBookedEvent after an appointment is successfully created.
///
/// This demonstrates the domain event handler pattern in Vertical Slice Architecture.
/// Domain events are raised by entities (see Appointment.Schedule()) and dispatched
/// after SaveChangesAsync() completes (see ApplicationDbContext).
///
/// Real-world handlers might:
/// - Send confirmation emails to patients
/// - Update doctor's calendar system
/// - Publish integration events to other services
/// - Update analytics/reporting systems.
/// </summary>
internal sealed class AppointmentBookedEventHandler(ILogger<AppointmentBookedEventHandler> logger)
    : INotificationHandler<DomainEventNotification<AppointmentBookedEvent>>
{
    private readonly ILogger<AppointmentBookedEventHandler> _logger = logger;

    public Task Handle(
        DomainEventNotification<AppointmentBookedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation(
            "Appointment booked: {AppointmentId} for Patient {PatientId} with Doctor {DoctorId} at {StartUtc}",
            domainEvent.AppointmentId,
            domainEvent.PatientId,
            domainEvent.DoctorId,
            domainEvent.StartUtc);

        // In a real application, you would:
        // await _emailService.SendAppointmentConfirmationAsync(domainEvent.PatientId, domainEvent.StartUtc);
        // await _calendarService.CreateEventAsync(domainEvent.DoctorId, domainEvent.StartUtc, domainEvent.EndUtc);
        return Task.CompletedTask;
    }
}