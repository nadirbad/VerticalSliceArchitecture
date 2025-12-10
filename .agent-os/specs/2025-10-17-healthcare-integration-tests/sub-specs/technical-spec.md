# Technical Specification

This is the technical specification for the spec detailed in @.agent-os/specs/2025-10-17-healthcare-integration-tests/spec.md

## Technical Requirements

### Test Project Configuration

- **Framework:** xUnit 2.6.x or later (aligned with existing unit tests)
- **Target Framework:** net9.0 (same as main application)
- **Test Host:** Microsoft.AspNetCore.Mvc.Testing (WebApplicationFactory)
- **Database:** In-memory database for test isolation (UseInMemoryDatabase = true)
- **Project References:** Reference src/Api and src/Application projects
- **Package References:**
  - xUnit
  - xUnit.runner.visualstudio
  - Microsoft.AspNetCore.Mvc.Testing
  - Microsoft.EntityFrameworkCore.InMemory
  - FluentAssertions (for readable assertions)
  - Microsoft.NET.Test.Sdk

### Test Infrastructure Components

1. **CustomWebApplicationFactory<TProgram>**
   - Inherit from WebApplicationFactory<Program>
   - Override ConfigureWebHost to configure test services
   - Force in-memory database usage for tests
   - Reset database between tests
   - Provide HttpClient factory for tests

2. **IntegrationTestBase (Abstract Base Class)**
   - Manage CustomWebApplicationFactory lifecycle
   - Provide HttpClient instance for tests
   - Implement IAsyncLifetime for xUnit
   - Reset database state before each test
   - Provide helper methods: GetAsync, PostAsync, PutAsync, DeleteAsync with typed responses

3. **TestDataBuilder Pattern**
   - BookAppointmentTestDataBuilder - fluent API for creating test appointment data
   - IssuePrescriptionTestDataBuilder - fluent API for creating test prescription data
   - Provide sensible defaults for all required fields
   - Allow overriding specific fields for test scenarios

4. **ResponseHelper Utilities**
   - Deserialize JSON responses with type safety
   - Extract ProblemDetails from error responses
   - Assert on status codes with descriptive error messages
   - Parse validation errors from 400 responses

### Test Organization

```
tests/
  Application.IntegrationTests/
    Application.IntegrationTests.csproj
    Infrastructure/
      CustomWebApplicationFactory.cs
      IntegrationTestBase.cs
    Helpers/
      TestDataBuilders/
        BookAppointmentTestDataBuilder.cs
        RescheduleAppointmentTestDataBuilder.cs
        IssuePrescriptionTestDataBuilder.cs
      ResponseHelper.cs
      HttpClientExtensions.cs
    Healthcare/
      Appointments/
        BookAppointmentTests.cs
        RescheduleAppointmentTests.cs
      Prescriptions/
        IssuePrescriptionTests.cs
```

### Test Coverage Requirements

**BookAppointmentTests.cs** (Minimum 5 tests):
- Happy path: valid appointment returns 201 Created with location header
- Validation error: start >= end returns 400 with validation errors
- Validation error: duration < 10 minutes returns 400
- Not found: invalid patient ID returns 404
- Conflict: overlapping doctor appointment returns 409

**RescheduleAppointmentTests.cs** (Minimum 5 tests):
- Happy path: valid reschedule returns 200 OK with updated times
- Validation error: appointment within 24 hours returns 422
- Validation error: new times invalid returns 400
- Not found: invalid appointment ID returns 404
- Conflict: new time overlaps with doctor schedule returns 409

**IssuePrescriptionTests.cs** (Minimum 5 tests):
- Happy path: valid prescription returns 201 Created
- Validation error: quantity out of range returns 400
- Validation error: refills > 12 returns 400
- Not found: invalid patient ID returns 404
- Not found: invalid doctor ID returns 404

### Test Isolation Strategy

1. **Database Reset:** Use EnsureDeleted() + EnsureCreated() in test setup
2. **Seed Data:** Re-seed test patients and doctors before each test
3. **Independent Tests:** Each test should create its own test data
4. **No Shared State:** Avoid static fields or shared test context
5. **Deterministic GUIDs:** Use known GUIDs for seed data matching HTTP request files

### Research Topics (To be completed in initial tasks)

Before implementation, research and document:
1. **.NET 9 WebApplicationFactory best practices** - Latest patterns, configuration approaches
2. **Test isolation strategies** - Database reset approaches, transaction rollback patterns
3. **Vertical slice architecture testing** - How to test feature slices end-to-end
4. **xUnit parallel execution** - Collection fixtures, test ordering considerations
5. **FluentAssertions for integration tests** - Best practices for HTTP response assertions

## External Dependencies

No new external dependencies required beyond standard Microsoft testing packages already used in the project.
