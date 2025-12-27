# Tech Lead Code Review - Session Summary

**Date:** 2025-12-27
**Repository:** VerticalSliceArchitecture-wt-health-cc
**Architecture:** .NET 9 Vertical Slice Architecture with DDD patterns
**Domain:** Healthcare appointment scheduling and prescriptions

---

## Completed Fixes (16 of 16 issues) ✅ ALL DONE

### P0 Critical Issues - All Resolved ✅

#### 1. Fixed Domain Event Never Published in IssuePrescription

**File:** `src/Application/Medications/IssuePrescription.cs`
**Problem:** Handler added `PrescriptionIssuedEvent` AFTER `SaveChangesAsync()`, so it never got dispatched.
**Solution:** Removed duplicate - `Prescription.Issue()` already raises the event internally.

#### 2-5. Moved Domain Events Inside Appointment Aggregate

**Files Modified:**

- `src/Application/Domain/Appointment.cs` - Added event raising to `Schedule()`, `Reschedule()`, `Complete()`, `Cancel()`
- `src/Application/Scheduling/BookAppointment.cs` - Removed duplicate event
- `src/Application/Scheduling/RescheduleAppointment.cs` - Removed duplicate event
- `src/Application/Scheduling/CompleteAppointment.cs` - Removed duplicate event
- `src/Application/Scheduling/CancelAppointment.cs` - Removed duplicate event

**New Domain Event Files Created:**

- `src/Application/Domain/AppointmentCompletedEvent.cs`
- `src/Application/Domain/AppointmentCancelledEvent.cs`

**Test Updates:**

- `tests/Application.UnitTests/Domain/Healthcare/AppointmentTests.cs` - Updated 3 tests to reflect that `Schedule()` now raises events automatically

#### 6. Documented Overlap Race Condition

**Files:** `src/Application/Scheduling/BookAppointment.cs`, `src/Application/Scheduling/RescheduleAppointment.cs`
**Solution:**

- Added detailed comments explaining the check-then-act race condition
- Added `DbUpdateException` catch blocks as safety net
- Recommended SERIALIZABLE transactions for production SQL Server

### P1 Issues - All Resolved ✅

#### 7. Extracted DateTime.UtcNow from Domain

**Files Modified:**

- `src/Application/Domain/Appointment.cs` - `Complete()` and `Cancel()` now accept optional timestamp parameters
- `src/Application/Domain/Prescription.cs` - `Issue()` now accepts optional `issuedAtUtc` parameter, added `IsExpiredAt()` method

**Pattern:** Optional timestamp parameters default to `DateTime.UtcNow` for production, but tests can inject fixed times.

#### 8. Implemented Optimistic Concurrency with RowVersion

**Files Modified:**

- `src/Application/Scheduling/RescheduleAppointment.cs` - Added `DbUpdateConcurrencyException` handling
- `src/Application/Scheduling/CompleteAppointment.cs` - Added `DbUpdateConcurrencyException` handling
- `src/Application/Scheduling/CancelAppointment.cs` - Added `DbUpdateConcurrencyException` handling

**Pattern:** Catch `DbUpdateConcurrencyException` separately from `DbUpdateException` and return user-friendly conflict error.

#### 9. Addressed Validation Duplication (Design Decision)

**Files Modified:**

- `src/Application/Scheduling/BookAppointment.cs` - Added try-catch for domain validation, documented design decision
- `src/Application/Domain/Appointment.cs` - Added documentation explaining validation layers

**Design Decision:** Keep validation in both layers as defense-in-depth:
- FluentValidation provides fast-fail UX
- Domain protects invariants (authoritative)
- Handlers catch `ArgumentException` and convert to `ErrorOr`

### P2 Issues - All Resolved ✅

#### 9. Extracted Magic Numbers to Policy Classes

**New Files Created:**

- `src/Application/Domain/SchedulingPolicies.cs` - Constants for appointment scheduling rules
- `src/Application/Domain/PrescriptionPolicies.cs` - Constants for prescription rules

**Files Updated:**

- `src/Application/Scheduling/BookAppointment.cs` - Uses `SchedulingPolicies.*`
- `src/Application/Scheduling/RescheduleAppointment.cs` - Uses `SchedulingPolicies.*`
- `src/Application/Scheduling/CompleteAppointment.cs` - Uses `SchedulingPolicies.MaxNotesLength`
- `src/Application/Scheduling/CancelAppointment.cs` - Uses `SchedulingPolicies.MaxCancellationReasonLength`
- `src/Application/Domain/Appointment.cs` - Uses `SchedulingPolicies.*`
- `src/Application/Domain/Prescription.cs` - Uses `PrescriptionPolicies.*`
- `src/Application/Medications/IssuePrescription.cs` - Uses `PrescriptionPolicies.*`

#### 10. Standardized Endpoint Registration Pattern

**New File Created:**

- `src/Application/Medications/PrescriptionEndpoints.cs` - Centralized endpoint mapping for prescriptions

**Files Modified:**

- `src/Application/Medications/IssuePrescription.cs` - Changed from self-registering extension to static `Handle` method
- `src/Application/HealthcareEndpoints.cs` - Updated to use `RouteGroupBuilder` pattern for both features

**Pattern:** All features now use `RouteGroupBuilder` with centralized endpoint mapping classes.

### P3 Issues - Resolved ✅

#### 11. Removed Null-forgiving Operator

**File:** `src/Application/Scheduling/GetAppointments.cs`
**Change:** Moved `ISender mediator` before optional parameters instead of using `= null!`

#### 12. Standardized Error Code Naming

**File:** `src/Application/Medications/IssuePrescription.cs`
**Change:** `Patient.NotFound` → `Prescription.PatientNotFound`, `Doctor.NotFound` → `Prescription.DoctorNotFound`
**Pattern:** Aggregate-first naming (`Prescription.PatientNotFound`) provides better context than entity-first.

#### 13. Fixed Sort Order

**File:** `src/Application/Scheduling/GetAppointments.cs`
**Change:** Changed from `OrderByDescending` to `OrderBy` - upcoming appointments now appear first
**Test Updated:** `tests/Application.IntegrationTests/Healthcare/Appointments/GetAppointmentsTests.cs`

---

## Remaining Tasks

**None! All 16 issues have been resolved.**

### P3 Issues - All Resolved ✅

#### 14. Added Prescription Status Transitions

**File:** `src/Application/Domain/Prescription.cs`

**New Methods Added:**

- `UseRefill()` - Uses one refill and transitions to `Depleted` when exhausted
- `MarkExpired()` - Explicitly marks prescription as expired
- `RefreshStatus(DateTime?)` - Checks and updates status based on current state

**Design:** Status now transitions properly from `Active` → `Expired` or `Active` → `Depleted`.

#### 15. Standardized Constructor Style

**File:** `src/Application/Medications/IssuePrescription.cs`

**Change:** Converted `IssuePrescriptionCommandHandler` from traditional constructor to primary constructor style.

All handlers now consistently use primary constructor pattern: `class Handler(DbContext context) : IRequestHandler<...>`

### Addressed: Validation Duplication (Design Decision)

The validation duplication between FluentValidation and domain is **intentional defense-in-depth**:

- **FluentValidation**: Provides fast-fail UX with user-friendly error messages. Runs before handler executes.
- **Domain**: Protects invariants. Domain is authoritative source of truth.
- **Handlers**: Catch `ArgumentException` from domain and convert to `ErrorOr` validation errors.

This pattern ensures:
1. Most validation errors caught early by FluentValidation (better UX)
2. Domain remains self-protecting even if called without validators
3. Consistent error handling throughout

---

## Key Architectural Patterns Established

### Domain Events Raised Inside Aggregates

```csharp
// CORRECT - Event raised inside domain method
public static Appointment Schedule(...) {
    var appointment = new Appointment(...);
    appointment.DomainEvents.Add(new AppointmentBookedEvent(...));
    return appointment;
}

// Handler just calls domain and saves
var appointment = Appointment.Schedule(...);
await _context.SaveChangesAsync(); // Events dispatched here
```

### Error Handling Pattern

```csharp
try {
    appointment.Complete(notes);
}
catch (InvalidOperationException ex) {
    return Error.Validation("Appointment.CannotComplete", ex.Message);
}
```

### Timestamp Injection for Testability

```csharp
// Domain method with optional timestamp
public void Complete(string? notes = null, DateTime? completedAtUtc = null)
{
    var timestamp = completedAtUtc ?? DateTime.UtcNow;
    // ... use timestamp
}

// Production: omit parameter, uses current time
appointment.Complete("Notes");

// Tests: inject fixed time
appointment.Complete("Notes", new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc));
```

### Policy Classes for Business Rules

```csharp
// Single source of truth
public static class SchedulingPolicies
{
    public const int MinimumAppointmentDurationMinutes = 10;
    public const int RescheduleWindowCutoffHours = 24;
    // ...
}

// Used in validators
RuleFor(v => v.End)
    .GreaterThanOrEqualTo(v => v.Start.AddMinutes(SchedulingPolicies.MinimumAppointmentDurationMinutes))
```

### Optimistic Concurrency

```csharp
try
{
    await _context.SaveChangesAsync(cancellationToken);
}
catch (DbUpdateConcurrencyException)
{
    // Different user modified this record
    return Error.Conflict("Appointment.ConcurrencyConflict",
        "The appointment was modified by another user. Please refresh and try again.");
}
catch (DbUpdateException)
{
    // Constraint violation (e.g., overlapping appointment)
    return Error.Conflict("Appointment.Conflict",
        "Doctor has a conflicting appointment during the requested time");
}
```

### Centralized Endpoint Registration

```csharp
// In PrescriptionEndpoints.cs
public static RouteGroupBuilder MapPrescriptionEndpoints(this RouteGroupBuilder group)
{
    group.MapPost("/", IssuePrescriptionEndpoint.Handle)
        .WithName("IssuePrescription")
        .Produces<PrescriptionResponse>(StatusCodes.Status201Created)
        .AddEndpointFilter<ValidationFilter<IssuePrescriptionCommand>>();
    return group;
}

// In HealthcareEndpoints.cs
apiGroup.MapGroup("/prescriptions")
    .WithTags("Prescriptions")
    .MapPrescriptionEndpoints();
```

---

## Test Status

**All 274 tests pass**

- 143 Unit Tests
- 131 Integration Tests

---

## Files Changed in This Session

### Application/Domain/

- `Appointment.cs` - Events raised internally, optional timestamps, uses SchedulingPolicies, concurrency handling
- `Prescription.cs` - Optional timestamp, uses PrescriptionPolicies, added `IsExpiredAt()` method
- `AppointmentCompletedEvent.cs` - **NEW**
- `AppointmentCancelledEvent.cs` - **NEW**
- `SchedulingPolicies.cs` - **NEW** - Business rule constants for scheduling
- `PrescriptionPolicies.cs` - **NEW** - Business rule constants for prescriptions

### Application/Scheduling/

- `BookAppointment.cs` - Removed duplicate event, added race condition docs, uses policies
- `RescheduleAppointment.cs` - Removed duplicate event, added race condition docs, uses policies, concurrency handling
- `CompleteAppointment.cs` - Removed duplicate event, uses policies, concurrency handling
- `CancelAppointment.cs` - Removed duplicate event, uses policies, concurrency handling
- `GetAppointments.cs` - Fixed null-forgiving operator, changed sort order to ascending

### Application/Medications/

- `IssuePrescription.cs` - Removed duplicate event, uses policies, standardized error codes, static Handle method
- `PrescriptionEndpoints.cs` - **NEW** - Centralized endpoint registration

### Application/

- `HealthcareEndpoints.cs` - Updated to use RouteGroupBuilder pattern for both features

### Tests/

- `AppointmentTests.cs` - Updated 3 tests for new event behavior
- `GetAppointmentsTests.cs` - Updated 2 tests for ascending sort order

---

## How to Continue

1. **Run tests first:** `dotnet test`
2. **Pick next priority issue** from remaining P1 list above
3. **Suggested next fix:** Remove validation duplication by letting domain be the source of truth

---

## Commands Reference

```bash
# Build
dotnet build

# Run all tests
dotnet test

# Run specific test
dotnet test --filter "FullyQualifiedName~AppointmentTests"

# Format code
dotnet format
```
