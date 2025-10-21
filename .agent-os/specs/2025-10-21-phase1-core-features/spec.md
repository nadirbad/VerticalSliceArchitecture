# Phase 1 Core Features - Specification

**Created:** 2025-10-21
**Status:** Draft
**Priority:** High
**Effort:** L (2-3 weeks total)

## Overview

Complete the remaining Phase 1 core healthcare features to provide full CRUD operations for appointments and prescriptions. These features are essential for a functional healthcare management system and demonstrate the vertical slice architecture approach across common scenarios.

## Goals

1. **Complete Appointment Lifecycle** - Enable full state management (Book → Reschedule → Complete/Cancel)
2. **Query Capabilities** - Provide filtering and pagination for appointments and prescriptions
3. **Prescription Refill Workflow** - Implement end-to-end refill request and approval process
4. **Demonstrate VSA Patterns** - Show query patterns, state transitions, and workflow features

## Success Criteria

- ✅ All appointment states can be reached through business operations
- ✅ Appointments can be queried with filtering, pagination, and sorting
- ✅ Prescription refill workflow is complete (request → approve/deny)
- ✅ All features have comprehensive unit and integration tests
- ✅ HTTP request files demonstrate all scenarios
- ✅ Documentation updated in CLAUDE.md
- ✅ Phase 1 marked as 100% complete in roadmap

## Features

This specification covers 5 features organized by complexity and priority:

### High Priority (Week 1-2)

1. **[Complete Appointment](complete-appointment/spec.md)** (`S` - 2-3 days)
   - Mark appointments as completed
   - Capture completion notes
   - Status validation and state transitions
   - Integration with appointment lifecycle

2. **[Cancel Appointment](cancel-appointment/spec.md)** (`S` - 2-3 days)
   - Cancel scheduled or rescheduled appointments
   - Capture cancellation reason
   - Status validation and business rules
   - Domain event for notifications

3. **[Get Appointments](get-appointments/spec.md)** (`M` - 1 week)
   - Query with filtering (patient, doctor, date range, status)
   - Pagination and sorting
   - Include related entities (patient, doctor details)
   - Demonstrate CQRS-lite pattern in VSA

### Medium Priority (Week 3-4)

4. **[Request Medication Refill](request-refill/spec.md)** (`L` - 2 weeks)
   - Patients request prescription refills
   - Validate prescription exists and not expired
   - Check refills remaining
   - Create RefillRequest entity
   - Domain event for doctor notification

5. **[Approve/Deny Refill Request](approve-deny-refill/spec.md)** (`M` - 1 week)
   - Doctors review pending refill requests
   - Approve: increment prescription refills used
   - Deny: capture reason
   - Status validation and idempotency
   - Domain events for patient notification

## Architecture Approach

### Vertical Slice Organization

Each feature will follow the established pattern:

```
src/Application/Features/Healthcare/
├── Appointments/
│   ├── CompleteAppointment.cs      # Complete appointment endpoint + handler
│   ├── CancelAppointment.cs        # Cancel appointment endpoint + handler
│   └── GetAppointments.cs          # Query appointments endpoint + handler
└── Prescriptions/
    ├── RequestRefill.cs             # Request refill endpoint + handler
    └── ProcessRefillRequest.cs      # Approve/Deny refill endpoint + handler
```

### Domain Models

**New Entity Required:**
- `RefillRequest` - Tracks medication refill requests with status workflow

**Enhanced Entities:**
- `Appointment` - Add `Complete()` and `Cancel()` methods
- `Prescription` - Add `RequestRefill()` and `ProcessRefill()` methods

### Testing Strategy

**Unit Tests:**
- Domain method tests (Complete, Cancel, etc.)
- Validator tests with boundary conditions
- Handler tests with mocked dependencies
- Business rule validation

**Integration Tests:**
- Full HTTP endpoint testing
- Database state verification
- Validation error scenarios
- Conflict detection
- Status transition workflows

**Target Coverage:**
- Each feature: 15-25 integration tests
- Domain methods: 10-15 unit tests per entity
- Validators: 5-10 tests per validator

## Technical Considerations

### Database Changes

**Migrations Required:**
- RefillRequest table with relationships to Prescription and Patient
- Indexes on common query fields (Status, PatientId, DoctorId, AppointmentDate)

### Domain Events

**New Events:**
- `AppointmentCompleted` - Raised when appointment marked complete
- `AppointmentCancelled` - Raised when appointment cancelled
- `MedicationRefillRequested` - Raised when patient requests refill
- `MedicationRefillApproved` - Raised when doctor approves refill
- `MedicationRefillDenied` - Raised when doctor denies refill

### Error Handling

All features use `ErrorOr<T>` pattern with appropriate error types:
- **Validation errors** (400) - Invalid input, business rule violations
- **Not Found** (404) - Entity doesn't exist
- **Conflict** (409) - State transition not allowed
- **Unprocessable Entity** (422) - Valid request but can't be processed due to business rules

## Implementation Order

### Recommended Sequence

1. **Complete Appointment** (Days 1-3)
   - Simple state transition
   - Builds on existing appointment patterns
   - Quick win to build momentum

2. **Cancel Appointment** (Days 4-6)
   - Similar to Complete
   - Completes appointment state machine
   - Enables full lifecycle testing

3. **Get Appointments** (Days 7-13)
   - More complex with filtering/pagination
   - Demonstrates query patterns
   - Enables frontend development

4. **Request Medication Refill** (Days 14-23)
   - New entity and workflow
   - Complex feature with multiple validations
   - Foundation for approval workflow

5. **Approve/Deny Refill Request** (Days 24-28)
   - Completes refill workflow
   - Depends on Request feature
   - Demonstrates multi-step processes

### Parallel Work Opportunities

- HTTP request files can be created alongside implementation
- Documentation updates can happen as features complete
- Integration tests can be written by different developer

## Dependencies

### External Dependencies
- None - all features use existing infrastructure

### Internal Dependencies
- ✅ Integration test infrastructure (completed 2025-10-21)
- ✅ Minimal API infrastructure (completed 2025-10-01)
- ✅ Domain event dispatching (existing)
- ✅ Validation pipeline (existing)

### Feature Dependencies
- Approve/Deny Refill depends on Request Refill entity
- Get Appointments independent of state transitions
- Complete/Cancel can be implemented in parallel

## Risks and Mitigations

### Risk: Scope Creep
**Mitigation:** Stick to spec, defer enhancements to Phase 2

### Risk: Complex Querying Performance
**Mitigation:** Start simple, add indexes, consider caching if needed

### Risk: Refill Workflow Complexity
**Mitigation:** Implement in phases, validate with unit tests first

### Risk: State Transition Edge Cases
**Mitigation:** Comprehensive test coverage for all transitions

## Testing Requirements

### Minimum Test Coverage

**Per Feature:**
- Happy path: ✅ Success scenario
- Validation: ✅ All validation rules tested
- Not Found: ✅ Non-existent entities
- Conflicts: ✅ State transition violations
- Edge Cases: ✅ Boundary conditions, null handling

**Integration Test Goals:**
- Complete Appointment: 10-12 tests
- Cancel Appointment: 10-12 tests
- Get Appointments: 15-20 tests
- Request Refill: 15-18 tests
- Approve/Deny Refill: 12-15 tests

**Total:** ~60-75 new integration tests

## Documentation Requirements

Each feature requires:
- ✅ Inline code comments for complex business logic
- ✅ HTTP request file with happy path and error scenarios
- ✅ Update to CLAUDE.md with examples
- ✅ Integration test documentation in test README
- ✅ Roadmap update marking features complete

## Acceptance Criteria

### Feature Completion Checklist

For each feature:
- [ ] Domain method implemented with business logic
- [ ] Validator created with all rules
- [ ] MediatR handler implemented
- [ ] Minimal API endpoint configured
- [ ] Domain events raised appropriately
- [ ] Unit tests written and passing
- [ ] Integration tests written and passing
- [ ] HTTP request file created
- [ ] Documentation updated
- [ ] Code review completed

### Phase 1 Completion

- [ ] All 5 features implemented
- [ ] All tests passing (estimated 200+ total tests)
- [ ] Documentation complete
- [ ] Roadmap updated to 100% Phase 1
- [ ] Demo prepared showing full workflows

## Timeline

### Aggressive Timeline (3 weeks)
- **Week 1:** Complete + Cancel Appointment
- **Week 2:** Get Appointments
- **Week 3:** Request Refill + Approve/Deny Refill

### Comfortable Timeline (4 weeks)
- **Week 1:** Complete Appointment
- **Week 2:** Cancel Appointment + Get Appointments (start)
- **Week 3:** Get Appointments (finish) + Request Refill (start)
- **Week 4:** Request Refill (finish) + Approve/Deny Refill

## Next Steps

1. Review and approve this specification
2. Create detailed task lists in individual feature specs
3. Begin implementation starting with Complete Appointment
4. Set up tracking for test coverage metrics
5. Schedule demo/review after each feature completion

## References

- [Roadmap](../../product/roadmap.md) - Overall project roadmap
- [Integration Test Spec](../2025-10-17-healthcare-integration-tests/spec.md) - Test infrastructure details
- [Minimal API Refactor](../2025-10-01-minimal-api-refactor/spec.md) - API patterns
- [CLAUDE.md](../../../CLAUDE.md) - Development guidelines
