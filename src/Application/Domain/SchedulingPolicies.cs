namespace VerticalSliceArchitecture.Application.Domain;

/// <summary>
/// Business rules and constraints for appointment scheduling.
/// Single source of truth used by validators, domain objects, and tests.
/// </summary>
public static class SchedulingPolicies
{
    /// <summary>
    /// Minimum duration for any appointment (in minutes).
    /// </summary>
    public const int MinimumAppointmentDurationMinutes = 10;

    /// <summary>
    /// Maximum duration for any appointment (in hours).
    /// </summary>
    public const int MaximumAppointmentDurationHours = 8;

    /// <summary>
    /// Minimum advance notice required when booking a new appointment (in minutes).
    /// </summary>
    public const int MinimumBookingAdvanceMinutes = 15;

    /// <summary>
    /// Minimum advance notice required when rescheduling an appointment (in hours).
    /// </summary>
    public const int MinimumRescheduleAdvanceHours = 2;

    /// <summary>
    /// Window before appointment start during which rescheduling is not allowed (in hours).
    /// Appointments cannot be rescheduled within this many hours of their start time.
    /// </summary>
    public const int RescheduleWindowCutoffHours = 24;

    /// <summary>
    /// Maximum length of appointment notes.
    /// </summary>
    public const int MaxNotesLength = 1024;

    /// <summary>
    /// Maximum length of cancellation reason.
    /// </summary>
    public const int MaxCancellationReasonLength = 512;

    /// <summary>
    /// Maximum length of reschedule reason.
    /// </summary>
    public const int MaxRescheduleReasonLength = 512;
}
