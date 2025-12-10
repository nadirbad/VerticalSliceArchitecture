# Technical Specification

This is the technical specification for the spec detailed in [@.agent-os/specs/2025-10-01-prescription-issuance/spec.md](../.agent-os/specs/2025-10-01-prescription-issuance/spec.md)

## Technical Requirements

### Domain Model

**Prescription Entity:**
- Properties with private setters: `Id`, `PatientId`, `DoctorId`, `MedicationName`, `Dosage`, `Directions`, `Quantity`, `NumberOfRefills`, `RemainingRefills`, `IssuedDateUtc`, `ExpirationDateUtc`, `Status`
- Factory method: `Prescription.Issue(patientId, doctorId, medicationName, dosage, directions, quantity, numberOfRefills, durationInDays)` that enforces all business rules
- Read-only property: `IsExpired` computed from `ExpirationDateUtc < DateTime.UtcNow`
- Read-only property: `IsDepleted` computed from `RemainingRefills <= 0`
- Status enum: `Active`, `Expired`, `Depleted`
- Immutable after creation (no Update methods in MVP)
- Raises domain event: `PrescriptionIssuedEvent` added to `DomainEvents` collection

**Business Rules (enforced in factory method):**
- MedicationName: required, 1-200 characters
- Dosage: required, 1-50 characters
- Directions: required, 1-500 characters
- Quantity: required, range 1-999
- NumberOfRefills: required, range 0-12
- DurationInDays: required, range 1-365
- IssuedDateUtc: automatically set to `DateTime.UtcNow`
- ExpirationDateUtc: calculated as `IssuedDateUtc.AddDays(durationInDays)`
- RemainingRefills: initialized to `NumberOfRefills`
- PatientId and DoctorId: required, must reference existing entities

**Domain Event:**
```csharp
public record PrescriptionIssuedEvent(
    int PrescriptionId,
    int PatientId,
    int DoctorId,
    string MedicationName,
    string Dosage,
    DateTime IssuedDateUtc,
    DateTime ExpirationDateUtc
) : IDomainEvent;
```

### API Endpoint

**Minimal API Endpoint:** POST `/api/healthcare/prescriptions`

**Request Body:**
```json
{
  "patientId": 1,
  "doctorId": 2,
  "medicationName": "Amoxicillin",
  "dosage": "500mg",
  "directions": "Take one capsule three times daily with food for 10 days",
  "quantity": 30,
  "numberOfRefills": 2,
  "durationInDays": 90
}
```

**Success Response (201 Created):**
```json
{
  "prescriptionId": 5,
  "patientId": 1,
  "doctorId": 2,
  "medicationName": "Amoxicillin",
  "dosage": "500mg",
  "directions": "Take one capsule three times daily with food for 10 days",
  "quantity": 30,
  "numberOfRefills": 2,
  "remainingRefills": 2,
  "issuedDateUtc": "2025-10-01T14:30:00Z",
  "expirationDateUtc": "2025-12-30T14:30:00Z",
  "status": "Active"
}
```

**Error Responses:**
- 400 Bad Request: Invalid request format
- 404 Not Found: Patient or Doctor not found
- 422 Unprocessable Entity: Validation errors (with detailed error messages)

### MediatR Command and Handler

**Command:**
```csharp
public record IssuePrescriptionCommand(
    int PatientId,
    int DoctorId,
    string MedicationName,
    string Dosage,
    string Directions,
    int Quantity,
    int NumberOfRefills,
    int DurationInDays
) : IRequest<ErrorOr<PrescriptionResponse>>;
```

**Validator:**
```csharp
internal sealed class IssuePrescriptionCommandValidator : AbstractValidator<IssuePrescriptionCommand>
{
    // Validate all fields match business rules
    // Ensure PatientId and DoctorId are positive integers
    // Validate string lengths, numeric ranges
}
```

**Handler Logic:**
1. Validate Patient exists in database
2. Validate Doctor exists in database
3. Call `Prescription.Issue()` factory method (business rules enforced here)
4. Add prescription to DbContext
5. SaveChangesAsync (triggers domain event dispatch)
6. Map to PrescriptionResponse
7. Return ErrorOr<PrescriptionResponse>

### Database Schema

**Table:** Prescriptions

| Column | Type | Constraints |
|--------|------|-------------|
| Id | int | Primary Key, Identity |
| PatientId | int | Foreign Key to Patients, NOT NULL |
| DoctorId | int | Foreign Key to Doctors, NOT NULL |
| MedicationName | nvarchar(200) | NOT NULL |
| Dosage | nvarchar(50) | NOT NULL |
| Directions | nvarchar(500) | NOT NULL |
| Quantity | int | NOT NULL |
| NumberOfRefills | int | NOT NULL |
| RemainingRefills | int | NOT NULL |
| IssuedDateUtc | datetime2 | NOT NULL |
| ExpirationDateUtc | datetime2 | NOT NULL |
| Status | nvarchar(50) | NOT NULL |
| Created | datetime2 | NOT NULL |
| CreatedBy | nvarchar(max) | NULL |
| LastModified | datetime2 | NULL |
| LastModifiedBy | nvarchar(max) | NULL |

**Indexes:**
- Non-clustered index on PatientId (for querying patient's prescriptions)
- Non-clustered index on DoctorId (for querying doctor's issued prescriptions)
- Non-clustered index on ExpirationDateUtc (for finding expired prescriptions)

**Navigation Properties:**
- Prescription → Patient (many-to-one)
- Prescription → Doctor (many-to-one)

### File Organization (Vertical Slice)

**File:** `src/Application/Features/Healthcare/IssuePrescription.cs`

Contains:
- Minimal API endpoint definition
- `IssuePrescriptionCommand` record
- `IssuePrescriptionCommandValidator` class
- `IssuePrescriptionCommandHandler` class
- `PrescriptionResponse` record
- Mapping extension method

**File:** `src/Application/Features/Healthcare/Entities/Prescription.cs`

Contains:
- `Prescription` entity class
- `PrescriptionStatus` enum
- Business rule enforcement
- Factory method

**File:** `src/Application/Features/Healthcare/Events/PrescriptionIssuedEvent.cs`

Contains:
- `PrescriptionIssuedEvent` record
- Optional: `PrescriptionIssuedEventHandler` (if immediate actions needed)

### Validation Strategy

- **FluentValidation** for command input validation (string lengths, numeric ranges, required fields)
- **Domain model** for business rule enforcement (logical constraints, calculated values)
- **Database constraints** for data integrity (foreign keys, NOT NULL)
- **Handler** for entity existence validation (Patient and Doctor must exist)

### Error Handling

Use `ErrorOr<T>` pattern:
- `Error.NotFound()` if Patient or Doctor not found
- `Error.Validation()` for validation failures
- `Error.Failure()` for unexpected errors
- Return meaningful error messages with field-specific details

### Testing Strategy

**Unit Tests:**
- Prescription factory method with valid inputs
- Business rule enforcement (each validation rule)
- Expiration date calculation
- Status determination (Active, Expired, Depleted)
- Validator rules
- Handler logic with mocked DbContext

**Integration Tests:**
- Happy path: Issue prescription and verify database persistence
- Patient not found scenario
- Doctor not found scenario
- Invalid medication name (too long, empty)
- Invalid quantity (0, negative, > 999)
- Invalid refills (-1, > 12)
- Invalid duration (0, negative, > 365)
- Domain event verification

**HTTP Request File:**
`requests/healthcare/issue-prescription.http` with examples:
- Valid prescription
- Invalid inputs (missing fields, out of range values)
- Non-existent patient/doctor IDs
