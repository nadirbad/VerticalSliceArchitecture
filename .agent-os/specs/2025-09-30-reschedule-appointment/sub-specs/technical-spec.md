# Technical Specification

This is the technical specification for the spec detailed in @.agent-os/specs/2025-09-30-reschedule-appointment/spec.md

## Technical Requirements

### Vertical Slice Structure

**File:** `src/Application/Features/Healthcare/Appointments/RescheduleAppointment.cs`

All related code organized in single file following Vertical Slice Architecture pattern:
- Controller: `RescheduleAppointmentController`
- Command: `RescheduleAppointmentCommand` record
- Result DTO: `RescheduleAppointmentResult` record
- Validator: `RescheduleAppointmentCommandValidator`
- Handler: `RescheduleAppointmentCommandHandler`

### Controller Specifications

```csharp
public class RescheduleAppointmentController : ApiControllerBase
{
    [HttpPost("/api/healthcare/appointments/{appointmentId}/reschedule")]
    public async Task<IActionResult> Reschedule(
        Guid appointmentId,
        RescheduleAppointmentCommand command)
}
```

**Requirements:**
- Route parameter `appointmentId` must match `command.AppointmentId` (validated in handler)
- Inherits from `ApiControllerBase` for consistent error handling
- Uses `ErrorOr` pattern with `Problem()` method for error responses
- Returns `200 OK` on success with old and new times
- Returns appropriate error status codes: 400, 404, 409, 422

### Command & Result DTOs

**Command:**
```csharp
public record RescheduleAppointmentCommand(
    Guid AppointmentId,
    DateTimeOffset NewStart,
    DateTimeOffset NewEnd,
    string? Reason) : IRequest<ErrorOr<RescheduleAppointmentResult>>;
```

**Result:**
```csharp
public record RescheduleAppointmentResult(
    Guid Id,
    DateTime StartUtc,
    DateTime EndUtc,
    DateTime PreviousStartUtc,
    DateTime PreviousEndUtc);
```

**Note:** RowVersion support deferred to Phase 2 for concurrency handling

### Validation Rules

**FluentValidation Rules:**
1. `AppointmentId` not empty GUID
2. `NewStart < NewEnd`
3. Duration `>= 10 minutes` (`NewEnd >= NewStart + 10 minutes`)
4. Duration `<= 8 hours` (`NewEnd <= NewStart + 8 hours`)
5. Advance notice `>= 2 hours` (`NewStart >= UtcNow + 2 hours`) - **Stricter than booking (15 min)**
6. `Reason` max length 512 characters (optional)

### Handler Business Logic

**Step-by-step flow:**

1. **Normalize to UTC**
   ```csharp
   var newStartUtc = request.NewStart.UtcDateTime;
   var newEndUtc = request.NewEnd.UtcDateTime;
   ```

2. **Load Appointment**
   ```csharp
   var appointment = await _context.Appointments
       .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken);
   ```
   - If not found → `Error.NotFound("Appointment.NotFound", ...)`
   - Store original `StartUtc` and `EndUtc` for response and event

3. **Check Status**
   - If `Status == Cancelled` → `Error.Validation("Appointment.CannotRescheduleCancelled", ...)`
   - If `Status == Completed` → `Error.Validation("Appointment.CannotRescheduleCompleted", ...)`

4. **Enforce 24-Hour Rule**
   ```csharp
   if (DateTime.UtcNow >= appointment.StartUtc.AddHours(-24))
       return Error.Validation("Appointment.RescheduleWindowClosed",
           "Appointments cannot be rescheduled within 24 hours of the start time");
   ```
   - Maps to HTTP 422 Unprocessable Entity

5. **Check Doctor Availability**
   ```csharp
   var hasOverlap = await _context.Appointments
       .AsNoTracking()
       .AnyAsync(
           a => a.Id != request.AppointmentId  // Exclude current appointment
                && a.DoctorId == appointment.DoctorId
                && (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Rescheduled)
                && a.StartUtc < newEndUtc
                && a.EndUtc > newStartUtc,
           cancellationToken);
   ```
   - If conflict → `Error.Conflict("Appointment.Conflict", ...)`

6. **Update Appointment via Domain Method**
   ```csharp
   appointment.Reschedule(newStartUtc, newEndUtc, request.Reason);
   ```
   - Domain method handles:
     - Updating `StartUtc`, `EndUtc`
     - Setting `Status = AppointmentStatus.Rescheduled`
     - Appending reason to `Notes` field if provided

7. **Raise Domain Event**
   ```csharp
   appointment.DomainEvents.Add(
       new AppointmentRescheduledEvent(
           appointment.Id,
           previousStartUtc,
           previousEndUtc,
           appointment.StartUtc,
           appointment.EndUtc));
   ```

8. **Persist Changes**
   ```csharp
   await _context.SaveChangesAsync(cancellationToken);
   ```
   - EF Core automatically dispatches domain events after save

9. **Return Result**
   ```csharp
   return new RescheduleAppointmentResult(
       appointment.Id,
       appointment.StartUtc,
       appointment.EndUtc,
       previousStartUtc,
       previousEndUtc);
   ```

### Domain Model Requirements

**Existing Method in `Appointment` entity:**
```csharp
public void Reschedule(DateTime newStartUtc, DateTime newEndUtc, string? reason = null)
{
    // Already implemented with:
    // - UTC validation
    // - Time window validation
    // - Status checks (cannot reschedule cancelled/completed)
    // - Status update to Rescheduled
    // - Notes appending logic
}
```

**Verification:** Domain method already exists and meets requirements (see `src/Application/Domain/Healthcare/Appointment.cs:52-80`)

### Domain Event

**New Event Required:**
```csharp
public class AppointmentRescheduledEvent(
    Guid appointmentId,
    DateTime previousStartUtc,
    DateTime previousEndUtc,
    DateTime newStartUtc,
    DateTime newEndUtc) : DomainEvent
{
    public Guid AppointmentId { get; } = appointmentId;
    public DateTime PreviousStartUtc { get; } = previousStartUtc;
    public DateTime PreviousEndUtc { get; } = previousEndUtc;
    public DateTime NewStartUtc { get; } = newStartUtc;
    public DateTime NewEndUtc { get; } = newEndUtc;
}
```

**Event Handler (Placeholder):**
```csharp
internal sealed class AppointmentRescheduledEventHandler(
    ILogger<AppointmentRescheduledEventHandler> logger)
    : INotificationHandler<DomainEventNotification<AppointmentRescheduledEvent>>
{
    public async Task Handle(
        DomainEventNotification<AppointmentRescheduledEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation(
            "Appointment {AppointmentId} rescheduled from {OldStart}-{OldEnd} to {NewStart}-{NewEnd}",
            domainEvent.AppointmentId,
            domainEvent.PreviousStartUtc,
            domainEvent.PreviousEndUtc,
            domainEvent.NewStartUtc,
            domainEvent.NewEndUtc);

        // TODO: Send notifications to patient and doctor
        await Task.CompletedTask;
    }
}
```

### Error Codes

| Error Code | HTTP Status | Description |
|------------|-------------|-------------|
| `Appointment.NotFound` | 404 | Appointment with given ID doesn't exist |
| `Appointment.CannotRescheduleCancelled` | 400 | Cannot reschedule a cancelled appointment |
| `Appointment.CannotRescheduleCompleted` | 400 | Cannot reschedule a completed appointment |
| `Appointment.RescheduleWindowClosed` | 422 | Cannot reschedule within 24 hours of start time |
| `Appointment.Conflict` | 409 | Doctor has overlapping appointment at new time |
| Validation errors | 400 | FluentValidation rule failures |

### Database Considerations

**No Schema Changes Required:**
- Existing `Appointments` table has all necessary columns
- `Status` enum already includes `Rescheduled = 2`
- `Notes` field can accommodate reason appending
- `RowVersion` column already configured for concurrency (deferred usage)
- Existing indexes support overlap query

**Indexes Used:**
- `IX_Appointments_Doctor_TimeRange (DoctorId, StartUtc, EndUtc)` - For conflict detection

### Performance Considerations

1. **Query Optimization**
   - Use `AsNoTracking()` for conflict detection query
   - Leverage existing composite index on (DoctorId, StartUtc, EndUtc)

2. **Concurrency**
   - EF Core `RowVersion` support deferred to Phase 2
   - Current implementation: Last write wins (acceptable for MVP)

3. **Domain Event Dispatch**
   - Events dispatched after `SaveChangesAsync` completes
   - Async notification handlers don't block response

### Testing Requirements

**Unit Tests:**
1. **Domain Model Tests**
   - `Reschedule_WithValidParameters_ShouldUpdateTimes`
   - `Reschedule_WhenCancelled_ShouldThrowInvalidOperationException`
   - `Reschedule_WhenCompleted_ShouldThrowInvalidOperationException`
   - `Reschedule_WithReason_ShouldAppendToNotes`

2. **Validator Tests**
   - `Should_Have_Error_When_AppointmentId_Is_Empty`
   - `Should_Have_Error_When_NewStart_Is_After_NewEnd`
   - `Should_Have_Error_When_Duration_Is_Less_Than_10_Minutes`
   - `Should_Have_Error_When_Duration_Is_More_Than_8_Hours`
   - `Should_Have_Error_When_NewStart_Is_Less_Than_2_Hours_From_Now`
   - `Should_Have_Error_When_Reason_Exceeds_512_Characters`
   - Boundary tests for exact durations

**Integration Tests:**
1. Happy path - reschedule succeeds, returns 200 with old and new times
2. 24-hour rule - reschedule within 24h returns 422
3. Conflict detection - overlapping appointment returns 409
4. Appointment not found - returns 404
5. Cannot reschedule cancelled appointment - returns 400
6. Cannot reschedule completed appointment - returns 400

**HTTP Request File:**
- Create `requests/Healthcare/Appointments/RescheduleAppointment.http`
- Include scenarios: happy path, 24h violation, conflict, not found, status validation

### UI/UX Considerations (Future)

**Out of scope for this spec, but documented for future reference:**
- Calendar UI for selecting new appointment time
- Visual indication of doctor availability
- Reason for rescheduling input field (optional)
- Confirmation dialog showing old vs new times
- Real-time conflict checking as user selects time

### Integration Points

**Current:**
- MediatR pipeline behaviors (Validation, Logging, Performance)
- EF Core ApplicationDbContext
- Domain event dispatcher

**Future (Phase 2):**
- Notification service (email/SMS via event handlers)
- Authorization service (role-based access control)
- Calendar integration APIs

## External Dependencies

**None required** - All functionality uses existing libraries and infrastructure already in the project:
- MediatR (already installed)
- FluentValidation (already installed)
- ErrorOr (already installed)
- Entity Framework Core (already installed)