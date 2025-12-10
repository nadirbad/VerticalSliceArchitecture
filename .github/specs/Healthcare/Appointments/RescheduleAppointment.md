# Reschedule Appointment - Spec

Purpose

- Allow rescheduling an existing appointment to a new time window.

Endpoint

- Method: POST
- Route: /api/healthcare/appointments/{appointmentId:int}/reschedule
- Auth: Patient or Admin; Doctor can initiate as well. Appointment must belong to patient when patient initiates (assumed enforced by auth later).

Request

- appointmentId: int (route)
- newStart: string (ISO-8601, required)
- newEnd: string (ISO-8601, required)
- reason: string (optional, 0..512)
- rowVersion: string (optional, base64) — for optimistic concurrency (assumption; maps to byte[])

Response

- 200 OK, body: { id: int, startUtc: string, endUtc: string, previousStartUtc: string, previousEndUtc: string }
- 400 Validation: window invalid
- 403 Forbidden: patient attempting to reschedule someone else (future auth)
- 404 NotFound: appointment not found
- 409 Conflict: overlaps another appointment for the same doctor
- 422 UnprocessableEntity: reschedule window closed (< 24h)

Vertical slice file

```text
src/Application/Features/Healthcare/Appointments/RescheduleAppointment.cs

- Controller: RescheduleAppointmentController : ApiControllerBase
  - [HttpPost("/api/healthcare/appointments/{appointmentId:int}/reschedule")] public Task<IActionResult> Reschedule(int appointmentId, [FromBody] Command body)
- Command: record Command(int AppointmentId, DateTimeOffset NewStart, DateTimeOffset NewEnd, string? Reason, string? RowVersion) : IRequest<ErrorOr<Result>>
- Result: record Result(int Id, DateTime StartUtc, DateTime EndUtc, DateTime PreviousStartUtc, DateTime PreviousEndUtc)
- Validator: AbstractValidator<Command>
  - NewStart < NewEnd
  - Duration >= 10 minutes and <= 8 hours (assumption)
  - NewStart >= UtcNow + 2 hours (assumption; stricter than booking)
- Handler
  - Load appointment by Id (include DoctorId and current times)
  - If not found -> Error.NotFound("Appointment.NotFound")
  - Enforce 24h rule: if UtcNow >= appointment.StartUtc - 24h -> Error.Validation("Appointment.RescheduleWindowClosed") mapped to 422
  - If RowVersion provided, set entity.RowVersion to it for concurrency check
  - Compute overlap for DoctorId excluding current appointment id
  - If conflict -> Error.Conflict("Appointment.Conflict")
  - Update StartUtc/EndUtc; optionally increment a RescheduleCount (future)
  - Add DomainEvent: AppointmentRescheduledEvent(AppointmentId, OldStartUtc, OldEndUtc, NewStartUtc, NewEndUtc)
  - SaveChangesAsync
  - Return Result with old/new times
```

Data access

- Entities: Appointment
- Concurrency: Use RowVersion (byte[]) as EF Core concurrency token

Domain events

- AppointmentRescheduledEvent -> notify doctor and patient

Errors

- Appointment.NotFound
- Appointment.Conflict
- Appointment.RescheduleWindowClosed

Tests

- Happy path updates times
- Within 24h returns 422
- Overlap returns 409
- Concurrency conflict returns 409 (EF concurrency translated)
