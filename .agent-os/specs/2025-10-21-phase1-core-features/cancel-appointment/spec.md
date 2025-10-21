# Cancel Appointment - Feature Specification

**Feature:** Cancel Appointment
**Priority:** High
**Effort:** S (2-3 days)
**Status:** Draft
**Created:** 2025-10-21

## Overview

Allow cancelling scheduled or rescheduled appointments before they occur. This enables patients and staff to manage appointment changes and frees up doctor availability for other patients.

## Business Requirements

### User Story

**As a** patient or healthcare staff member
**I want to** cancel appointments that can no longer be kept
**So that** the time slot becomes available for other patients and the schedule stays accurate

### Business Rules

1. **Status Validation**
   - Can only cancel appointments in `Scheduled` or `Rescheduled` status
   - Cannot cancel `Completed` appointments
   - Cannot cancel already `Cancelled` appointments (idempotent)

2. **Cancellation Reason**
   - Reason is required (helps with analytics and patient communication)
   - Maximum 512 characters
   - Examples: "Patient request", "Doctor unavailable", "Weather emergency", etc.

3. **Timestamp**
   - `CancelledUtc` is set to current UTC time
   - Cannot be backdated

4. **Notification**
   - Domain event raised for notification system
   - Event contains patient, doctor, and cancellation details

5. **No Cancellation Window**
   - Unlike rescheduling, appointments can be cancelled at any time (even last minute)
   - No minimum notice requirement
   - Business may add policies in event handlers (e.g., no-show fees)

## Technical Specification

### API Endpoint

```http
POST /api/healthcare/appointments/{appointmentId}/cancel
Content-Type: application/json

{
  "reason": "Patient requested cancellation due to conflict"
}
```

**Response (200 OK):**
```json
{
  "id": "guid",
  "status": "Cancelled",
  "cancelledUtc": "2025-10-21T14:30:00Z",
  "cancellationReason": "Patient requested cancellation due to conflict"
}
```

### Domain Model Changes

**Appointment.cs** - Add Cancel method:

```csharp
public void Cancel(string reason)
{
    // Validation
    if (string.IsNullOrWhiteSpace(reason))
        throw new ArgumentException("Cancellation reason is required", nameof(reason));

    if (reason.Length > 512)
        throw new ArgumentException("Cancellation reason cannot exceed 512 characters", nameof(reason));

    if (Status == AppointmentStatus.Completed)
        throw new InvalidOperationException("Cannot cancel a completed appointment");

    if (Status == AppointmentStatus.Cancelled)
        return; // Idempotent - already cancelled

    // State transition
    Status = AppointmentStatus.Cancelled;
    CancelledUtc = DateTime.UtcNow;
    CancellationReason = reason;
}
```

Add properties:
```csharp
public DateTime? CancelledUtc { get; private set; }
public string? CancellationReason { get; private set; }
```

### Command & Handler

**CancelAppointmentCommand:**
```csharp
public record CancelAppointmentCommand(
    Guid AppointmentId,
    string Reason) : IRequest<ErrorOr<CancelAppointmentResult>>;
```

**Validation Rules:**
- `AppointmentId` - Required, not empty
- `Reason` - Required, not empty/whitespace, max 512 characters

**Handler Logic:**
1. Load appointment by ID
2. Return 404 if not found
3. Call `appointment.Cancel(reason)`
4. Catch `InvalidOperationException` → return Validation error (400)
5. Catch `ArgumentException` → return Validation error (400)
6. Raise `AppointmentCancelled` domain event
7. Save changes
8. Return result

### Domain Event

```csharp
public record AppointmentCancelledEvent(
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId,
    DateTime ScheduledStartUtc,
    DateTime ScheduledEndUtc,
    DateTime CancelledUtc,
    string CancellationReason) : INotification;
```

**Event Handler Use Cases (Future):**
- Send notification to patient (confirmation)
- Send notification to doctor/staff (availability update)
- Update reporting/analytics
- Apply cancellation policy (e.g., late cancellation fees)
- Update availability calendar

### Response Model

```csharp
public record CancelAppointmentResult(
    Guid Id,
    string Status,
    DateTime CancelledUtc,
    string CancellationReason);
```

### Error Scenarios

| Scenario | Status Code | Error Code | Message |
|----------|-------------|------------|---------|
| Appointment not found | 404 | Appointment.NotFound | Appointment with ID {id} not found |
| Already completed | 400 | Appointment.CannotCancelCompleted | Cannot cancel a completed appointment |
| Reason empty | 400 | Appointment.ReasonRequired | Cancellation reason is required |
| Reason too long | 400 | Appointment.ReasonTooLong | Cancellation reason cannot exceed 512 characters |

## Testing Requirements

### Unit Tests

**Domain Method Tests** (Appointment.Cancel):
1. ✅ Cancel scheduled appointment - sets status, timestamp, reason
2. ✅ Cancel rescheduled appointment - works correctly
3. ✅ Cancel already cancelled - idempotent (no error)
4. ✅ Cancel completed appointment - throws InvalidOperationException
5. ✅ Cancel with empty reason - throws ArgumentException
6. ✅ Cancel with null reason - throws ArgumentException
7. ✅ Cancel with whitespace reason - throws ArgumentException
8. ✅ Cancel with reason > 512 chars - throws ArgumentException
9. ✅ Cancel with valid reason - stores reason correctly

**Validator Tests:**
1. ✅ Valid command - passes validation
2. ✅ Empty AppointmentId - fails validation
3. ✅ Empty reason - fails validation
4. ✅ Null reason - fails validation
5. ✅ Whitespace reason - fails validation
6. ✅ Reason exactly 512 chars - passes
7. ✅ Reason 513 chars - fails validation

**Handler Tests:**
1. ✅ Valid request - cancels appointment
2. ✅ Non-existent appointment - returns NotFound
3. ✅ Completed appointment - returns Validation error
4. ✅ Already cancelled - succeeds (idempotent)
5. ✅ Domain event raised - verifies event dispatched

### Integration Tests

1. ✅ **Happy Path** - Cancel scheduled appointment returns 200
2. ✅ **With Long Reason** - Reason stored correctly
3. ✅ **Idempotent** - Cancelling twice succeeds, doesn't change timestamp
4. ✅ **Not Found** - Invalid appointment ID returns 404
5. ✅ **Cannot Cancel Completed** - Returns 400 with clear message
6. ✅ **Empty Reason** - Returns 400 validation error
7. ✅ **Reason Too Long** - Returns 400 validation error
8. ✅ **Database Verification** - Status updated, CancelledUtc set, reason stored
9. ✅ **Rescheduled Appointment** - Can cancel rescheduled appointment
10. ✅ **Timestamp Set** - CancelledUtc is close to current time
11. ✅ **Cancellation Near Appointment Time** - Can cancel even minutes before
12. ✅ **Event Contains Correct Data** - Verify event has all fields

**Estimated:** 10-12 integration tests

## Implementation Checklist

- [ ] Add `CancelledUtc` and `CancellationReason` properties to Appointment entity
- [ ] Implement `Cancel()` domain method with validation
- [ ] Create `CancelAppointmentCommand` and validator
- [ ] Implement `CancelAppointmentCommandHandler`
- [ ] Create `AppointmentCancelledEvent`
- [ ] Configure Minimal API endpoint
- [ ] Create database migration for new columns
- [ ] Write unit tests (domain method + validator + handler)
- [ ] Write integration tests
- [ ] Create HTTP request file with examples
- [ ] Update CLAUDE.md with example
- [ ] Code review

## HTTP Request Examples

Create `requests/Healthcare/Appointments/CancelAppointment.http`:

```http
### Cancel appointment - Happy path
POST {{baseUrl}}/api/healthcare/appointments/{{appointmentId}}/cancel
Content-Type: application/json

{
  "reason": "Patient requested cancellation due to scheduling conflict"
}

### Cancel appointment - Medical emergency
POST {{baseUrl}}/api/healthcare/appointments/{{appointmentId}}/cancel
Content-Type: application/json

{
  "reason": "Doctor unavailable due to emergency surgery"
}

### Cancel appointment - Weather
POST {{baseUrl}}/api/healthcare/appointments/{{appointmentId}}/cancel
Content-Type: application/json

{
  "reason": "Office closed due to severe weather conditions"
}

### Cancel appointment - Not found
POST {{baseUrl}}/api/healthcare/appointments/99999999-9999-9999-9999-999999999999/cancel
Content-Type: application/json

{
  "reason": "Testing not found"
}

### Cancel appointment - Already completed
# First complete appointment, then try to cancel
POST {{baseUrl}}/api/healthcare/appointments/{{completedAppointmentId}}/cancel
Content-Type: application/json

{
  "reason": "Trying to cancel completed appointment"
}

### Cancel appointment - Empty reason (validation error)
POST {{baseUrl}}/api/healthcare/appointments/{{appointmentId}}/cancel
Content-Type: application/json

{
  "reason": ""
}

### Cancel appointment - Reason too long
POST {{baseUrl}}/api/healthcare/appointments/{{appointmentId}}/cancel
Content-Type: application/json

{
  "reason": "{{513characterstring}}"
}

### Cancel appointment - Idempotent test
# Cancel same appointment twice
POST {{baseUrl}}/api/healthcare/appointments/{{appointmentId}}/cancel
Content-Type: application/json

{
  "reason": "First cancellation"
}

###
POST {{baseUrl}}/api/healthcare/appointments/{{appointmentId}}/cancel
Content-Type: application/json

{
  "reason": "Second cancellation - should succeed but not change timestamp"
}

### Cancel appointment - Last minute cancellation
# Book appointment soon and cancel immediately
POST {{baseUrl}}/api/healthcare/appointments
Content-Type: application/json

{
  "patientId": "{{patientId}}",
  "doctorId": "{{doctorId}}",
  "start": "{{onehourFromNow}}",
  "end": "{{onehourfromNowPlus30min}}",
  "notes": "Test appointment for last-minute cancellation"
}

### Now cancel it
POST {{baseUrl}}/api/healthcare/appointments/{{justCreatedAppointmentId}}/cancel
Content-Type: application/json

{
  "reason": "Testing last-minute cancellation - no minimum notice required"
}
```

## Database Migration

```csharp
public partial class AddCancellationFieldsToAppointment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "CancelledUtc",
            table: "Appointments",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CancellationReason",
            table: "Appointments",
            type: "nvarchar(512)",
            maxLength: 512,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CancelledUtc",
            table: "Appointments");

        migrationBuilder.DropColumn(
            name: "CancellationReason",
            table: "Appointments");
    }
}
```

## Documentation Updates

### CLAUDE.md Addition

Add section under "Feature Development Patterns":

```markdown
### Cancelling an Appointment

```csharp
// Domain method with validation
public void Cancel(string reason)
{
    if (string.IsNullOrWhiteSpace(reason))
        throw new ArgumentException("Cancellation reason is required");

    if (Status == AppointmentStatus.Completed)
        throw new InvalidOperationException("Cannot cancel a completed appointment");

    if (Status == AppointmentStatus.Cancelled)
        return; // Idempotent

    Status = AppointmentStatus.Cancelled;
    CancelledUtc = DateTime.UtcNow;
    CancellationReason = reason;
}

// Usage in handler
var appointment = await _context.Appointments.FindAsync(request.AppointmentId);
appointment.Cancel(request.Reason);
await _context.SaveChangesAsync();
```

## Acceptance Criteria

- [x] Scheduled appointments can be cancelled
- [x] Rescheduled appointments can be cancelled
- [x] Completed appointments cannot be cancelled (returns 400)
- [x] Cancellation is idempotent (no error on double-cancel)
- [x] Reason is required and validated (max 512 chars)
- [x] CancelledUtc timestamp is recorded
- [x] Cancellation reason is stored
- [x] Domain event is raised with all relevant data
- [x] No minimum cancellation notice required (can cancel anytime)
- [x] All unit tests passing (15+ tests)
- [x] All integration tests passing (10-12 tests)
- [x] HTTP request file created
- [x] Documentation updated

## Timeline

- **Day 1:** Domain model + unit tests
- **Day 2:** Handler + validation + integration tests
- **Day 3:** HTTP requests + documentation + review

## Comparison with Complete

| Aspect | Complete | Cancel |
|--------|----------|--------|
| **Required Input** | Notes (optional) | Reason (required) |
| **Max Length** | 1024 chars | 512 chars |
| **Can do to Cancelled** | No | N/A (already cancelled) |
| **Can do to Completed** | N/A (already completed) | No |
| **Timing Restrictions** | None | None (can cancel last minute) |
| **Common Use Case** | After appointment happens | Before appointment happens |

## Next Steps

After completing this feature:
1. Verify appointment state machine is complete
2. Create state diagram showing all transitions
3. Begin Get Appointments query feature
4. Consider adding cancellation policy hooks in event handler
