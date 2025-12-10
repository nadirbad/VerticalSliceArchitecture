# API Specification

This is the API specification for the spec detailed in [@.agent-os/specs/2025-10-01-prescription-issuance/spec.md](../.agent-os/specs/2025-10-01-prescription-issuance/spec.md)

## Endpoints

### POST /api/healthcare/prescriptions

**Purpose:** Issue a new prescription for a patient with medication details, dosage instructions, and refill information.

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

**Request Schema:**
| Field | Type | Required | Constraints | Description |
|-------|------|----------|-------------|-------------|
| patientId | integer | Yes | > 0 | ID of patient receiving prescription |
| doctorId | integer | Yes | > 0 | ID of doctor issuing prescription |
| medicationName | string | Yes | 1-200 chars | Name of medication (brand or generic) |
| dosage | string | Yes | 1-50 chars | Dosage amount (e.g., "500mg", "10ml") |
| directions | string | Yes | 1-500 chars | Instructions for taking medication |
| quantity | integer | Yes | 1-999 | Number of units (tablets, capsules, etc.) |
| numberOfRefills | integer | Yes | 0-12 | Number of refills allowed |
| durationInDays | integer | Yes | 1-365 | How many days prescription is valid |

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

**Response Schema:**
| Field | Type | Description |
|-------|------|-------------|
| prescriptionId | integer | Unique prescription identifier |
| patientId | integer | Patient ID |
| doctorId | integer | Doctor ID |
| medicationName | string | Medication name |
| dosage | string | Dosage amount |
| directions | string | Usage instructions |
| quantity | integer | Number of units prescribed |
| numberOfRefills | integer | Total refills allowed |
| remainingRefills | integer | Refills remaining (initially equals numberOfRefills) |
| issuedDateUtc | string (ISO 8601) | When prescription was issued (UTC) |
| expirationDateUtc | string (ISO 8601) | When prescription expires (UTC) |
| status | string | Current status: "Active", "Expired", or "Depleted" |

**Error Responses:**

**400 Bad Request** - Invalid request format
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "medicationName": ["The medicationName field is required."]
  }
}
```

**404 Not Found** - Patient or Doctor not found
```json
{
  "type": "Prescription.PatientNotFound",
  "title": "Patient Not Found",
  "status": 404,
  "detail": "Patient with ID 999 was not found."
}
```

```json
{
  "type": "Prescription.DoctorNotFound",
  "title": "Doctor Not Found",
  "status": 404,
  "detail": "Doctor with ID 888 was not found."
}
```

**422 Unprocessable Entity** - Validation errors
```json
{
  "type": "Prescription.Validation",
  "title": "Validation Error",
  "status": 422,
  "detail": "One or more validation errors occurred.",
  "errors": [
    "Medication name must be between 1 and 200 characters.",
    "Quantity must be between 1 and 999.",
    "Number of refills must be between 0 and 12."
  ]
}
```

**Example Validation Error Scenarios:**
- Medication name empty or > 200 characters
- Dosage empty or > 50 characters
- Directions empty or > 500 characters
- Quantity < 1 or > 999
- NumberOfRefills < 0 or > 12
- DurationInDays < 1 or > 365

## Implementation Details

**Minimal API Endpoint Definition:**

**File:** `src/Application/Features/Healthcare/IssuePrescription.cs`

```csharp
public static class IssuePrescriptionEndpoint
{
    public static void MapIssuePrescriptionEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/healthcare/prescriptions", async (
            IssuePrescriptionCommand command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);

            return result.Match(
                prescription => Results.Created(
                    $"/api/healthcare/prescriptions/{prescription.PrescriptionId}",
                    prescription),
                errors => errors.ToProblemDetails());
        })
        .WithName("IssuePrescription")
        .WithTags("Healthcare")
        .Produces<PrescriptionResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithOpenApi();
    }
}
```

**Registration in Program.cs:**
```csharp
app.MapIssuePrescriptionEndpoint();
```

## HTTP Request Examples

**File:** `requests/healthcare/issue-prescription.http`

```http
### Issue prescription - Success
POST {{baseUrl}}/api/healthcare/prescriptions
Content-Type: application/json

{
  "patientId": 1,
  "doctorId": 1,
  "medicationName": "Amoxicillin",
  "dosage": "500mg",
  "directions": "Take one capsule three times daily with food for 10 days",
  "quantity": 30,
  "numberOfRefills": 2,
  "durationInDays": 90
}

### Issue prescription - Long-term medication
POST {{baseUrl}}/api/healthcare/prescriptions
Content-Type: application/json

{
  "patientId": 2,
  "doctorId": 1,
  "medicationName": "Lisinopril",
  "dosage": "10mg",
  "directions": "Take one tablet once daily in the morning",
  "quantity": 90,
  "numberOfRefills": 5,
  "durationInDays": 180
}

### Issue prescription - No refills
POST {{baseUrl}}/api/healthcare/prescriptions
Content-Type: application/json

{
  "patientId": 1,
  "doctorId": 1,
  "medicationName": "Prednisone",
  "dosage": "20mg",
  "directions": "Take as directed: 2 tablets daily for 3 days, then 1 tablet daily for 3 days",
  "quantity": 9,
  "numberOfRefills": 0,
  "durationInDays": 30
}

### Issue prescription - Invalid quantity (too high)
POST {{baseUrl}}/api/healthcare/prescriptions
Content-Type: application/json

{
  "patientId": 1,
  "doctorId": 1,
  "medicationName": "Ibuprofen",
  "dosage": "200mg",
  "directions": "Take one tablet as needed for pain",
  "quantity": 1000,
  "numberOfRefills": 3,
  "durationInDays": 90
}

### Issue prescription - Invalid refills (too many)
POST {{baseUrl}}/api/healthcare/prescriptions
Content-Type: application/json

{
  "patientId": 1,
  "doctorId": 1,
  "medicationName": "Vitamin D",
  "dosage": "1000 IU",
  "directions": "Take one tablet daily",
  "quantity": 90,
  "numberOfRefills": 15,
  "durationInDays": 365
}

### Issue prescription - Invalid duration (too long)
POST {{baseUrl}}/api/healthcare/prescriptions
Content-Type: application/json

{
  "patientId": 1,
  "doctorId": 1,
  "medicationName": "Multivitamin",
  "dosage": "1 tablet",
  "directions": "Take one tablet daily",
  "quantity": 400,
  "numberOfRefills": 0,
  "durationInDays": 400
}

### Issue prescription - Missing required field (medicationName)
POST {{baseUrl}}/api/healthcare/prescriptions
Content-Type: application/json

{
  "patientId": 1,
  "doctorId": 1,
  "dosage": "500mg",
  "directions": "Take one capsule three times daily",
  "quantity": 30,
  "numberOfRefills": 2,
  "durationInDays": 90
}

### Issue prescription - Patient not found
POST {{baseUrl}}/api/healthcare/prescriptions
Content-Type: application/json

{
  "patientId": 99999,
  "doctorId": 1,
  "medicationName": "Amoxicillin",
  "dosage": "500mg",
  "directions": "Take one capsule three times daily",
  "quantity": 30,
  "numberOfRefills": 2,
  "durationInDays": 90
}

### Issue prescription - Doctor not found
POST {{baseUrl}}/api/healthcare/prescriptions
Content-Type: application/json

{
  "patientId": 1,
  "doctorId": 99999,
  "medicationName": "Amoxicillin",
  "dosage": "500mg",
  "directions": "Take one capsule three times daily",
  "quantity": 30,
  "numberOfRefills": 2,
  "durationInDays": 90
}

### Issue prescription - Empty medication name
POST {{baseUrl}}/api/healthcare/prescriptions
Content-Type: application/json

{
  "patientId": 1,
  "doctorId": 1,
  "medicationName": "",
  "dosage": "500mg",
  "directions": "Take one capsule three times daily",
  "quantity": 30,
  "numberOfRefills": 2,
  "durationInDays": 90
}

### Issue prescription - Negative quantity
POST {{baseUrl}}/api/healthcare/prescriptions
Content-Type: application/json

{
  "patientId": 1,
  "doctorId": 1,
  "medicationName": "Amoxicillin",
  "dosage": "500mg",
  "directions": "Take one capsule three times daily",
  "quantity": -5,
  "numberOfRefills": 2,
  "durationInDays": 90
}
```

## Query Endpoints (Future Consideration)

While not in scope for MVP, the following query endpoints would be natural follow-ups:

**GET /api/healthcare/prescriptions/{id}** - Get prescription by ID
**GET /api/healthcare/patients/{patientId}/prescriptions** - Get all prescriptions for a patient
**GET /api/healthcare/doctors/{doctorId}/prescriptions** - Get all prescriptions issued by a doctor
**GET /api/healthcare/prescriptions?status=Active** - Filter prescriptions by status

These would follow similar patterns but return lists or single items without modification capabilities.
