# Issue Prescription - Spec

Purpose

- Doctor issues and signs a new prescription for a patient.

Endpoint

- Method: POST
- Route: /api/healthcare/prescriptions
- Auth: Doctor role required (assumed; enforced by pipeline later)

Request

- patientId: int (required)
- doctorId: int (required)
- medication: string (required, 1..256)
- dosage: string (required, 1..128) — e.g., "5 mg"
- directions: string (required, 1..1024) — e.g., "Take once daily with food"
- daysValid: int (optional, default 30) — translated to ExpiresUtc = IssuedUtc + days
- maxRefills: int (optional, default 0; 0 means no refills)

Response

- 201 Created, body: { id: int, issuedUtc: string, expiresUtc: string, refillsRemaining: int }
- 400 Validation: missing/invalid fields
- 404 NotFound: patient or doctor not found

Vertical slice file

```text
src/Application/Features/Healthcare/Prescriptions/IssuePrescription.cs

- Controller: IssuePrescriptionController : ApiControllerBase
  - [HttpPost("/api/healthcare/prescriptions")] public Task<IActionResult> Issue([FromBody] Command request)
- Command: record Command(int PatientId, int DoctorId, string Medication, string Dosage, string Directions, int? DaysValid, int? MaxRefills) : IRequest<ErrorOr<int>>
- Validator: AbstractValidator<Command>
  - PatientId, DoctorId > 0
  - Medication, Dosage, Directions not empty; with length limits
  - DaysValid null or between 1 and 180
  - MaxRefills null or between 0 and 12
- Handler
  - Verify patient and doctor exist
  - Create Prescription entity
    - IssuedUtc = UtcNow
    - ExpiresUtc = IssuedUtc + (DaysValid ?? 30)
    - MaxRefills = MaxRefills ?? 0
    - RefillsUsed = 0
    - SignedByDoctorUtc = UtcNow
  - Add DomainEvent: PrescriptionIssuedEvent(PrescriptionId)
  - Add AuditLog entry: Action = "Prescription.Issued" with payload (no PHI beyond necessary fields)
  - SaveChangesAsync
  - Return prescription.Id
```

Data access

- Entities: Patient, Doctor, Prescription, AuditLog
- Index: (PatientId, IssuedUtc)

Domain events

- PrescriptionIssuedEvent

Errors

- Prescription.Invalid
- Prescription.NotFound (patient/doctor)

Tests

- Happy path returns 201 with id
- Invalid fields return 400
