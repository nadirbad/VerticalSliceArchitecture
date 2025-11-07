# API Specification: Book Appointment

**Endpoint:** Book Appointment
**Feature:** Healthcare - Appointment Management
**Version:** 1.0
**Date:** 2025-09-30

---

## Endpoint Details

**Method:** `POST`
**Path:** `/api/healthcare/appointments`
**Content-Type:** `application/json`
**Authorization:** Not implemented (future: Patient role)

---

## Request

### Request Body

```json
{
  "patientId": "string (guid)",
  "doctorId": "string (guid)",
  "start": "string (ISO-8601 datetime)",
  "end": "string (ISO-8601 datetime)",
  "notes": "string (optional, max 1024 chars)"
}
```

### Field Specifications

| Field | Type | Required | Constraints | Description |
|-------|------|----------|-------------|-------------|
| `patientId` | GUID | ✅ Yes | Must exist in database | Unique identifier of the patient |
| `doctorId` | GUID | ✅ Yes | Must exist in database | Unique identifier of the doctor |
| `start` | ISO-8601 DateTime | ✅ Yes | Must be >= UtcNow + 15 minutes | Appointment start time (UTC or local, normalized to UTC) |
| `end` | ISO-8601 DateTime | ✅ Yes | Must be > start | Appointment end time (UTC or local, normalized to UTC) |
| `notes` | string | ❌ No | Max 1024 characters | Optional appointment notes |

### Business Rules

| Rule | Validation | Error |
|------|------------|-------|
| Minimum duration | `end >= start + 10 minutes` | 400 Bad Request |
| Maximum duration | `end <= start + 8 hours` | 400 Bad Request |
| Advance booking | `start > UtcNow + 15 minutes` | 400 Bad Request |
| Patient exists | Database lookup | 404 Not Found |
| Doctor exists | Database lookup | 404 Not Found |
| Doctor availability | No overlapping appointments for doctor | 409 Conflict |

---

## Response Scenarios

### ✅ Success (201 Created)

**Status Code:** `201 Created`

**Headers:**
```
Location: /api/healthcare/appointments/{appointmentId}
Content-Type: application/json
```

**Body:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "startUtc": "2025-08-20T10:00:00Z",
  "endUtc": "2025-08-20T10:30:00Z"
}
```

**Response Schema:**

| Field | Type | Description |
|-------|------|-------------|
| `id` | GUID | Unique identifier of created appointment |
| `startUtc` | ISO-8601 DateTime | Appointment start time in UTC |
| `endUtc` | ISO-8601 DateTime | Appointment end time in UTC |

---

### ❌ Validation Error (400 Bad Request)

**Scenario:** Invalid request data (e.g., duration too short, missing fields)

**Status Code:** `400 Bad Request`

**Body:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "End": ["Appointment must be at least 10 minutes long"],
    "Notes": ["Notes cannot exceed 1024 characters"]
  }
}
```

**Validation Error Examples:**

| Scenario | Error Message |
|----------|---------------|
| `patientId` is empty GUID | `"PatientId is required"` |
| `doctorId` is empty GUID | `"DoctorId is required"` |
| `start >= end` | `"Start time must be before end time"` |
| Duration < 10 minutes | `"Appointment must be at least 10 minutes long"` |
| Duration > 8 hours | `"Appointment cannot be longer than 8 hours"` |
| `start < UtcNow + 15 min` | `"Appointment must be scheduled at least 15 minutes in advance"` |
| `notes` > 1024 characters | `"Notes cannot exceed 1024 characters"` |

---

### ❌ Patient Not Found (404)

**Scenario:** Patient with given `patientId` doesn't exist

**Status Code:** `404 Not Found`

**Body:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Appointment.PatientNotFound",
  "status": 404,
  "detail": "Patient with ID 11111111-1111-1111-1111-111111111111 not found"
}
```

---

### ❌ Doctor Not Found (404)

**Scenario:** Doctor with given `doctorId` doesn't exist

**Status Code:** `404 Not Found`

**Body:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Appointment.DoctorNotFound",
  "status": 404,
  "detail": "Doctor with ID bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb not found"
}
```

---

### ❌ Appointment Conflict (409)

**Scenario:** Doctor has an overlapping appointment during the requested time window

**Status Code:** `409 Conflict`

**Body:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.8",
  "title": "Appointment.Conflict",
  "status": 409,
  "detail": "Doctor has a conflicting appointment during the requested time"
}
```

**Conflict Detection:**
- Checks appointments with status: `Scheduled` or `Rescheduled`
- Ignores appointments with status: `Cancelled` or `Completed`
- Overlap logic: `existingStart < requestEnd && existingEnd > requestStart`

---

## HTTP Request Examples

### Example 1: Happy Path (201 Created)

```http
POST https://localhost:7098/api/healthcare/appointments
Content-Type: application/json

{
  "patientId": "11111111-1111-1111-1111-111111111111",
  "doctorId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  "start": "2025-08-20T10:00:00Z",
  "end": "2025-08-20T10:30:00Z",
  "notes": "Initial consultation"
}
```

**Expected Response:**
```http
HTTP/1.1 201 Created
Location: /api/healthcare/appointments/3fa85f64-5717-4562-b3fc-2c963f66afa6
Content-Type: application/json

{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "startUtc": "2025-08-20T10:00:00Z",
  "endUtc": "2025-08-20T10:30:00Z"
}
```

---

### Example 2: Overlapping Appointment (409 Conflict)

```http
POST https://localhost:7098/api/healthcare/appointments
Content-Type: application/json

{
  "patientId": "33333333-3333-3333-3333-333333333333",
  "doctorId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  "start": "2025-08-20T10:15:00Z",
  "end": "2025-08-20T10:45:00Z"
}
```

**Expected Response:**
```http
HTTP/1.1 409 Conflict
Content-Type: application/json

{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.8",
  "title": "Appointment.Conflict",
  "status": 409,
  "detail": "Doctor has a conflicting appointment during the requested time"
}
```

---

### Example 3: Invalid Time Window (400 Bad Request)

```http
POST https://localhost:7098/api/healthcare/appointments
Content-Type: application/json

{
  "patientId": "11111111-1111-1111-1111-111111111111",
  "doctorId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  "start": "2025-08-20T10:00:00Z",
  "end": "2025-08-20T09:00:00Z"
}
```

**Expected Response:**
```http
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Start": ["Start time must be before end time"]
  }
}
```

---

### Example 4: Duration Too Short (400 Bad Request)

```http
POST https://localhost:7098/api/healthcare/appointments
Content-Type: application/json

{
  "patientId": "11111111-1111-1111-1111-111111111111",
  "doctorId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  "start": "2025-08-20T10:00:00Z",
  "end": "2025-08-20T10:05:00Z"
}
```

**Expected Response:**
```http
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "End": ["Appointment must be at least 10 minutes long"]
  }
}
```

---

### Example 5: Duration Too Long (400 Bad Request)

```http
POST https://localhost:7098/api/healthcare/appointments
Content-Type: application/json

{
  "patientId": "11111111-1111-1111-1111-111111111111",
  "doctorId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  "start": "2025-08-20T10:00:00Z",
  "end": "2025-08-20T19:00:00Z"
}
```

**Expected Response:**
```http
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "End": ["Appointment cannot be longer than 8 hours"]
  }
}
```

---

### Example 6: Not Enough Advance Time (400 Bad Request)

```http
POST https://localhost:7098/api/healthcare/appointments
Content-Type: application/json

{
  "patientId": "11111111-1111-1111-1111-111111111111",
  "doctorId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  "start": "2025-09-30T09:50:00Z",
  "end": "2025-09-30T10:20:00Z"
}
```

**Expected Response:**
```http
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Start": ["Appointment must be scheduled at least 15 minutes in advance"]
  }
}
```

---

### Example 7: Notes Too Long (400 Bad Request)

```http
POST https://localhost:7098/api/healthcare/appointments
Content-Type: application/json

{
  "patientId": "11111111-1111-1111-1111-111111111111",
  "doctorId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  "start": "2025-08-20T10:00:00Z",
  "end": "2025-08-20T10:30:00Z",
  "notes": "A very long note that exceeds 1024 characters... [1025 characters total]"
}
```

**Expected Response:**
```http
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Notes": ["Notes cannot exceed 1024 characters"]
  }
}
```

---

### Example 8: Patient Not Found (404)

```http
POST https://localhost:7098/api/healthcare/appointments
Content-Type: application/json

{
  "patientId": "99999999-9999-9999-9999-999999999999",
  "doctorId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  "start": "2025-08-20T10:00:00Z",
  "end": "2025-08-20T10:30:00Z"
}
```

**Expected Response:**
```http
HTTP/1.1 404 Not Found
Content-Type: application/json

{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Appointment.PatientNotFound",
  "status": 404,
  "detail": "Patient with ID 99999999-9999-9999-9999-999999999999 not found"
}
```

---

### Example 9: Doctor Not Found (404)

```http
POST https://localhost:7098/api/healthcare/appointments
Content-Type: application/json

{
  "patientId": "11111111-1111-1111-1111-111111111111",
  "doctorId": "99999999-9999-9999-9999-999999999999",
  "start": "2025-08-20T10:00:00Z",
  "end": "2025-08-20T10:30:00Z"
}
```

**Expected Response:**
```http
HTTP/1.1 404 Not Found
Content-Type: application/json

{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Appointment.DoctorNotFound",
  "status": 404,
  "detail": "Doctor with ID 99999999-9999-9999-9999-999999999999 not found"
}
```

---

## Test Data

### Available Patients (Seeded)

| ID | Name | Email | Phone |
|----|------|-------|-------|
| `11111111-1111-1111-1111-111111111111` | John Smith | john.smith@example.com | +1-555-0101 |
| `22222222-2222-2222-2222-222222222222` | Jane Doe | jane.doe@example.com | +1-555-0102 |
| `33333333-3333-3333-3333-333333333333` | Bob Johnson | bob.johnson@example.com | +1-555-0103 |

### Available Doctors (Seeded)

| ID | Name | Specialty |
|----|------|-----------|
| `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa` | Dr. Sarah Wilson | Family Medicine |
| `bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb` | Dr. Michael Chen | Cardiology |
| `cccccccc-cccc-cccc-cccc-cccccccccccc` | Dr. Emily Rodriguez | Pediatrics |

---

## Implementation Notes

### Timezone Handling

- **Input:** Accepts both UTC and local time in ISO-8601 format
- **Normalization:** Converts to UTC via `DateTimeOffset.UtcDateTime`
- **Storage:** All times stored as UTC in database
- **Output:** Returns UTC times in response

**Example:**
```json
// Input (PST)
"start": "2025-08-20T10:00:00-07:00"

// Normalized to UTC
"startUtc": "2025-08-20T17:00:00Z"
```

---

### Concurrency Control

- **Mechanism:** Optimistic concurrency via `RowVersion` column
- **Scenario:** Multiple users booking same doctor at same time
- **Behavior:** First request wins, subsequent requests get 409 Conflict

---

### Performance Optimizations

1. **Database Indexes:**
   - `IX_Appointments_Doctor_TimeRange (DoctorId, StartUtc, EndUtc)` - Speeds up overlap detection
   - `IX_Appointments_Patient_StartTime (PatientId, StartUtc)` - Speeds up patient appointment queries

2. **Query Optimization:**
   - Uses `AsNoTracking()` for existence checks (read-only operations)
   - Reduces memory usage and improves query performance

---

## Future Enhancements

### Idempotency (Not Implemented)

**Spec mentions:** Optional `Idempotency-Key` header support

**Proposed Implementation:**
```http
POST /api/healthcare/appointments
Idempotency-Key: unique-client-generated-key
Content-Type: application/json
```

**Behavior:**
- Check if appointment with same (PatientId, DoctorId, Start, End) exists
- If exists within time window (e.g., 24 hours), return existing appointment
- If not exists, create new appointment

---

### Authorization (Not Implemented)

**Future Requirements:**
- JWT bearer token authentication
- Role-based authorization:
  - **Patient:** Can only book for themselves
  - **Doctor/Admin:** Can book on behalf of any patient

---

## Error Code Reference

| Error Code | HTTP Status | Description |
|------------|-------------|-------------|
| `Appointment.PatientNotFound` | 404 | Patient with given ID doesn't exist |
| `Appointment.DoctorNotFound` | 404 | Doctor with given ID doesn't exist |
| `Appointment.Conflict` | 409 | Doctor has overlapping appointment |
| Validation errors | 400 | Request fails FluentValidation rules |

---

## Related Endpoints

- **GET /api/healthcare/appointments** - Query appointments (not yet implemented)
- **POST /api/healthcare/appointments/{id}/reschedule** - Reschedule appointment (not yet implemented)
- **POST /api/healthcare/appointments/{id}/complete** - Complete appointment (not yet implemented)
- **POST /api/healthcare/appointments/{id}/cancel** - Cancel appointment (not yet implemented)

---

## References

- **RFC 7807:** Problem Details for HTTP APIs
- **ISO 8601:** Date and time format standard
- **Original Spec:** `.github/specs/Healthcare/Appointments/BookAppointment.md`
- **Implementation:** `src/Application/Features/Healthcare/Appointments/BookAppointment.cs`