# Book Appointment - Spec

Purpose

- Allow a patient to book a new appointment with a doctor.

Endpoint

- Method: POST
- Route: /api/healthcare/appointments
- Auth: Patient role (assumed). Doctor/Admin may also create on behalf of a patient.
- Idempotency: Optional Idempotency-Key header supported via validator (assumption). If provided, reuse existing booking within same (patient, doctor, start, end) window.

Request

- patientId: guid (required)
- doctorId: guid (required)
- start: string (ISO-8601, required) — client local or UTC; server normalizes to UTC
- end: string (ISO-8601, required) — must be > start
- notes: string (optional, 0..1024)

Response

- 201 Created, body: { id: guid, startUtc: string, endUtc: string }
- 409 Conflict: overlapping appointment for doctor
- 400 Validation: invalid time window, missing fields
- 404 NotFound: patient or doctor not found

Vertical slice file

```text
src/Application/Features/Healthcare/Appointments/BookAppointment.cs

- Controller: BookAppointmentController : ApiControllerBase
  - [HttpPost("/api/healthcare/appointments")] public Task<IActionResult> Book([FromBody] Command request)
- Command: record Command(Guid PatientId, Guid DoctorId, DateTimeOffset Start, DateTimeOffset End, string? Notes) : IRequest<ErrorOr<Guid>>
- Validator: AbstractValidator<Command>
  - PatientId, DoctorId > 0
  - Start < End
  - Duration >= 10 minutes and <= 8 hours (assumption)
  - Start >= UtcNow + 15 minutes (assumption)
  - Notes length <= 1024
- Handler: IRequestHandler<Command, ErrorOr<int>>
  - Normalize Start/End to UTC DateTime
  - Check existence of Patient, Doctor (DbContext.Patients/Doctors)
  - Check overlap for DoctorId where Status in { Scheduled, Rescheduled } and time ranges intersect [Start, End)
  - If conflict -> return Error.Conflict(code: "Appointment.Conflict", description)
  - Create Appointment entity
    - Status = Scheduled
    - RowVersion default
    - Add DomainEvent: AppointmentBookedEvent(AppointmentId, PatientId, DoctorId, StartUtc, EndUtc)
  - SaveChangesAsync
  - Return appointment.Id
```

Data access

- Entities: Patient, Doctor, Appointment
- Db: Appointments table should have index (DoctorId, StartUtc, EndUtc)

Domain events

- AppointmentBookedEvent
  - Handlers may send email/SMS to patient and doctor (out of scope now)

Errors

- Appointment.Conflict
- Appointment.InvalidTimeWindow
- Appointment.NotFound (patient/doctor)

Tests

- Happy path creates appointment and returns 201 with id
- Overlap produces 409
- Start >= End produces 400
- Missing doctor produces 404

Notes

- Store all times as UTC. Use AsNoTracking for read checks. Keep controller thin and delegate to handler.
