# Healthcare Domain - Vertical Slice Specs

This folder contains implementation-ready specs for a small yet realistic Healthcare domain to showcase Vertical Slice Architecture. Each spec maps to a single feature file (controller + command/query + validator + handler + DTOs) in `src/Application/Features/Healthcare/**` and sample HTTP requests in `requests/Healthcare/**`.

Core scenarios:

- Appointments
  - Book Appointment
  - Reschedule Appointment
- Prescriptions
  - Issue Prescription
  - Medication Refill Request

See individual specs for routes, request/response contracts, validation rules, handler flow, domain events, error codes, and tests.

Conventions

- Controllers inherit `ApiControllerBase` and use explicit absolute routes.
- Handlers use `ApplicationDbContext` directly; queries use `AsNoTracking()`.
- Results use ErrorOr: commands return `ErrorOr<T>`; queries return `ErrorOr<VM>`.
- Validation with FluentValidation; errors flow to `ApiControllerBase.Problem`.
- Domain events are raised from handlers and dispatched post-save.

Planned entity model (high-level)

- Patient { Id, FullName, Email, Phone }
- Doctor { Id, FullName, Specialty }
- Appointment { Id, PatientId, DoctorId, StartUtc, EndUtc, Status, Notes, RowVersion }
- Prescription { Id, PatientId, DoctorId, Medication, Dosage, Directions, IssuedUtc, ExpiresUtc, MaxRefills, RefillsUsed, SignedByDoctorUtc }
- RefillRequest { Id, PrescriptionId, RequestedUtc, ApprovedUtc?, DeniedUtc?, Reason?, Status }
- AuditLog { Id, OccurredUtc, ActorType, ActorId, Action, PayloadJson }

Time & timezone

- All times in requests are ISO-8601 and will be normalized to UTC. Store as UTC. Server clock assumed UTC.

Security & roles (assumption)

- Roles: Patient, Doctor, Admin. Authorization behavior can be added later; specs note expected role for each action.

Domain events (examples)

- AppointmentBookedEvent, AppointmentRescheduledEvent
- PrescriptionIssuedEvent, MedicationRefillRequestedEvent, MedicationRefillApprovedEvent, MedicationRefillDeniedEvent

Error taxonomy (examples)

- Appointment.Conflict, Appointment.NotFound, Appointment.InvalidTimeWindow, Appointment.RescheduleWindowClosed
- Prescription.NotFound, Prescription.Invalid, Prescription.MaxRefillsReached, Prescription.Expired
- RefillRequest.AlreadyProcessed, RefillRequest.Invalid

Implementation files

- Appointments
  - `src/Application/Features/Healthcare/Appointments/BookAppointment.cs`
  - `src/Application/Features/Healthcare/Appointments/RescheduleAppointment.cs`
- Prescriptions
  - `src/Application/Features/Healthcare/Prescriptions/IssuePrescription.cs`
  - `src/Application/Features/Healthcare/Prescriptions/RequestRefill.cs`
