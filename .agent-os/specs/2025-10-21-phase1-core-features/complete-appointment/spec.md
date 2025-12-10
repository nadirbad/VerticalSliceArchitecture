# Complete Appointment - Feature Specification

**Feature:** Complete Appointment
**Priority:** High
**Effort:** S (2-3 days)
**Status:** Draft
**Created:** 2025-10-21

## Overview

Allow marking appointments as completed after they have been conducted. This completes the appointment lifecycle state machine and enables tracking of appointment history.

## Business Requirements

### User Story

**As a** doctor or healthcare staff member
**I want to** mark appointments as completed after they finish
**So that** we can track appointment history and patient visit records

### Business Rules

1. **Status Validation**
   - Can only complete appointments in `Scheduled` or `Rescheduled` status
   - Cannot complete `Cancelled` appointments
   - Cannot complete already `Completed` appointments (idempotent)

2. **Completion Notes**
   - Notes are optional
   - Maximum 1024 characters
   - Can include visit summary, treatment provided, follow-up needed, etc.

3. **Timestamp**
   - `CompletedUtc` is set to current UTC time
   - Cannot be backdated

4. **Audit Trail**
   - Status transition logged
   - Completion notes stored
   - Domain event raised for integrations

## Technical Specification

### API Endpoint

```http
POST /api/healthcare/appointments/{appointmentId}/complete
Content-Type: application/json

{
  "notes": "Patient arrived on time. Routine checkup completed. No issues found."
}
```

**Response (200 OK):**
```json
{
  "id": "guid",
  "status": "Completed",
  "completedUtc": "2025-10-21T14:30:00Z",
  "notes": "Patient arrived on time..."
}
```

### Domain Model Changes

**Appointment.cs** - Add Complete method:

```csharp
public void Complete(string? notes = null)
{
    // Validation
    if (Status == AppointmentStatus.Cancelled)
        throw new InvalidOperationException("Cannot complete a cancelled appointment");

    if (Status == AppointmentStatus.Completed)
        return; // Idempotent - already completed

    if (!string.IsNullOrEmpty(notes) && notes.Length > 1024)
        throw new ArgumentException("Notes cannot exceed 1024 characters", nameof(notes));

    // State transition
    Status = AppointmentStatus.Completed;
    CompletedUtc = DateTime.UtcNow;
    Notes = notes;
}
```

Add property:
```csharp
public DateTime? CompletedUtc { get; private set; }
```

### Command & Handler

**CompleteAppointmentCommand:**
```csharp
public record CompleteAppointmentCommand(
    Guid AppointmentId,
    string? Notes) : IRequest<ErrorOr<CompleteAppointmentResult>>;
```

**Validation Rules:**
- `AppointmentId` - Required, not empty
- `Notes` - Optional, max 1024 characters

**Handler Logic:**
1. Load appointment by ID
2. Return 404 if not found
3. Call `appointment.Complete(notes)`
4. Catch `InvalidOperationException` → return Validation error (400)
5. Catch `ArgumentException` → return Validation error (400)
6. Raise `AppointmentCompleted` domain event
7. Save changes
8. Return result

### Domain Event

```csharp
public record AppointmentCompletedEvent(
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId,
    DateTime CompletedUtc,
    string? Notes) : INotification;
```

**Event Handler (Future):**
- Send notification to patient
- Update analytics/reporting
- Trigger billing workflow

### Error Scenarios

| Scenario | Status Code | Error Code | Message |
|----------|-------------|------------|---------|
| Appointment not found | 404 | Appointment.NotFound | Appointment with ID {id} not found |
| Already cancelled | 400 | Appointment.CannotCompleteStatus | Cannot complete a cancelled appointment |
| Notes too long | 400 | Appointment.NotesTooLong | Notes cannot exceed 1024 characters |

## Testing Requirements

### Unit Tests

**Domain Method Tests** (Appointment.Complete):
1. ✅ Complete scheduled appointment - sets status and timestamp
2. ✅ Complete rescheduled appointment - works correctly
3. ✅ Complete with notes - stores notes
4. ✅ Complete with null notes - allows null
5. ✅ Complete already completed - idempotent (no error)
6. ✅ Complete cancelled appointment - throws InvalidOperationException
7. ✅ Complete with notes > 1024 chars - throws ArgumentException

**Validator Tests:**
1. ✅ Valid command - passes validation
2. ✅ Empty AppointmentId - fails validation
3. ✅ Notes exactly 1024 chars - passes
4. ✅ Notes 1025 chars - fails validation
5. ✅ Null notes - passes (optional)

**Handler Tests:**
1. ✅ Valid request - completes appointment
2. ✅ Non-existent appointment - returns NotFound
3. ✅ Cancelled appointment - returns Validation error
4. ✅ Already completed - succeeds (idempotent)
5. ✅ Domain event raised - verifies event dispatched

### Integration Tests

1. ✅ **Happy Path** - Complete scheduled appointment returns 200
2. ✅ **With Notes** - Completion notes stored correctly
3. ✅ **Without Notes** - Null notes accepted
4. ✅ **Idempotent** - Completing twice succeeds, doesn't change timestamp
5. ✅ **Not Found** - Invalid appointment ID returns 404
6. ✅ **Cannot Complete Cancelled** - Returns 400 with clear message
7. ✅ **Notes Too Long** - Returns 400 validation error
8. ✅ **Database Verification** - Status updated, CompletedUtc set
9. ✅ **Rescheduled Appointment** - Can complete rescheduled appointment
10. ✅ **Timestamp Set** - CompletedUtc is close to current time

**Estimated:** 10-12 integration tests

## Implementation Checklist

- [ ] Add `CompletedUtc` property to Appointment entity
- [ ] Implement `Complete()` domain method with validation
- [ ] Create `CompleteAppointmentCommand` and validator
- [ ] Implement `CompleteAppointmentCommandHandler`
- [ ] Create `AppointmentCompletedEvent`
- [ ] Configure Minimal API endpoint
- [ ] Create database migration for CompletedUtc column
- [ ] Write unit tests (domain method + validator + handler)
- [ ] Write integration tests
- [ ] Create HTTP request file with examples
- [ ] Update CLAUDE.md with example
- [ ] Code review

## HTTP Request Examples

Create `requests/Healthcare/Appointments/CompleteAppointment.http`:

```http
### Complete appointment - Happy path
POST {{baseUrl}}/api/healthcare/appointments/{{appointmentId}}/complete
Content-Type: application/json

{
  "notes": "Patient arrived on time. Routine checkup completed. No issues found. Recommended follow-up in 6 months."
}

### Complete appointment - No notes
POST {{baseUrl}}/api/healthcare/appointments/{{appointmentId}}/complete
Content-Type: application/json

{
  "notes": null
}

### Complete appointment - Not found
POST {{baseUrl}}/api/healthcare/appointments/99999999-9999-9999-9999-999999999999/complete
Content-Type: application/json

{}

### Complete appointment - Already cancelled
# First cancel appointment, then try to complete
POST {{baseUrl}}/api/healthcare/appointments/{{cancelledAppointmentId}}/complete
Content-Type: application/json

{
  "notes": "Trying to complete cancelled appointment"
}

### Complete appointment - Notes too long
POST {{baseUrl}}/api/healthcare/appointments/{{appointmentId}}/complete
Content-Type: application/json

{
  "notes": "{{1025characterstring}}"
}

### Complete appointment - Idempotent test
# Complete same appointment twice
POST {{baseUrl}}/api/healthcare/appointments/{{appointmentId}}/complete
Content-Type: application/json

{
  "notes": "First completion"
}

###
POST {{baseUrl}}/api/healthcare/appointments/{{appointmentId}}/complete
Content-Type: application/json

{
  "notes": "Second completion - should succeed but not change timestamp"
}
```

## Database Migration

```csharp
public partial class AddCompletedUtcToAppointment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "CompletedUtc",
            table: "Appointments",
            type: "datetime2",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CompletedUtc",
            table: "Appointments");
    }
}
```

## Documentation Updates

### CLAUDE.md Addition

Add section under "Feature Development Patterns":

```markdown
### Completing an Appointment

```csharp
// Domain method with validation
public void Complete(string? notes = null)
{
    if (Status == AppointmentStatus.Cancelled)
        throw new InvalidOperationException("Cannot complete a cancelled appointment");

    if (Status == AppointmentStatus.Completed)
        return; // Idempotent

    Status = AppointmentStatus.Completed;
    CompletedUtc = DateTime.UtcNow;
    Notes = notes;
}

// Usage in handler
var appointment = await _context.Appointments.FindAsync(request.AppointmentId);
appointment.Complete(request.Notes);
await _context.SaveChangesAsync();
```

## Acceptance Criteria

- [x] Scheduled appointments can be marked complete
- [x] Rescheduled appointments can be marked complete
- [x] Cancelled appointments cannot be completed (returns 400)
- [x] Completion is idempotent (no error on double-complete)
- [x] Optional notes can be provided (max 1024 chars)
- [x] CompletedUtc timestamp is recorded
- [x] Domain event is raised
- [x] All unit tests passing (15+ tests)
- [x] All integration tests passing (10-12 tests)
- [x] HTTP request file created
- [x] Documentation updated

## Timeline

- **Day 1:** Domain model + unit tests
- **Day 2:** Handler + validation + integration tests
- **Day 3:** HTTP requests + documentation + review

## Next Steps

After completing this feature:
1. Implement Cancel Appointment (similar complexity)
2. Update appointment state machine diagram
3. Verify full appointment lifecycle works end-to-end
