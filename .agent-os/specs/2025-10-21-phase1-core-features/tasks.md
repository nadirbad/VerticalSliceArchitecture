# Phase 1 Core Features - Task List

**Created:** 2025-10-21
**Status:** Not Started
**Target Completion:** 2025-11-15 (4 weeks)

## Task Overview

This specification covers 5 features to complete Phase 1 of the healthcare management system:

- [ ] **Feature 1:** Complete Appointment (`S` - 2-3 days)
- [ ] **Feature 2:** Cancel Appointment (`S` - 2-3 days)
- [ ] **Feature 3:** Get Appointments (`M` - 1 week)
- [ ] **Feature 4:** Request Medication Refill (`L` - 2 weeks)
- [ ] **Feature 5:** Approve/Deny Refill Request (`M` - 1 week)

**Total Estimated Effort:** 3-4 weeks

## Feature 1: Complete Appointment

**Priority:** High | **Effort:** S (2-3 days) | **Status:** Not Started

### Implementation Tasks

- [ ] 1.1 Add `CompletedUtc` property to Appointment entity
- [ ] 1.2 Implement `Complete()` domain method with validation
- [ ] 1.3 Write unit tests for Complete domain method (7 tests)
- [ ] 1.4 Create `CompleteAppointmentCommand` record
- [ ] 1.5 Create `CompleteAppointmentCommandValidator`
- [ ] 1.6 Write validator unit tests (5 tests)
- [ ] 1.7 Implement `CompleteAppointmentCommandHandler`
- [ ] 1.8 Write handler unit tests (5 tests)
- [ ] 1.9 Create `AppointmentCompletedEvent` domain event
- [ ] 1.10 Configure Minimal API endpoint in AppointmentEndpoints
- [ ] 1.11 Create database migration for CompletedUtc column
- [ ] 1.12 Run migration and verify schema
- [ ] 1.13 Write integration tests (10-12 tests)
  - [ ] Happy path - complete scheduled appointment
  - [ ] Complete with notes
  - [ ] Complete without notes
  - [ ] Idempotent - complete twice
  - [ ] Cannot complete cancelled
  - [ ] Not found scenario
  - [ ] Notes too long validation
  - [ ] Database verification
  - [ ] Rescheduled appointment can be completed
  - [ ] Timestamp verification
- [ ] 1.14 Create HTTP request file (`requests/Healthcare/Appointments/CompleteAppointment.http`)
- [ ] 1.15 Update CLAUDE.md with Complete example
- [ ] 1.16 Update integration test README if needed
- [ ] 1.17 Code review and final testing
- [ ] 1.18 Merge and mark complete

**Acceptance Criteria:**
- ✅ All unit tests passing (15+ tests)
- ✅ All integration tests passing (10-12 tests)
- ✅ HTTP request examples created
- ✅ Documentation updated

---

## Feature 2: Cancel Appointment

**Priority:** High | **Effort:** S (2-3 days) | **Status:** Not Started

### Implementation Tasks

- [ ] 2.1 Add `CancelledUtc` and `CancellationReason` properties to Appointment entity
- [ ] 2.2 Implement `Cancel()` domain method with validation
- [ ] 2.3 Write unit tests for Cancel domain method (9 tests)
- [ ] 2.4 Create `CancelAppointmentCommand` record
- [ ] 2.5 Create `CancelAppointmentCommandValidator`
- [ ] 2.6 Write validator unit tests (7 tests)
- [ ] 2.7 Implement `CancelAppointmentCommandHandler`
- [ ] 2.8 Write handler unit tests (5 tests)
- [ ] 2.9 Create `AppointmentCancelledEvent` domain event
- [ ] 2.10 Configure Minimal API endpoint in AppointmentEndpoints
- [ ] 2.11 Create database migration for cancellation columns
- [ ] 2.12 Run migration and verify schema
- [ ] 2.13 Write integration tests (10-12 tests)
  - [ ] Happy path - cancel scheduled appointment
  - [ ] Cancel with reason
  - [ ] Idempotent - cancel twice
  - [ ] Cannot cancel completed
  - [ ] Not found scenario
  - [ ] Empty reason validation
  - [ ] Reason too long validation
  - [ ] Database verification
  - [ ] Rescheduled appointment can be cancelled
  - [ ] Last-minute cancellation works
  - [ ] Event data verification
- [ ] 2.14 Create HTTP request file (`requests/Healthcare/Appointments/CancelAppointment.http`)
- [ ] 2.15 Update CLAUDE.md with Cancel example
- [ ] 2.16 Create appointment state machine diagram
- [ ] 2.17 Code review and final testing
- [ ] 2.18 Merge and mark complete

**Acceptance Criteria:**
- ✅ All unit tests passing (20+ tests)
- ✅ All integration tests passing (10-12 tests)
- ✅ HTTP request examples created
- ✅ State machine documented

---

## Feature 3: Get Appointments

**Priority:** High | **Effort:** M (1 week) | **Status:** Not Started

### Implementation Tasks

- [ ] 3.1 Create `AppointmentDto` record
- [ ] 3.2 Create `PaginatedResult<T>` and `PaginationMetadata` records
- [ ] 3.3 Create `GetAppointmentsQuery` record
- [ ] 3.4 Create `GetAppointmentsQueryValidator`
- [ ] 3.5 Write validator unit tests (8 tests)
- [ ] 3.6 Implement `GetAppointmentsQueryHandler` with filtering
- [ ] 3.7 Add patient ID filter logic
- [ ] 3.8 Add doctor ID filter logic
- [ ] 3.9 Add date range filter logic
- [ ] 3.10 Add status filter logic
- [ ] 3.11 Implement sorting (StartTime, CreatedUtc)
- [ ] 3.12 Implement pagination logic
- [ ] 3.13 Write handler unit tests (10 tests)
- [ ] 3.14 Configure Minimal API GET endpoint
- [ ] 3.15 Add database indexes for query performance
  - [ ] IX_Appointments_PatientId
  - [ ] IX_Appointments_DoctorId
  - [ ] IX_Appointments_StartUtc
  - [ ] IX_Appointments_Status
  - [ ] IX_Appointments_PatientId_StartUtc (composite)
  - [ ] IX_Appointments_DoctorId_StartUtc (composite)
- [ ] 3.16 Create database migration for indexes
- [ ] 3.17 Write integration tests (15-20 tests)
  - [ ] No filters - all appointments
  - [ ] Filter by patient
  - [ ] Filter by doctor
  - [ ] Filter by date range
  - [ ] Filter by each status
  - [ ] Multiple filters combined
  - [ ] Pagination first/middle/last page
  - [ ] Page size limits
  - [ ] Sort ascending/descending
  - [ ] Sort by different fields
  - [ ] Empty results
  - [ ] Patient/doctor details included
  - [ ] All status fields populated
  - [ ] Invalid parameters
  - [ ] Pagination metadata accuracy
- [ ] 3.18 Create HTTP request file with 10+ examples
- [ ] 3.19 Performance test with large dataset (1000+ appointments)
- [ ] 3.20 Update CLAUDE.md with query pattern example
- [ ] 3.21 Document best practices for VSA queries
- [ ] 3.22 Code review and final testing
- [ ] 3.23 Merge and mark complete

**Acceptance Criteria:**
- ✅ All unit tests passing (18+ tests)
- ✅ All integration tests passing (15-20 tests)
- ✅ Performance < 200ms for complex queries
- ✅ HTTP request examples cover all scenarios
- ✅ Indexes improve query performance

---

## Feature 4: Request Medication Refill

**Priority:** Medium | **Effort:** L (2 weeks) | **Status:** Not Started

### Implementation Tasks

#### Week 1: Entity & Domain Model

- [ ] 4.1 Create `RefillRequestStatus` enum (Pending, Approved, Denied)
- [ ] 4.2 Create `RefillRequest` entity with all properties
- [ ] 4.3 Configure EF Core entity mappings
- [ ] 4.4 Add navigation properties to Prescription
- [ ] 4.5 Create database migration for RefillRequest table
- [ ] 4.6 Run migration and verify schema
- [ ] 4.7 Add `RequestRefill()` method to Prescription domain entity
- [ ] 4.8 Write unit tests for RequestRefill domain method (8 tests)
- [ ] 4.9 Create `MedicationRefillRequestedEvent` domain event
- [ ] 4.10 Create `RequestRefillCommand` record
- [ ] 4.11 Create `RequestRefillCommandValidator`
- [ ] 4.12 Write validator unit tests (6 tests)

#### Week 2: Handler & Testing

- [ ] 4.13 Implement `RequestRefillCommandHandler`
  - [ ] Load prescription with refill requests
  - [ ] Validate prescription exists
  - [ ] Check expiration
  - [ ] Check refills remaining
  - [ ] Check no pending request exists
  - [ ] Create RefillRequest entity
  - [ ] Raise domain event
- [ ] 4.14 Write handler unit tests (10 tests)
- [ ] 4.15 Configure Minimal API endpoint
- [ ] 4.16 Write integration tests (15-18 tests)
  - [ ] Happy path - request refill
  - [ ] Prescription expired
  - [ ] No refills remaining
  - [ ] Prescription not found
  - [ ] Duplicate pending request
  - [ ] Notes too long
  - [ ] Request notes optional
  - [ ] Database verification
  - [ ] Patient ID matches prescription
  - [ ] Status is Pending
  - [ ] Timestamp verification
  - [ ] Event raised
  - [ ] Multiple prescriptions handled separately
  - [ ] Edge cases
- [ ] 4.17 Create HTTP request file
- [ ] 4.18 Update CLAUDE.md with workflow documentation
- [ ] 4.19 Code review and final testing
- [ ] 4.20 Merge and mark complete

**Acceptance Criteria:**
- ✅ RefillRequest entity persists correctly
- ✅ All validation rules enforced
- ✅ All unit tests passing (24+ tests)
- ✅ All integration tests passing (15-18 tests)
- ✅ Documentation covers refill workflow

---

## Feature 5: Approve/Deny Refill Request

**Priority:** Medium | **Effort:** M (1 week) | **Status:** Not Started

### Implementation Tasks

- [ ] 5.1 Add `Approve()` domain method to RefillRequest
- [ ] 5.2 Add `Deny()` domain method to RefillRequest
- [ ] 5.3 Write unit tests for Approve method (5 tests)
- [ ] 5.4 Write unit tests for Deny method (6 tests)
- [ ] 5.5 Create `ApproveRefillRequestCommand` record
- [ ] 5.6 Create `ApproveRefillRequestCommandValidator`
- [ ] 5.7 Write approve validator tests (4 tests)
- [ ] 5.8 Implement `ApproveRefillRequestCommandHandler`
  - [ ] Load refill request
  - [ ] Validate status is Pending
  - [ ] Call Approve domain method
  - [ ] Update prescription RefillsUsed
  - [ ] Raise MedicationRefillApproved event
- [ ] 5.9 Write approve handler unit tests (6 tests)
- [ ] 5.10 Create `DenyRefillRequestCommand` record
- [ ] 5.11 Create `DenyRefillRequestCommandValidator`
- [ ] 5.12 Write deny validator tests (5 tests)
- [ ] 5.13 Implement `DenyRefillRequestCommandHandler`
  - [ ] Load refill request
  - [ ] Validate status is Pending
  - [ ] Call Deny domain method
  - [ ] Raise MedicationRefillDenied event
- [ ] 5.14 Write deny handler unit tests (6 tests)
- [ ] 5.15 Create `MedicationRefillApprovedEvent` domain event
- [ ] 5.16 Create `MedicationRefillDeniedEvent` domain event
- [ ] 5.17 Configure both Minimal API endpoints
- [ ] 5.18 Write integration tests for Approve (6-8 tests)
  - [ ] Happy path
  - [ ] Already processed
  - [ ] Not found
  - [ ] Prescription refills updated
  - [ ] Database verification
  - [ ] Event raised
- [ ] 5.19 Write integration tests for Deny (6-7 tests)
  - [ ] Happy path
  - [ ] Empty reason validation
  - [ ] Already processed
  - [ ] Not found
  - [ ] Database verification
  - [ ] Event raised
- [ ] 5.20 Create HTTP request files for both operations
- [ ] 5.21 Update CLAUDE.md with complete refill workflow
- [ ] 5.22 Create sequence diagram for refill process
- [ ] 5.23 Code review and final testing
- [ ] 5.24 Merge and mark complete

**Acceptance Criteria:**
- ✅ Approve and Deny operations work correctly
- ✅ Prescription refills updated on approval
- ✅ All unit tests passing (32+ tests)
- ✅ All integration tests passing (12-15 tests)
- ✅ Complete workflow documented

---

## Cross-Feature Tasks

### Documentation

- [ ] D.1 Update CLAUDE.md with all 5 features
- [ ] D.2 Add integration test examples for new features
- [ ] D.3 Update Integration Test README with new test counts
- [ ] D.4 Create appointment lifecycle state diagram
- [ ] D.5 Create prescription refill workflow sequence diagram
- [ ] D.6 Document query patterns in VSA
- [ ] D.7 Add troubleshooting guide updates

### Testing

- [ ] T.1 Run full test suite (estimated 280+ total tests)
- [ ] T.2 Verify all tests pass
- [ ] T.3 Check test coverage metrics
- [ ] T.4 Performance test all query endpoints
- [ ] T.5 Verify tests run in parallel without issues

### Roadmap & Project Management

- [ ] R.1 Update roadmap.md Phase 1 progress to 100%
- [ ] R.2 Add completion dates for each feature
- [ ] R.3 Update recent updates section
- [ ] R.4 Mark all Phase 1 features as complete
- [ ] R.5 Prepare Phase 2 planning

### Demo & Review

- [ ] Demo.1 Prepare demo script showing complete workflows
- [ ] Demo.2 Book → Reschedule → Complete workflow
- [ ] Demo.3 Book → Cancel workflow
- [ ] Demo.4 Query appointments with various filters
- [ ] Demo.5 Request → Approve refill workflow
- [ ] Demo.6 Request → Deny refill workflow
- [ ] Demo.7 Record demo or create walkthrough document

---

## Progress Tracking

### Week 1 (Days 1-5)
**Target:** Complete Appointment + Cancel Appointment

- [ ] Complete Appointment implemented
- [ ] Cancel Appointment implemented
- [ ] All tests passing for both features
- [ ] HTTP requests created
- [ ] Documentation updated

### Week 2 (Days 6-12)
**Target:** Get Appointments

- [ ] Query handler with all filters implemented
- [ ] Pagination working correctly
- [ ] Database indexes added
- [ ] All tests passing
- [ ] Performance validated
- [ ] Documentation updated

### Week 3 (Days 13-19)
**Target:** Request Medication Refill

- [ ] RefillRequest entity created
- [ ] Domain logic implemented
- [ ] Handler with all validations
- [ ] All tests passing
- [ ] Documentation updated

### Week 4 (Days 20-26)
**Target:** Approve/Deny Refill + Final Polish

- [ ] Approve/Deny operations implemented
- [ ] Full refill workflow working
- [ ] All tests passing
- [ ] Documentation complete
- [ ] Demo prepared
- [ ] Phase 1 marked complete

---

## Risk Mitigation

### Technical Risks

**Risk:** Query performance with large datasets
- **Mitigation:** Add indexes early, performance test with 1000+ records

**Risk:** RefillRequest entity complexity
- **Mitigation:** Start with simple implementation, iterate

**Risk:** Test suite becoming slow
- **Mitigation:** Keep tests focused, use test data builders efficiently

### Scope Risks

**Risk:** Feature creep during implementation
- **Mitigation:** Stick to specs, defer enhancements to Phase 2

**Risk:** Underestimated effort for refill workflow
- **Mitigation:** Break into smaller tasks, validate as you go

---

## Success Metrics

### Code Quality
- ✅ All tests passing (target: 280+ total tests)
- ✅ No code style violations
- ✅ Code review completed for all features

### Performance
- ✅ Query endpoints < 200ms
- ✅ Command endpoints < 100ms
- ✅ All tests run in < 2 seconds

### Documentation
- ✅ CLAUDE.md updated with examples
- ✅ HTTP request files complete
- ✅ Workflows documented with diagrams

### Completion
- ✅ All 5 features implemented
- ✅ Phase 1 marked 100% complete
- ✅ Demo prepared and validated

---

## Next Steps After Completion

1. **Update roadmap** - Mark Phase 1 complete, update dates
2. **Demo** - Record walkthrough or present to team
3. **Blog post** - Write about VSA approach and learnings
4. **Phase 2 planning** - Review remaining features, prioritize
5. **Community feedback** - Share progress, gather input
