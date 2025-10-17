# Spec Tasks

## Tasks

- [x] 1. Remove Deprecated Integration Test Project
  - [x] 1.1 Verify current integration test project status and identify all files to remove
  - [x] 1.2 Delete tests/Application.IntegrationTests directory and all contents
  - [x] 1.3 Remove integration test project reference from solution file (was already removed)
  - [x] 1.4 Verify solution builds successfully after removal
  - [x] 1.5 Commit removal with clear commit message

- [ ] 2. Research .NET 9 Integration Testing Best Practices
  - [ ] 2.1 Research WebApplicationFactory patterns for .NET 9 and Minimal APIs
  - [ ] 2.2 Research test isolation strategies (database reset, transactions, parallel execution)
  - [ ] 2.3 Research vertical slice architecture integration testing approaches
  - [ ] 2.4 Research test data management patterns (builders, fixtures, seed data)
  - [ ] 2.5 Research FluentAssertions best practices for HTTP response validation
  - [ ] 2.6 Document findings in sub-specs/research-findings.md with code examples
  - [ ] 2.7 Identify recommended patterns to use in this project

- [ ] 3. Create New Integration Test Project Infrastructure
  - [ ] 3.1 Create new tests/Application.IntegrationTests project with .NET 9 target
  - [ ] 3.2 Add required NuGet packages (xUnit, WebApplicationFactory, FluentAssertions, InMemory DB)
  - [ ] 3.3 Add project references to src/Api and src/Application
  - [ ] 3.4 Implement CustomWebApplicationFactory<Program> with in-memory database configuration
  - [ ] 3.5 Create IntegrationTestBase abstract class with HttpClient setup and IAsyncLifetime
  - [ ] 3.6 Implement database reset logic for test isolation
  - [ ] 3.7 Create helper utilities (ResponseHelper, HttpClientExtensions)
  - [ ] 3.8 Verify basic infrastructure with a simple smoke test
  - [ ] 3.9 Verify all tests pass

- [ ] 4. Implement Test Data Builders and Helpers
  - [ ] 4.1 Create BookAppointmentTestDataBuilder with fluent API and sensible defaults
  - [ ] 4.2 Create RescheduleAppointmentTestDataBuilder with fluent API
  - [ ] 4.3 Create IssuePrescriptionTestDataBuilder with fluent API
  - [ ] 4.4 Create test seed data helper for patients and doctors with deterministic GUIDs
  - [ ] 4.5 Create ProblemDetails parsing utilities for error response validation
  - [ ] 4.6 Test data builders with sample usage to verify API
  - [ ] 4.7 Verify all tests pass

- [ ] 5. Implement BookAppointment Integration Tests
  - [ ] 5.1 Create Healthcare/Appointments/BookAppointmentTests.cs test class
  - [ ] 5.2 Implement happy path test: valid appointment returns 201 Created
  - [ ] 5.3 Implement validation test: start >= end returns 400 with validation errors
  - [ ] 5.4 Implement validation test: duration < 10 minutes returns 400
  - [ ] 5.5 Implement not found test: invalid patient ID returns 404
  - [ ] 5.6 Implement conflict test: overlapping doctor appointment returns 409
  - [ ] 5.7 Add additional edge case tests (boundary conditions, null handling)
  - [ ] 5.8 Verify all BookAppointment tests pass

- [ ] 6. Implement RescheduleAppointment Integration Tests
  - [ ] 6.1 Create Healthcare/Appointments/RescheduleAppointmentTests.cs test class
  - [ ] 6.2 Implement happy path test: valid reschedule returns 200 OK
  - [ ] 6.3 Implement validation test: reschedule within 24 hours returns 422
  - [ ] 6.4 Implement validation test: invalid new times return 400
  - [ ] 6.5 Implement not found test: invalid appointment ID returns 404
  - [ ] 6.6 Implement conflict test: new time overlaps doctor schedule returns 409
  - [ ] 6.7 Implement status check tests: cannot reschedule cancelled/completed appointments
  - [ ] 6.8 Verify all RescheduleAppointment tests pass

- [ ] 7. Implement IssuePrescription Integration Tests
  - [ ] 7.1 Create Healthcare/Prescriptions/IssuePrescriptionTests.cs test class
  - [ ] 7.2 Implement happy path test: valid prescription returns 201 Created
  - [ ] 7.3 Implement validation test: quantity out of range returns 400
  - [ ] 7.4 Implement validation test: refills > 12 returns 400
  - [ ] 7.5 Implement validation test: invalid duration returns 400
  - [ ] 7.6 Implement not found test: invalid patient ID returns 404
  - [ ] 7.7 Implement not found test: invalid doctor ID returns 404
  - [ ] 7.8 Verify all IssuePrescription tests pass

- [ ] 8. Documentation and Finalization
  - [ ] 8.1 Update CLAUDE.md with integration testing section and patterns
  - [ ] 8.2 Add examples of test data builders and usage to documentation
  - [ ] 8.3 Document test isolation strategy and database reset approach
  - [ ] 8.4 Add inline comments to key infrastructure classes (WebApplicationFactory, TestBase)
  - [ ] 8.5 Create README.md in integration test project with quick start guide
  - [ ] 8.6 Run full test suite (unit + integration) and verify all pass
  - [ ] 8.7 Verify tests can run in parallel without conflicts
  - [ ] 8.8 Mark specification as complete in tasks.md

## Implementation Notes

### Task 1: Removal
- Check git history to understand why current integration tests are deprecated
- Ensure no valuable test patterns are lost before deletion
- Clean removal to avoid broken references

### Task 2: Research
- Focus on .NET 9 and Minimal API specific patterns (not older .NET Core approaches)
- Look for vertical slice architecture testing examples
- Document specific code patterns to adopt, not just conceptual understanding
- Save research findings for future reference

### Task 3: Infrastructure
- CustomWebApplicationFactory should force in-memory database for all tests
- IntegrationTestBase should handle cleanup in DisposeAsync
- Consider using xUnit collections for shared context if needed
- Ensure deterministic seed data with known GUIDs matching HTTP request files

### Task 4: Test Data Builders
- Follow builder pattern with fluent API (WithPatientId(), WithStartTime(), etc.)
- Provide sensible defaults so tests only specify what they care about
- Make builders immutable (return new instance on each With method)
- Align default data with existing seed data for consistency

### Tasks 5-7: Endpoint Tests
- Each test should follow Arrange-Act-Assert pattern
- Use FluentAssertions for readable assertions
- Validate HTTP status code, response body structure, and error messages
- For POST operations, verify database state after operation
- Test names should clearly describe scenario and expected outcome

### Task 8: Documentation
- Include code snippets showing typical test structure
- Explain when to use integration tests vs unit tests
- Document how to add tests for new features
- Provide troubleshooting guide for common test issues
