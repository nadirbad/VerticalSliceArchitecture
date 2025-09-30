# Spec Tasks

## Tasks

- [x] 1. Implement Domain Event for Appointment Rescheduling
  - [x] 1.1 Write tests for AppointmentRescheduledEvent structure
  - [x] 1.2 Create AppointmentRescheduledEvent class in Domain/Healthcare/Events
  - [x] 1.3 Create placeholder event handler (AppointmentRescheduledEventHandler)
  - [x] 1.4 Verify event is properly structured with all required properties
  - [x] 1.5 Verify all tests pass

- [ ] 2. Implement Reschedule Appointment Feature Slice
  - [ ] 2.1 Write unit tests for RescheduleAppointmentCommandValidator (all validation rules)
  - [ ] 2.2 Write integration tests for happy path, 24-hour rule, conflicts, and error scenarios
  - [ ] 2.3 Create RescheduleAppointment.cs feature file with Controller, Command, Result DTO, Validator, and Handler
  - [ ] 2.4 Implement command validator with FluentValidation rules (min/max duration, advance notice, reason length)
  - [ ] 2.5 Implement command handler with business logic (24-hour check, conflict detection, domain method call, event raising)
  - [ ] 2.6 Verify all unit tests pass
  - [ ] 2.7 Verify all integration tests pass
  - [ ] 2.8 Verify all existing tests still pass

- [ ] 3. Create HTTP Request File for Manual Testing
  - [ ] 3.1 Create requests/Healthcare/Appointments/RescheduleAppointment.http file
  - [ ] 3.2 Add test scenarios: happy path, 24h violation, conflict, not found, cancelled, completed, invalid duration
  - [ ] 3.3 Manually test each scenario to verify expected responses
  - [ ] 3.4 Verify all tests pass

## Implementation Notes

### Task 1: Domain Event
- Event class should inherit from DomainEvent
- Properties: AppointmentId, PreviousStartUtc, PreviousEndUtc, NewStartUtc, NewEndUtc
- Event handler logs the reschedule (placeholder for future notification implementation)

### Task 2: Feature Slice
- Single file at: src/Application/Features/Healthcare/Appointments/RescheduleAppointment.cs
- Controller inherits ApiControllerBase, route: POST /api/healthcare/appointments/{appointmentId}/reschedule
- Validator enforces: empty GUID check, start < end, 10min-8hr duration, 2hr advance notice, 512 char reason limit
- Handler performs: UTC normalization, appointment lookup, status check, 24-hour rule check, conflict detection, domain method call, event raising, persistence
- Error codes: NotFound (404), CannotRescheduleCancelled/Completed (400), RescheduleWindowClosed (422), Conflict (409)

### Task 3: Manual Testing
- Use existing appointments from BookAppointment.http as base data
- Test all success and failure scenarios
- Verify response codes and error messages match API spec