# Integration Tests

Comprehensive integration tests for the Vertical Slice Architecture Healthcare API.

## Quick Start

```bash
# Run all integration tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~BookAppointmentTests"

# Run specific test
dotnet test --filter "BookAppointment_WithValidData_Returns201Created"

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"
```

## Project Structure

```
Application.IntegrationTests/
├── Healthcare/
│   ├── Appointments/
│   │   ├── BookAppointmentTests.cs          # 15 tests for booking appointments
│   │   └── RescheduleAppointmentTests.cs    # 15 tests for rescheduling
│   └── Prescriptions/
│       └── IssuePrescriptionTests.cs        # 26 tests for issuing prescriptions
├── Infrastructure/
│   ├── CustomWebApplicationFactory.cs       # Test server configuration
│   ├── IntegrationTestBase.cs               # Base class with lifecycle management
│   └── InfrastructureSmokeTests.cs          # Basic infrastructure verification
├── Helpers/
│   ├── ResponseHelper.cs                    # ProblemDetails parsing utilities
│   └── HttpClientExtensions.cs              # HTTP client extensions
└── TestData/
    ├── TestSeedData.cs                      # Deterministic test data (GUIDs)
    ├── BookAppointmentTestDataBuilder.cs    # Appointment booking builder
    ├── RescheduleAppointmentTestDataBuilder.cs  # Appointment reschedule builder
    └── IssuePrescriptionTestDataBuilder.cs  # Prescription issuance builder
```

## Test Statistics

- **Total Tests**: 76 integration tests
- **BookAppointment**: 15 tests covering happy path, validation, conflicts, edge cases
- **RescheduleAppointment**: 15 tests covering rescheduling rules, 24-hour window, status checks
- **IssuePrescription**: 26 tests covering validation, boundaries, business rules
- **Infrastructure**: 20 tests for test infrastructure and helpers

## Writing Your First Test

### 1. Create Test Class

```csharp
using VerticalSliceArchitecture.Application.IntegrationTests.Infrastructure;

namespace VerticalSliceArchitecture.Application.IntegrationTests.Healthcare.MyFeature;

public class MyFeatureTests : IntegrationTestBase
{
    public MyFeatureTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task MyFeature_Scenario_ExpectedBehavior()
    {
        // Arrange
        // Act
        // Assert
    }
}
```

### 2. Use Test Data Builders

```csharp
// Create appointment with defaults
var command = new BookAppointmentTestDataBuilder().Build();

// Customize specific properties
var command = new BookAppointmentTestDataBuilder()
    .WithPatientId(TestSeedData.SecondPatientId)
    .WithDoctorId(TestSeedData.DefaultDoctorId)
    .WithStartTime(DateTimeOffset.UtcNow.AddDays(5))
    .WithDuration(30)
    .WithNotes("Follow-up visit")
    .Build();

// Use helper methods
var invalidCommand = new BookAppointmentTestDataBuilder()
    .WithTooShortDuration()  // Sets duration < 10 minutes
    .Build();

var notFoundCommand = new BookAppointmentTestDataBuilder()
    .WithNonExistentPatient()  // Uses TestSeedData.NonExistentId
    .Build();
```

### 3. Make HTTP Requests

```csharp
// POST request
var response = await Client.PostAsJsonAsync("/api/appointments", command);

// GET request
var response = await Client.GetAsync($"/api/appointments/{id}");

// PUT request
var response = await Client.PutAsJsonAsync($"/api/appointments/{id}", updateCommand);

// DELETE request
var response = await Client.DeleteAsync($"/api/appointments/{id}");
```

### 4. Assert Responses

```csharp
// Assert status code
response.StatusCode.Should().Be(HttpStatusCode.Created);

// Parse and assert response body
var result = await response.Content.ReadFromJsonAsync<BookAppointmentResult>();
result.Should().NotBeNull();
result!.Id.Should().NotBeEmpty();
result.PatientId.Should().Be(TestSeedData.DefaultPatientId);

// Assert Location header (for 201 Created)
response.Headers.Location.Should().NotBeNull();
response.Headers.Location!.ToString().Should().Contain($"/api/appointments/{result.Id}");
```

### 5. Assert Validation Errors

```csharp
// Parse ProblemDetails
var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);

// Check error type
ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();

// Check specific property error
ResponseHelper.HasValidationError(problemDetails, "End").Should().BeTrue();

// Get error message
var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "End");
errorMessage.Should().Contain("at least 10 minutes");
```

### 6. Verify Database State

```csharp
// Find entity in database
var appointment = await DbContext.Appointments.FindAsync(appointmentId);
appointment.Should().NotBeNull();
appointment!.Status.Should().Be(AppointmentStatus.Scheduled);

// Query with conditions
var prescriptions = await DbContext.Prescriptions
    .Where(p => p.PatientId == TestSeedData.DefaultPatientId)
    .ToListAsync();

prescriptions.Should().HaveCount(2);
```

## Test Data

### Seeded Test Data

Every test starts with fresh, deterministic data:

**Patients**:
- `11111111-1111-1111-1111-111111111111` - John Smith (DefaultPatientId)
- `22222222-2222-2222-2222-222222222222` - Jane Doe (SecondPatientId)
- `33333333-3333-3333-3333-333333333333` - Bob Johnson (ThirdPatientId)

**Doctors**:
- `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa` - Dr. Sarah Wilson, Family Medicine (SecondDoctorId)
- `bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb` - Dr. Michael Chen, Cardiology (DefaultDoctorId)
- `cccccccc-cccc-cccc-cccc-cccccccccccc` - Dr. Emily Rodriguez, Pediatrics (ThirdDoctorId)

**Special IDs**:
- `99999999-9999-9999-9999-999999999999` - NonExistentId (for 404 tests)

### Test Data Builder Methods

#### BookAppointmentTestDataBuilder

**Basic Configuration**:
- `WithPatientId(Guid)` - Set patient
- `WithDoctorId(Guid)` - Set doctor
- `WithStartTime(DateTimeOffset)` - Set start time
- `WithEndTime(DateTimeOffset)` - Set end time
- `WithDuration(int minutes)` - Set duration from start
- `WithNotes(string)` - Set appointment notes

**Helper Methods**:
- `WithNonExistentPatient()` - Invalid patient (404)
- `WithNonExistentDoctor()` - Invalid doctor (404)
- `WithInvalidTimeRange()` - Start >= End (400)
- `WithTooShortDuration()` - Duration < 10 min (400)
- `WithTooLongDuration()` - Duration > 8 hours (400)
- `WithTooLongNotes()` - Notes > 1024 chars (400)
- `TooSoon()` - < 15 minutes advance (400)
- `InThePast()` - Past start time (400)

#### RescheduleAppointmentTestDataBuilder

**Basic Configuration**:
- `WithAppointmentId(Guid)` - Set appointment to reschedule
- `WithNewStartTime(DateTimeOffset)` - Set new start
- `WithNewEndTime(DateTimeOffset)` - Set new end
- `WithNewDuration(int minutes)` - Set new duration
- `WithReason(string)` - Set reschedule reason

**Helper Methods**:
- `WithNonExistentAppointment()` - Invalid appointment (404)
- `WithInvalidTimeRange()` - New start >= new end (400)
- `WithTooShortDuration()` - Duration < 10 min (400)
- `WithTooLongDuration()` - Duration > 8 hours (400)
- `WithTooLongReason()` - Reason > 512 chars (400)
- `TooSoon()` - < 2 hours advance (400)

#### IssuePrescriptionTestDataBuilder

**Basic Configuration**:
- `WithPatientId(Guid)` - Set patient
- `WithDoctorId(Guid)` - Set doctor
- `WithMedicationName(string)` - Set medication
- `WithDosage(string)` - Set dosage
- `WithDirections(string)` - Set directions
- `WithQuantity(int)` - Set quantity (1-999)
- `WithNumberOfRefills(int)` - Set refills (0-12)
- `WithDurationInDays(int)` - Set duration (1-365)

**Helper Methods**:
- `WithNonExistentPatient()` - Invalid patient (404)
- `WithNonExistentDoctor()` - Invalid doctor (404)
- `WithInvalidQuantityTooLow()` - Quantity = 0 (400)
- `WithInvalidQuantityTooHigh()` - Quantity > 999 (400)
- `WithInvalidRefillsNegative()` - Refills < 0 (400)
- `WithInvalidRefillsTooHigh()` - Refills > 12 (400)
- `WithInvalidDurationTooLow()` - Duration = 0 (400)
- `WithInvalidDurationTooHigh()` - Duration > 365 (400)
- `WithEmptyMedicationName()` - Empty medication (400)
- `WithTooLongMedicationName()` - > 200 chars (400)
- `WithEmptyDosage()` - Empty dosage (400)
- `WithTooLongDosage()` - > 50 chars (400)
- `WithEmptyDirections()` - Empty directions (400)
- `WithTooLongDirections()` - > 500 chars (400)
- `AsControlledSubstance()` - Controlled substance preset (0 refills, 7 days)
- `AsLongTermMedication()` - Long-term preset (12 refills, 365 days)

## Test Isolation

Each test runs in complete isolation:

1. **Before Each Test** (`InitializeAsync`):
   - Database is deleted and recreated
   - Fresh seed data is inserted (patients, doctors)
   - New DbContext scope is created

2. **After Each Test** (`DisposeAsync`):
   - DbContext is disposed
   - Service scope is disposed
   - HttpClient is disposed

This ensures:
- No test can affect another test's state
- Tests can run in any order
- Tests can run in parallel
- Deterministic, repeatable results

## ResponseHelper Utilities

### Parsing ProblemDetails

```csharp
var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
```

### Checking Error Types

```csharp
ResponseHelper.IsValidationError(problemDetails);     // 400 Bad Request
ResponseHelper.IsNotFoundError(problemDetails);       // 404 Not Found
ResponseHelper.IsConflictError(problemDetails);       // 409 Conflict
ResponseHelper.IsUnprocessableEntityError(problemDetails); // 422 Unprocessable Entity
```

### Getting Validation Errors

```csharp
// Check if property has errors
bool hasError = ResponseHelper.HasValidationError(problemDetails, "PropertyName");

// Get first error for property
string? firstError = ResponseHelper.GetFirstValidationError(problemDetails, "PropertyName");

// Get all errors for property
string[] allErrors = ResponseHelper.GetAllValidationErrors(problemDetails, "PropertyName");

// Get total error count
int count = ResponseHelper.GetValidationErrorCount(problemDetails);

// Get all property names with errors
IEnumerable<string> properties = ResponseHelper.GetValidationErrorPropertyNames(problemDetails);
```

### Working with Error Extensions

```csharp
// Get error code
string? code = ResponseHelper.GetErrorCode(problemDetails);

// Get custom extension
object? value = ResponseHelper.GetExtension(problemDetails, "key");
```

## Common Patterns

### Testing Happy Path

```csharp
[Fact]
public async Task Feature_WithValidData_Returns201Created()
{
    // Arrange
    var command = new FeatureTestDataBuilder().Build();

    // Act
    var response = await Client.PostAsJsonAsync("/api/endpoint", command);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);

    var result = await response.Content.ReadFromJsonAsync<FeatureResult>();
    result.Should().NotBeNull();

    // Verify database
    var saved = await DbContext.Entities.FindAsync(result!.Id);
    saved.Should().NotBeNull();
}
```

### Testing Validation Errors

```csharp
[Fact]
public async Task Feature_WithInvalidData_Returns400()
{
    // Arrange
    var command = new FeatureTestDataBuilder()
        .WithInvalidProperty()
        .Build();

    // Act
    var response = await Client.PostAsJsonAsync("/api/endpoint", command);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
    ResponseHelper.HasValidationError(problemDetails, "PropertyName").Should().BeTrue();
}
```

### Testing Not Found

```csharp
[Fact]
public async Task Feature_WithNonExistentId_Returns404()
{
    // Arrange
    var command = new FeatureTestDataBuilder()
        .WithNonExistentEntity()
        .Build();

    // Act
    var response = await Client.PostAsJsonAsync("/api/endpoint", command);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.NotFound);

    var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
    problemDetails!.Title.Should().Contain("not found");
}
```

### Testing Conflicts

```csharp
[Fact]
public async Task Feature_WithConflictingData_Returns409()
{
    // Arrange - Create first resource
    var first = await CreateResourceAsync();

    // Try to create conflicting resource
    var conflicting = new FeatureTestDataBuilder()
        .WithSameTimeSlot(first)
        .Build();

    // Act
    var response = await Client.PostAsJsonAsync("/api/endpoint", conflicting);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Conflict);
}
```

### Testing Business Rules

```csharp
[Fact]
public async Task Feature_ViolatesBusinessRule_Returns400()
{
    // Arrange - Set up scenario that violates business rule
    var entity = await CreateEntityAsync();
    var command = BuildCommandThatViolatesRule(entity);

    // Act
    var response = await Client.PostAsJsonAsync("/api/endpoint", command);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
    var error = ResponseHelper.GetFirstValidationError(problemDetails, "RuleCode");
    error.Should().Contain("business rule message");
}
```

## Best Practices

### ✅ Do

- **Use descriptive test names**: `Feature_Scenario_ExpectedBehavior`
- **Test one thing per test**: Single behavior or scenario
- **Use test data builders**: Minimize setup code
- **Verify database state**: Don't just test HTTP responses
- **Test error paths**: Validation, not found, conflicts
- **Use FluentAssertions**: Readable assertions
- **Follow AAA pattern**: Arrange, Act, Assert

### ❌ Don't

- **Don't share state between tests**: Each test is isolated
- **Don't depend on execution order**: Tests must be independent
- **Don't use hardcoded dates**: Use relative times
- **Don't manually call InitializeAsync**: Called automatically
- **Don't skip database verification**: Ensure side effects occurred
- **Don't test multiple scenarios in one test**: Keep tests focused

## Troubleshooting

### Entity Already Tracked Error

**Problem**: "The instance of entity type 'X' cannot be tracked because another instance with the same key value is already being tracked."

**Solution**: Don't manually call `InitializeAsync()` in tests. It's called automatically by xUnit before each test. Each test gets a fresh DbContext scope.

### Timezone Issues

**Problem**: Tests fail with time comparison errors like "expected 10:00:00 but was 08:00:00".

**Solution**:
```csharp
// Use BuildValues() to get actual sent values
var (_, _, start, end, _) = builder.BuildValues();

// Compare UTC times
result.StartUtc.Should().BeCloseTo(start.UtcDateTime, TimeSpan.FromSeconds(1));
```

### Intermittent Test Failures

**Problem**: Tests pass sometimes, fail other times.

**Causes & Solutions**:
- **Execution order dependency**: Ensure tests are truly isolated
- **Hardcoded dates**: Use `DateTimeOffset.UtcNow.AddDays(7)` instead of fixed dates
- **Database state**: Verify each test resets properly
- **Race conditions**: Check for async/await issues

### Validation Not Working

**Problem**: Validation errors expected but not returned.

**Check**:
1. Validator is properly registered (should be automatic for `internal sealed` classes)
2. Validator inherits `AbstractValidator<TCommand>`
3. ValidationBehaviour is in MediatR pipeline
4. FluentValidation package is referenced

## Adding New Tests

When adding a new feature, create corresponding integration tests:

1. **Create test class** in appropriate folder (Healthcare/Feature/)
2. **Inherit from IntegrationTestBase**
3. **Create test data builder** if needed (in TestData/)
4. **Write tests** covering:
   - Happy path (201/200)
   - Validation errors (400)
   - Not found (404)
   - Conflicts (409)
   - Business rule violations (400/422)
   - Boundary conditions
   - Edge cases

## Running Tests in CI/CD

```bash
# Run with test results output
dotnet test --logger "trx;LogFileName=test-results.trx"

# Run with code coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Run with parallel execution (default)
dotnet test --parallel

# Run without parallel execution
dotnet test -- RunConfiguration.MaxCpuCount=1
```

## Further Reading

- [CLAUDE.md](../../CLAUDE.md) - Full integration testing documentation
- [CustomWebApplicationFactory.cs](Infrastructure/CustomWebApplicationFactory.cs) - Test server setup
- [IntegrationTestBase.cs](Infrastructure/IntegrationTestBase.cs) - Test base class
- [ResponseHelper.cs](Helpers/ResponseHelper.cs) - ProblemDetails utilities
