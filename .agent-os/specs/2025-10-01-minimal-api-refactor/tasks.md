# Spec Tasks

These are the tasks to be completed for the spec detailed in @.agent-os/specs/2025-10-01-minimal-api-refactor/spec.md

> Created: 2025-10-01
> Status: Ready for Implementation

## Tasks

- [ ] 1. Create Error Handling Infrastructure for Minimal APIs
  - [ ] 1.1 Write tests for MinimalApiProblemHelper class
  - [ ] 1.2 Implement MinimalApiProblemHelper with ErrorOr to Problem Details mapping
  - [ ] 1.3 Add support for validation error collections
  - [ ] 1.4 Test Problem Details generation for all error types (400, 404, 409, 403, 500)
  - [ ] 1.5 Verify all error handling tests pass

- [ ] 2. Implement Validation Pipeline Filter
  - [ ] 2.1 Write tests for ValidationFilter<TRequest> endpoint filter
  - [ ] 2.2 Create ValidationFilter that resolves IValidator<TRequest> from DI
  - [ ] 2.3 Implement automatic validation execution before handler
  - [ ] 2.4 Map validation failures to Problem Details responses
  - [ ] 2.5 Test filter with existing BookAppointment and RescheduleAppointment validators
  - [ ] 2.6 Verify all validation tests pass

- [ ] 3. Migrate BookAppointment Endpoint to Minimal API
  - [ ] 3.1 Write integration tests for BookAppointment Minimal API endpoint
  - [ ] 3.2 Create AppointmentEndpoints static class with MapAppointmentEndpoints extension
  - [ ] 3.3 Implement POST /api/healthcare/appointments endpoint with MediatR integration
  - [ ] 3.4 Configure endpoint with validation filter and OpenAPI metadata
  - [ ] 3.5 Update Program.cs to register Healthcare endpoints via MapGroup
  - [ ] 3.6 Remove or deprecate BookAppointmentController
  - [ ] 3.7 Verify all BookAppointment tests pass with identical behavior

- [ ] 4. Migrate RescheduleAppointment Endpoint to Minimal API
  - [ ] 4.1 Write integration tests for RescheduleAppointment Minimal API endpoint
  - [ ] 4.2 Add RescheduleAppointment endpoint to AppointmentEndpoints class
  - [ ] 4.3 Implement route parameter binding with appointmentId validation
  - [ ] 4.4 Configure endpoint with validation filter and OpenAPI metadata
  - [ ] 4.5 Remove or deprecate RescheduleAppointmentController
  - [ ] 4.6 Verify all RescheduleAppointment tests pass with identical behavior

- [ ] 5. Documentation and Pattern Establishment
  - [ ] 5.1 Create example template for future Healthcare feature migrations
  - [ ] 5.2 Document Minimal API patterns in CLAUDE.md for consistency
  - [ ] 5.3 Verify Swagger/OpenAPI documentation generates correctly
  - [ ] 5.4 Run full test suite to ensure no regressions
  - [ ] 5.5 Create migration guide for remaining features if needed
