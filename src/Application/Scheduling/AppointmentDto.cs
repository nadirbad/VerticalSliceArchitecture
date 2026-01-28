using VerticalSliceArchitecture.Application.Domain;

namespace VerticalSliceArchitecture.Application.Scheduling;

/// <summary>
/// Shared DTO representing an appointment with related patient and doctor information.
/// </summary>
public record AppointmentDto(
    Guid Id,
    Guid PatientId,
    string PatientFullName,
    Guid DoctorId,
    string DoctorFullName,
    string DoctorSpecialty,
    DateTime StartUtc,
    DateTime EndUtc,
    AppointmentStatus Status,
    string? Notes,
    DateTime? CompletedUtc,
    DateTime? CancelledUtc,
    string? CancellationReason,
    DateTime Created,
    DateTime? LastModified);