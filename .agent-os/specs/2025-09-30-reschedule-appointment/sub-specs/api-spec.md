# API Specification

This is the API specification for the spec detailed in @.agent-os/specs/2025-09-30-reschedule-appointment/spec.md

## Endpoint Details

**Method:** `POST`
**Path:** `/api/healthcare/appointments/{appointmentId}/reschedule`
**Content-Type:** `application/json`
**Authorization:** Not implemented (future: Patient, Doctor, or Admin roles)

---

## Request

### Route Parameter

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `appointmentId` | GUID | ✅ Yes | Unique identifier of the appointment to reschedule |

### Request Body

```json
{
  "appointmentId": "string (guid)",
  "newStart": "string (ISO-8601 datetime)",
  "newEnd": "string (ISO-8601 datetime)",
  "reason": "string (optional, max 512 chars)"
}
```

### Field Specifications

| Field | Type | Required | Constraints | Description |
|-------|------|----------|-------------|-------------|
| `appointmentId` | GUID | ✅ Yes | Must match route parameter, must exist | Appointment to reschedule |
| `newStart` | ISO-8601 DateTime | ✅ Yes | Must be >= UtcNow + 2 hours | New appointment start time |
| `newEnd` | ISO-8601 DateTime | ✅ Yes | Must be > newStart | New appointment end time |
| `reason` | string | ❌ No | Max 512 characters | Optional reason for rescheduling |

### Business Rules

| Rule | Validation | Error |
|------|------------|-------|
| Minimum duration | `newEnd >= newStart + 10 minutes` | 400 Bad Request |
| Maximum duration | `newEnd <= newStart + 8 hours` | 400 Bad Request |
| Advance notice (new time) | `newStart > UtcNow + 2 hours` | 400 Bad Request |
| 24-hour rule (original time) | `UtcNow < originalStart - 24 hours` | 400 Bad Request |
| Appointment exists | Database lookup | 404 Not Found |
| Appointment not cancelled | `Status != Cancelled` | 400 Bad Request |
| Appointment not completed | `Status != Completed` | 400 Bad Request |
| Doctor availability | No overlapping appointments | 409 Conflict |

---

## Response Scenarios

### ✅ Success (200 OK)

**Status Code:** `200 OK`

**Body:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "startUtc": "2025-08-21T10:00:00Z",
  "endUtc": "2025-08-21T10:30:00Z",
  "previousStartUtc": "2025-08-20T10:00:00Z",
  "previousEndUtc": "2025-08-20T10:30:00Z"
}
```

**Response Schema:**

| Field | Type | Description |
|-------|------|-------------|
| `id` | GUID | Unique identifier of the appointment |
| `startUtc` | ISO-8601 DateTime | New appointment start time in UTC |
| `endUtc` | ISO-8601 DateTime | New appointment end time in UTC |
| `previousStartUtc` | ISO-8601 DateTime | Original appointment start time in UTC |
| `previousEndUtc` | ISO-8601 DateTime | Original appointment end time in UTC |

---

### ❌ Validation Error (400 Bad Request)

**Scenario:** Invalid request data (e.g., duration too short, newStart >= newEnd)

**Status Code:** `400 Bad Request`

**Body:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "NewEnd": ["Appointment must be at least 10 minutes long"],
    "Reason": ["Reason cannot exceed 512 characters"]
  }
}
```

**Validation Error Examples:**

| Scenario | Error Message |
|----------|---------------|
| `appointmentId` is empty GUID | `"AppointmentId is required"` |
| `newStart >= newEnd` | `"New start time must be before new end time"` |
| Duration < 10 minutes | `"Appointment must be at least 10 minutes long"` |
| Duration > 8 hours | `"Appointment cannot be longer than 8 hours"` |
| `newStart < UtcNow + 2 hours` | `"Appointment must be scheduled at least 2 hours in advance"` |
| `reason` > 512 characters | `"Reason cannot exceed 512 characters"` |

---

### ❌ Cannot Reschedule Cancelled (400 Bad Request)

**Scenario:** Appointment status is Cancelled

**Status Code:** `400 Bad Request`

**Body:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Appointment.CannotRescheduleCancelled",
  "status": 400,
  "detail": "Cannot reschedule a cancelled appointment"
}
```

---

### ❌ Cannot Reschedule Completed (400 Bad Request)

**Scenario:** Appointment status is Completed

**Status Code:** `400 Bad Request`

**Body:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Appointment.CannotRescheduleCompleted",
  "status": 400,
  "detail": "Cannot reschedule a completed appointment"
}
```

---

### ❌ Appointment Not Found (404)

**Scenario:** Appointment with given `appointmentId` doesn't exist

**Status Code:** `404 Not Found`

**Body:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Appointment.NotFound",
  "status": 404,
  "detail": "Appointment with ID 3fa85f64-5717-4562-b3fc-2c963f66afa6 not found"
}
```

---

### ❌ Appointment Conflict (409)

**Scenario:** Doctor has an overlapping appointment at the new time

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
- Excludes the appointment being rescheduled (by ID)
- Ignores appointments with status: `Cancelled` or `Completed`
- Overlap logic: `existingStart < requestEnd && existingEnd > requestStart`

---

### ❌ Reschedule Window Closed (400 Bad Request)

**Scenario:** Attempting to reschedule within 24 hours of original appointment start time

**Status Code:** `400 Bad Request`

**Body:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Appointment.RescheduleWindowClosed",
  "status": 400,
  "detail": "Appointments cannot be rescheduled within 24 hours of the start time"
}
```

---

## HTTP Request Examples

### Example 1: Happy Path (200 OK)

```http
POST https://localhost:7098/api/healthcare/appointments/3fa85f64-5717-4562-b3fc-2c963f66afa6/reschedule
Content-Type: application/json

{
  "appointmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "newStart": "2025-08-21T10:00:00Z",
  "newEnd": "2025-08-21T10:30:00Z",
  "reason": "Patient requested earlier time slot"
}
```

**Expected Response:**
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "startUtc": "2025-08-21T10:00:00Z",
  "endUtc": "2025-08-21T10:30:00Z",
  "previousStartUtc": "2025-08-20T10:00:00Z",
  "previousEndUtc": "2025-08-20T10:30:00Z"
}
```

---

### Example 2: Reschedule Within 24 Hours (400)

```http
POST https://localhost:7098/api/healthcare/appointments/3fa85f64-5717-4562-b3fc-2c963f66afa6/reschedule
Content-Type: application/json

{
  "appointmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "newStart": "2025-10-02T10:00:00Z",
  "newEnd": "2025-10-02T10:30:00Z"
}
```

**Expected Response:**
```http
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Appointment.RescheduleWindowClosed",
  "status": 400,
  "detail": "Appointments cannot be rescheduled within 24 hours of the start time"
}
```

---

### Example 3: Overlapping Appointment (409)

```http
POST https://localhost:7098/api/healthcare/appointments/3fa85f64-5717-4562-b3fc-2c963f66afa6/reschedule
Content-Type: application/json

{
  "appointmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "newStart": "2025-08-20T14:00:00Z",
  "newEnd": "2025-08-20T14:30:00Z"
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

### Example 4: Invalid Time Window (400)

```http
POST https://localhost:7098/api/healthcare/appointments/3fa85f64-5717-4562-b3fc-2c963f66afa6/reschedule
Content-Type: application/json

{
  "appointmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "newStart": "2025-08-21T10:00:00Z",
  "newEnd": "2025-08-21T09:00:00Z"
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
    "NewStart": ["New start time must be before new end time"]
  }
}
```

---

### Example 5: Duration Too Short (400)

```http
POST https://localhost:7098/api/healthcare/appointments/3fa85f64-5717-4562-b3fc-2c963f66afa6/reschedule
Content-Type: application/json

{
  "appointmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "newStart": "2025-08-21T10:00:00Z",
  "newEnd": "2025-08-21T10:05:00Z"
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
    "NewEnd": ["Appointment must be at least 10 minutes long"]
  }
}
```

---

### Example 6: Appointment Not Found (404)

```http
POST https://localhost:7098/api/healthcare/appointments/99999999-9999-9999-9999-999999999999/reschedule
Content-Type: application/json

{
  "appointmentId": "99999999-9999-9999-9999-999999999999",
  "newStart": "2025-08-21T10:00:00Z",
  "newEnd": "2025-08-21T10:30:00Z"
}
```

**Expected Response:**
```http
HTTP/1.1 404 Not Found
Content-Type: application/json

{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Appointment.NotFound",
  "status": 404,
  "detail": "Appointment with ID 99999999-9999-9999-9999-999999999999 not found"
}
```

---

### Example 7: Cannot Reschedule Cancelled (400)

```http
POST https://localhost:7098/api/healthcare/appointments/cancelled-appointment-id/reschedule
Content-Type: application/json

{
  "appointmentId": "cancelled-appointment-id",
  "newStart": "2025-08-21T10:00:00Z",
  "newEnd": "2025-08-21T10:30:00Z"
}
```

**Expected Response:**
```http
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Appointment.CannotRescheduleCancelled",
  "status": 400,
  "detail": "Cannot reschedule a cancelled appointment"
}
```

---

## Implementation Notes

### Timezone Handling

- **Input:** Accepts both UTC and local time in ISO-8601 format
- **Normalization:** Converts to UTC via `DateTimeOffset.UtcDateTime`
- **Storage:** All times stored as UTC in database
- **Output:** Returns UTC times in response

---

### Status Transitions

| From Status | Can Reschedule? | New Status After Reschedule |
|-------------|-----------------|---------------------------|
| Scheduled | ✅ Yes | Rescheduled |
| Rescheduled | ✅ Yes | Rescheduled |
| Completed | ❌ No | N/A - returns 400 |
| Cancelled | ❌ No | N/A - returns 400 |

---

### Audit Trail

**Notes Field Updates:**
- If no reason provided: Notes unchanged
- If reason provided and Notes empty: Notes = reason
- If reason provided and Notes exist: Notes = original + "; " + reason

**Example:**
```
Original Notes: "Initial consultation"
Reason: "Patient requested earlier time"
Updated Notes: "Initial consultation; Patient requested earlier time"
```

---

### Domain Event

**AppointmentRescheduledEvent** is raised after successful update:
```csharp
public class AppointmentRescheduledEvent(
    Guid appointmentId,
    DateTime previousStartUtc,
    DateTime previousEndUtc,
    DateTime newStartUtc,
    DateTime newEndUtc) : DomainEvent
```

**Event Handler (Placeholder):**
- Logs reschedule action
- TODO: Send notification to patient
- TODO: Send notification to doctor

---

### Performance Optimizations

1. **Database Indexes:**
   - Uses existing `IX_Appointments_Doctor_TimeRange (DoctorId, StartUtc, EndUtc)` for conflict detection

2. **Query Optimization:**
   - Uses `AsNoTracking()` for conflict detection (read-only)
   - Single database round trip for conflict check

---

## Error Code Reference

| Error Code | HTTP Status | Description |
|------------|-------------|-------------|
| `Appointment.NotFound` | 404 | Appointment doesn't exist |
| `Appointment.CannotRescheduleCancelled` | 400 | Appointment is cancelled |
| `Appointment.CannotRescheduleCompleted` | 400 | Appointment is completed |
| `Appointment.RescheduleWindowClosed` | 400 | Within 24 hours of original start time |
| `Appointment.Conflict` | 409 | Doctor has overlapping appointment |
| Validation errors | 400 | Request fails FluentValidation rules |

---

## Related Endpoints

- **POST /api/healthcare/appointments** - Book new appointment
- **POST /api/healthcare/appointments/{id}/complete** - Complete appointment (not yet implemented)
- **POST /api/healthcare/appointments/{id}/cancel** - Cancel appointment (not yet implemented)
- **GET /api/healthcare/appointments** - Query appointments (not yet implemented)

---

## References

- **RFC 7807:** Problem Details for HTTP APIs
- **ISO 8601:** Date and time format standard
- **Original Spec:** `.github/specs/Healthcare/Appointments/RescheduleAppointment.md`