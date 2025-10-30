# Application Unit Tests

Comprehensive unit test suite for the Vertical Slice Architecture application. Tests verify individual components in isolation without external dependencies.

## Quick Start

```bash
# Run all unit tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~AppointmentTests"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run and watch for changes
dotnet watch test
```

## Project Structure

```
Application.UnitTests/
├── Common/                          # Tests for shared infrastructure
│   ├── Behaviours/                  # MediatR pipeline behavior tests
│   ├── MinimalApiProblemHelperTests.cs
│   └── ValidationFilterTests.cs
├── Domain/                          # Domain entity tests
│   └── Healthcare/
│       ├── AppointmentTests.cs      # Appointment entity behavior
│       └── PrescriptionTests.cs     # Prescription entity behavior
├── Features/                        # Feature-specific tests
│   └── Healthcare/
│       └── IssuePrescriptionValidatorTests.cs
├── Healthcare/                      # Healthcare feature validators
│   └── Appointments/
│       ├── BookAppointmentValidatorTests.cs
│       ├── RescheduleAppointmentValidatorTests.cs
│       └── CompleteAppointmentValidatorTests.cs
└── ValueObjects/                    # Value object tests
    └── ColourTests.cs
```

## Test Categories

### 1. Domain Entity Tests

Test business logic, state transitions, and invariant protection in domain entities.

**Example**: [AppointmentTests.cs](Domain/Healthcare/AppointmentTests.cs)

```csharp
[Fact]
public void Complete_ScheduledAppointment_SetsStatusAndTimestamp()
{
    // Arrange
    var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);
    var beforeComplete = DateTime.UtcNow;

    // Act
    appointment.Complete("Patient checked in and seen");

    // Assert
    appointment.Status.Should().Be(AppointmentStatus.Completed);
    appointment.CompletedUtc.Should().NotBeNull();
    appointment.CompletedUtc.Should().BeCloseTo(beforeComplete, TimeSpan.FromSeconds(1));
}
```

**What to Test**:
- State transitions (Scheduled → Completed, etc.)
- Validation logic (throw exceptions for invalid inputs)
- Business rule enforcement
- Idempotency (can call method multiple times safely)
- Edge cases (null values, boundary conditions)

### 2. Validator Tests

Test FluentValidation validators using FluentValidation.TestHelper.

**Example**: [CompleteAppointmentValidatorTests.cs](Healthcare/Appointments/CompleteAppointmentValidatorTests.cs)

```csharp
[Fact]
public void Should_Have_Error_When_AppointmentId_Is_Empty()
{
    // Arrange
    var command = new CompleteAppointmentCommand(Guid.Empty, "Test notes");

    // Act & Assert
    var result = _validator.TestValidate(command);
    result.ShouldHaveValidationErrorFor(x => x.AppointmentId)
        .WithErrorMessage("AppointmentId is required");
}
```

**What to Test**:
- Required fields validation
- Length/range constraints
- Format validation (email, phone, etc.)
- Custom business rule validation
- Cross-field validation

### 3. Pipeline Behavior Tests

Test MediatR pipeline behaviors with mocked dependencies.

**Example**: [ValidationBehaviorTests.cs](Common/Behaviours/ValidationBehaviorTests.cs)

```csharp
[Fact]
public async Task InvokeValidationBehavior_WhenValidatorResultIsNotValid_ShouldReturnListOfErrors()
{
    // Arrange
    var mockValidator = Substitute.For<IValidator<TestRequest>>();
    var failures = new List<ValidationFailure>
    {
        new ValidationFailure("Property1", "Error 1")
    };
    mockValidator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), default)
        .Returns(new ValidationResult(failures));

    var behavior = new ValidationBehaviour<TestRequest, ErrorOr<int>>(new[] { mockValidator });

    // Act
    var result = await behavior.Handle(
        new TestRequest(),
        () => Task.FromResult(ErrorOr<int>.From(1)),
        CancellationToken.None);

    // Assert
    result.IsError.Should().BeTrue();
    result.Errors.Should().HaveCount(1);
}
```

**What to Test**:
- Behavior executes correctly with valid input
- Behavior handles errors appropriately
- Pipeline continues/stops as expected
- Side effects occur correctly

### 4. Helper/Utility Tests

Test pure functions and utility methods.

**Example**: [MinimalApiProblemHelperTests.cs](Common/MinimalApiProblemHelperTests.cs)

```csharp
[Fact]
public void Problem_WithNotFoundError_ReturnsProblemWithStatus404()
{
    // Arrange
    var errors = new List<Error>
    {
        Error.NotFound("Resource.NotFound", "Resource not found")
    };

    // Act
    var result = MinimalApiProblemHelper.Problem(errors);

    // Assert
    result.Should().BeOfType<ProblemHttpResult>();
    var problemResult = (ProblemHttpResult)result;
    problemResult.StatusCode.Should().Be(404);
}
```

## Testing Frameworks & Libraries

### xUnit

Testing framework with `[Fact]` and `[Theory]` attributes.

```csharp
[Fact]  // Single test case
public void MyTest() { }

[Theory]  // Multiple test cases with different inputs
[InlineData(1, 2, 3)]
[InlineData(2, 3, 5)]
public void Add_TwoNumbers_ReturnsSum(int a, int b, int expected)
{
    var result = a + b;
    result.Should().Be(expected);
}
```

### FluentAssertions

Provides readable, expressive assertions:

```csharp
// Basic assertions
result.Should().Be(expected);
result.Should().NotBeNull();
result.Should().BeEquivalentTo(expected);

// Strings
name.Should().StartWith("Dr.");
message.Should().Contain("error");
email.Should().MatchRegex(@"^[^@]+@[^@]+\.[^@]+$");

// Numbers
count.Should().BeGreaterThan(0);
percentage.Should().BeInRange(0, 100);

// DateTime
timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

// Collections
list.Should().HaveCount(3);
list.Should().Contain(x => x.Id == expectedId);
list.Should().NotBeEmpty();

// Exceptions
var act = () => myObject.DoSomething();
act.Should().Throw<InvalidOperationException>()
    .WithMessage("Cannot complete*");

// Types
result.Should().BeOfType<MyType>();
result.Should().BeAssignableTo<IMyInterface>();
```

### NSubstitute

Mocking framework for creating test doubles:

```csharp
// Create mock
var mockService = Substitute.For<IMyService>();

// Setup return values
mockService.GetValue().Returns(42);
mockService.GetValueAsync().Returns(Task.FromResult(42));

// Setup conditional returns
mockService.GetById(Arg.Any<Guid>()).Returns(x => new Item { Id = x.Arg<Guid>() });

// Setup exceptions
mockService.DoSomething().Throws(new InvalidOperationException("Error"));

// Verify calls
mockService.Received(1).DoSomething();
mockService.DidNotReceive().DoSomethingElse();

// Capture arguments
Guid capturedId = Guid.Empty;
mockService.Save(Arg.Do<Item>(x => capturedId = x.Id));
```

### FluentValidation.TestHelper

Specialized testing for validators:

```csharp
// Test validation errors
var result = _validator.TestValidate(command);
result.ShouldHaveValidationErrorFor(x => x.PropertyName);
result.ShouldHaveValidationErrorFor(x => x.PropertyName)
    .WithErrorMessage("Expected error message");

// Test no validation errors
result.ShouldNotHaveValidationErrorFor(x => x.PropertyName);
result.ShouldNotHaveAnyValidationErrors();
```

## Common Patterns

### Testing State Transitions

```csharp
[Fact]
public void Complete_ScheduledAppointment_TransitionsToCompleted()
{
    // Arrange
    var appointment = Appointment.Schedule(_patientId, _doctorId, _startUtc, _endUtc);

    // Act
    appointment.Complete("Notes");

    // Assert
    appointment.Status.Should().Be(AppointmentStatus.Completed);
}
```

### Testing Validation

```csharp
[Fact]
public void Complete_NotesExceed1024Characters_ThrowsArgumentException()
{
    // Arrange
    var appointment = Appointment.Schedule(_patientId, _doctorId, _startUtc, _endUtc);
    var longNotes = new string('x', 1025);

    // Act & Assert
    var act = () => appointment.Complete(longNotes);
    act.Should().Throw<ArgumentException>()
        .WithMessage("Notes cannot exceed 1024 characters*")
        .And.ParamName.Should().Be("notes");
}
```

### Testing Idempotency

```csharp
[Fact]
public void Complete_AlreadyCompleted_RemainsUnchanged()
{
    // Arrange
    var appointment = Appointment.Schedule(_patientId, _doctorId, _startUtc, _endUtc);
    appointment.Complete("First completion");
    var originalTimestamp = appointment.CompletedUtc;

    // Act
    appointment.Complete("Second attempt");

    // Assert
    appointment.CompletedUtc.Should().Be(originalTimestamp);
}
```

### Testing Invariant Protection

```csharp
[Fact]
public void Cancel_CompletedAppointment_ThrowsInvalidOperationException()
{
    // Arrange
    var appointment = Appointment.Schedule(_patientId, _doctorId, _startUtc, _endUtc);
    appointment.Complete("Completed");

    // Act & Assert
    var act = () => appointment.Cancel("Trying to cancel");
    act.Should().Throw<InvalidOperationException>()
        .WithMessage("Cannot cancel a completed appointment");
}
```

## Best Practices

### ✅ Do's

1. **Test One Thing** - Each test should verify a single behavior
2. **Use Arrange-Act-Assert** - Clear three-part structure
3. **Descriptive Names** - `MethodName_Scenario_ExpectedBehavior`
4. **Fast Tests** - Unit tests should complete in milliseconds
5. **Isolated Tests** - No dependencies between tests
6. **Test Edge Cases** - Null, empty, boundary values
7. **Test Exceptions** - Verify error handling
8. **Use FluentAssertions** - Readable assertions
9. **Mock External Dependencies** - No database, HTTP, file system
10. **Keep Tests Simple** - No complex logic in tests

### ❌ Don'ts

1. **Don't Test Implementation Details** - Test behavior, not internals
2. **Don't Share State** - Each test should be independent
3. **Don't Use Magic Numbers** - Use named constants
4. **Don't Skip Assertions** - Always verify expected outcome
5. **Don't Test Framework Code** - Trust that EF, ASP.NET work
6. **Don't Make Tests Complex** - Tests should be simple to understand
7. **Don't Depend on Execution Order** - Tests should run in any order
8. **Don't Use Real Dependencies** - Mock everything external
9. **Don't Write Slow Tests** - Unit tests should be fast
10. **Don't Test Multiple Things** - One assertion per test (generally)

## Naming Conventions

### Test Class Names

```
{ClassUnderTest}Tests
```

Examples:
- `AppointmentTests`
- `CompleteAppointmentValidatorTests`
- `MinimalApiProblemHelperTests`

### Test Method Names

```
{MethodName}_{Scenario}_{ExpectedBehavior}
```

Examples:
- `Complete_ScheduledAppointment_SetsStatusAndTimestamp`
- `Complete_CancelledAppointment_ThrowsInvalidOperationException`
- `Should_Have_Error_When_AppointmentId_Is_Empty`
- `Problem_WithNotFoundError_ReturnsProblemWithStatus404`

## When to Use Unit Tests vs Integration Tests

### Use Unit Tests For:

- ✅ Domain object behavior (state transitions, validation)
- ✅ Validators (FluentValidation rules)
- ✅ Helper/utility methods (pure functions)
- ✅ Pipeline behaviors (with mocked dependencies)
- ✅ Value objects (equality, immutability)
- ✅ Edge cases and exceptional paths

### Use Integration Tests For:

- ✅ HTTP endpoint testing (full request/response cycle)
- ✅ Database operations (queries, persistence)
- ✅ Feature workflows (multiple steps)
- ✅ Error responses (ProblemDetails format)
- ✅ Component interactions

**Rule of Thumb**: If it needs a database or HTTP, use integration tests. If it's pure logic, use unit tests.

## Troubleshooting

### Test Fails with "Expected X but found Y"

FluentAssertions provides detailed output. Common fixes:

- Use `.Should().BeEquivalentTo()` for object comparisons
- Use `.Should().Be()` for value comparisons
- Check for timezone issues with DateTime values

### Test Fails Intermittently

- Avoid `DateTime.Now` - use fixed values or `DateTime.UtcNow`
- Don't depend on test execution order
- Ensure no shared static state

### Mock Not Returning Expected Value

- Check argument matchers: `Arg.Any<T>()` vs `Arg.Is<T>(x => condition)`
- Verify setup is called before test execution
- Use `Returns()` for sync, `ReturnsAsync()` for async methods

### Validator Test Not Finding Error

- Verify property expression: `x => x.PropertyName` must match exactly
- Check validator is using correct command type
- Use `TestValidate()` not `Validate()`

### Tests Are Slow

- Unit tests should complete in < 100ms each
- Check for accidental database/HTTP calls
- Remove `Thread.Sleep` or unnecessary delays
- Consider if test should be an integration test instead

## Contributing

When adding new tests:

1. Place tests in the appropriate category folder
2. Follow naming conventions
3. Use Arrange-Act-Assert pattern
4. Add descriptive test names
5. Verify tests run in isolation
6. Ensure tests are fast
7. Add comments for complex scenarios

## Additional Resources

- [CLAUDE.md](../../CLAUDE.md#unit-testing) - Comprehensive testing guide
- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [NSubstitute Documentation](https://nsubstitute.github.io/)
- [FluentValidation Testing](https://docs.fluentvalidation.net/en/latest/testing.html)
