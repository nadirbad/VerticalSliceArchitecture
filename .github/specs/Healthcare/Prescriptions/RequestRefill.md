# Medication Refill Request - Spec

Purpose

- Patient requests a refill for an existing prescription. System approves if prescription is valid and refills remain, otherwise denies.

Endpoint

- Method: POST
- Route: /api/healthcare/prescriptions/{prescriptionId:int}/refills
- Auth: Patient role (assumed). Doctor/Admin can request on behalf (future).

Request

- prescriptionId: int (route)
- patientId: int (required) — must match prescription patient

Response

- 200 OK, body: { prescriptionId: int, approved: bool, reason?: string, refillsRemaining?: int }
- 400 Validation
- 403 Forbidden: patientId mismatch
- 404 NotFound: prescription not found
- 409 Conflict: already at max refills
- 422 UnprocessableEntity: prescription expired

Vertical slice file

```text
src/Application/Features/Healthcare/Prescriptions/RequestRefill.cs

- Controller: RequestRefillController : ApiControllerBase
  - [HttpPost("/api/healthcare/prescriptions/{prescriptionId:int}/refills")] public Task<IActionResult> Request(int prescriptionId, [FromBody] Command body)
- Command: record Command(int PrescriptionId, int PatientId) : IRequest<ErrorOr<Result>>
- Result: record Result(int PrescriptionId, bool Approved, string? Reason, int? RefillsRemaining)
- Validator: AbstractValidator<Command>
  - PrescriptionId, PatientId > 0
- Handler
  - Load prescription
  - If not found -> Error.NotFound("Prescription.NotFound")
  - If PatientId mismatch -> Error.Forbidden("RefillRequest.Invalid")
  - If ExpiresUtc < UtcNow -> Error.Validation("Prescription.Expired") mapped to 422
  - If RefillsUsed >= MaxRefills -> Error.Conflict("Prescription.MaxRefillsReached")
  - Increment RefillsUsed; compute remaining = MaxRefills - RefillsUsed
  - Add DomainEvents: MedicationRefillRequestedEvent, MedicationRefillApprovedEvent
  - Add AuditLog entry: Action = "Prescription.RefillApproved"
  - SaveChangesAsync
  - Return Result { Approved = true, RefillsRemaining = remaining }
```

Alternative path (denial)

- If expired or limits reached
  - Add DomainEvents: MedicationRefillRequestedEvent, MedicationRefillDeniedEvent(reason)
  - Add AuditLog entry: Action = "Prescription.RefillDenied"
  - Return Result { Approved = false, Reason = "Expired/MaxRefillsReached" }

Data access

- Entities: Prescription, AuditLog

Errors

- Prescription.NotFound
- RefillRequest.Invalid (patient mismatch)
- Prescription.Expired
- Prescription.MaxRefillsReached

Tests

- Happy path approves and decrements remaining
- Expired -> 422
- Max refills -> 409
- Wrong patient -> 403
