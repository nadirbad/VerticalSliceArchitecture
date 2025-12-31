# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Architecture Overview

This is a .NET 9 Vertical Slice Architecture example that organizes code by features rather than technical layers. The solution consists of 2 projects:

- **src/Api**: ASP.NET Core entry point with DI, middleware, and hosting
- **src/Application**: Contains all features, domain entities, infrastructure, and shared concerns organized by vertical slices

The project uses **.NET 9 Minimal APIs** instead of MVC controllers. Each feature keeps endpoint handlers, request/response types, validation, and MediatR handlers together in a single file (e.g., `Scheduling/BookAppointment.cs` contains endpoint, command, validator, and handler).

### Current Domain: Healthcare

The codebase implements a Healthcare domain with:

- **Scheduling**: Appointment booking, rescheduling, completion, and cancellation
- **Medications**: Prescription issuance and management

### Project Structure

```text
src/Application/
├── Domain/                    # Domain entities, value objects, events
│   ├── Appointment.cs
│   ├── Patient.cs
│   ├── Doctor.cs
│   ├── Prescription.cs
│   ├── SchedulingPolicies.cs
│   ├── PrescriptionPolicies.cs
│   └── Events/                # Domain events
│       ├── AppointmentBookedEvent.cs
│       ├── AppointmentRescheduledEvent.cs
│       ├── AppointmentCompletedEvent.cs
│       └── AppointmentCancelledEvent.cs
├── Scheduling/                # Appointment feature slice
│   ├── AppointmentEndpoints.cs
│   ├── BookAppointment.cs
│   ├── RescheduleAppointment.cs
│   ├── CompleteAppointment.cs
│   ├── CancelAppointment.cs
│   ├── GetAppointments.cs
│   ├── GetAppointmentById.cs
│   ├── GetPatientAppointments.cs
│   ├── GetDoctorAppointments.cs
│   └── EventHandlers/
│       ├── AppointmentBookedEventHandler.cs
│       └── AppointmentRescheduledEventHandler.cs
├── Medications/               # Prescription feature slice
│   ├── PrescriptionEndpoints.cs
│   └── IssuePrescription.cs
├── Common/                    # Shared concerns
│   ├── MinimalApiProblemHelper.cs
│   ├── ValidationFilter.cs
│   └── ...
├── Infrastructure/            # Data access, services
│   └── Persistence/
│       ├── ApplicationDbContext.cs
│       └── Configurations/
├── HealthcareEndpoints.cs     # Root endpoint mapper
└── ConfigureServices.cs       # DI registration
```

## Development Commands

All commands should be run from the repository root:

```bash
# Build the solution
dotnet build

# Run the API (Swagger UI at https://localhost:7098/)
dotnet run --project src/Api/Api.csproj

# Run unit tests
dotnet test tests/Application.UnitTests/Application.UnitTests.csproj

# Run integration tests (requires database)
dotnet test tests/Application.IntegrationTests/Application.IntegrationTests.csproj

# Run a specific test
dotnet test --filter "FullyQualifiedName~<TestNameOrNamespace>"

# Code formatting and analysis
dotnet format
dotnet format --verify-no-changes

# Watch mode for development
dotnet watch run --project src/Api/Api.csproj

# Publish for deployment
dotnet publish src/Api/Api.csproj --configuration Release
```

## Database and Migrations

Uses in-memory database by default. To use SQL Server, set `UseInMemoryDatabase=false` in `src/Api/appsettings.json` and configure `DefaultConnection`.

EF Core migration commands (from repository root):

```bash
# Add new migration
dotnet ef migrations add "MigrationName" --project src/Application --startup-project src/Api --output-dir Infrastructure/Persistence/Migrations

# Update database
dotnet ef database update --project src/Application --startup-project src/Api
```

## Feature Development Patterns

### Adding New Features

1. Create feature file under `src/Application/<FeatureArea>/<VerbNoun>.cs` (e.g., `Scheduling/BookAppointment.cs`)
2. Include all related code in one file: endpoint handler, command/query, validator, handler, and DTOs
3. Create endpoint registration in `<FeatureArea>Endpoints.cs` using route groups
4. Use `ErrorOr<T>` return types for consistent error handling
5. Access data via `ApplicationDbContext` directly (no repository pattern)
6. Add validators with FluentValidation (auto-registered with `includeInternalTypes: true`)
7. Register endpoints in `HealthcareEndpoints.cs` (or create new root endpoint mapper)

### Example Feature Structure

```csharp
// Endpoint Handler (static class with Handle method)
public static class BookAppointmentEndpoint
{
    public static async Task<IResult> Handle(
        BookAppointmentCommand command,
        ISender mediator)
    {
        var result = await mediator.Send(command);

        return result.Match(
            success => Results.Created($"/api/healthcare/appointments/{success.Id}", success),
            errors => MinimalApiProblemHelper.Problem(errors));
    }
}

// Command/Query
public record BookAppointmentCommand(
    Guid PatientId,
    Guid DoctorId,
    DateTimeOffset Start,
    DateTimeOffset End,
    string? Notes) : IRequest<ErrorOr<BookAppointmentResult>>;

// Result DTO
public record BookAppointmentResult(Guid Id, DateTime StartUtc, DateTime EndUtc);

// Validator
internal sealed class BookAppointmentCommandValidator : AbstractValidator<BookAppointmentCommand>
{
    public BookAppointmentCommandValidator()
    {
        RuleFor(v => v.PatientId).NotEmpty().WithMessage("PatientId is required");
        RuleFor(v => v.DoctorId).NotEmpty().WithMessage("DoctorId is required");
        // ... more rules
    }
}

// Handler
internal sealed class BookAppointmentCommandHandler(ApplicationDbContext context)
    : IRequestHandler<BookAppointmentCommand, ErrorOr<BookAppointmentResult>>
{
    public async Task<ErrorOr<BookAppointmentResult>> Handle(
        BookAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        // Implementation...
    }
}
```

### Endpoint Registration

Register endpoints in the feature's endpoint file:

```csharp
// Scheduling/AppointmentEndpoints.cs
public static class SchedulingEndpoints
{
    public static RouteGroupBuilder MapAppointmentEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", BookAppointmentEndpoint.Handle)
            .WithName("BookAppointment")
            .Produces<BookAppointmentResult>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddEndpointFilter<ValidationFilter<BookAppointmentCommand>>();

        group.MapPost("/{appointmentId}/reschedule", RescheduleAppointmentEndpoint.Handle)
            .WithName("RescheduleAppointment")
            // ... more configuration

        return group;
    }
}
```

Wire up in root endpoint mapper:

```csharp
// HealthcareEndpoints.cs
public static class HealthcareEndpoints
{
    public static IEndpointRouteBuilder MapHealthcareEndpoints(this IEndpointRouteBuilder app)
    {
        var apiGroup = app.MapGroup("/api");

        apiGroup.MapGroup("/healthcare/appointments")
            .WithTags("Appointments")
            .MapAppointmentEndpoints();

        apiGroup.MapGroup("/prescriptions")
            .WithTags("Prescriptions")
            .MapPrescriptionEndpoints();

        return app;
    }
}
```

### Domain objects

When writing domain objects in C#, follow these patterns and principles:

#### Property Design

- Use public get; private set; for all domain properties
- Only use public get; set; for DTOs, view models, or simple data containers
- Properties should represent the object's state, not behavior

#### Constructor Patterns

- Create constructors that establish valid object state from the beginning
- Use constructor parameters for required properties
- Validate all inputs in constructors and throw descriptive exceptions for invalid data
- Consider using factory methods for complex object creation

#### Encapsulation

- Keep business logic inside the domain object
- Expose behavior through methods, not public setters
- Use private methods for internal operations
- Protect object invariants at all times

#### Method Design

- Create methods that represent business operations (e.g., ProcessPayment(), ApproveOrder(), UpdateStatus())
- Methods should maintain object validity and enforce business rules
- Use descriptive method names that reflect domain language
- Return domain events or results rather than void when appropriate

#### Validation and Business Rules

- Validate inputs in constructors and methods
- Throw domain-specific exceptions with meaningful messages
- Use guard clauses for precondition checks
- Implement business rules within the domain object, not in external services

Example Structure (from Appointment domain entity)

```csharp
public class Appointment : AuditableEntity, IHasDomainEvent
{
    // Factory method - preferred over public constructor
    public static Appointment Schedule(Guid patientId, Guid doctorId, DateTime startUtc, DateTime endUtc, string? notes = null)
    {
        var appointment = new Appointment(patientId, doctorId, startUtc, endUtc, notes);

        // Raise domain event
        appointment.DomainEvents.Add(new AppointmentBookedEvent(
            appointment.Id,
            appointment.PatientId,
            appointment.DoctorId,
            appointment.StartUtc,
            appointment.EndUtc));

        return appointment;
    }

    // Private parameterless constructor for EF Core
    private Appointment() { }

    // Private constructor enforces use of factory method
    private Appointment(Guid patientId, Guid doctorId, DateTime startUtc, DateTime endUtc, string? notes)
    {
        ValidateDateTime(startUtc, nameof(startUtc));
        ValidateDateTime(endUtc, nameof(endUtc));

        if (startUtc >= endUtc)
            throw new ArgumentException("Start time must be before end time", nameof(startUtc));

        PatientId = patientId;
        DoctorId = doctorId;
        StartUtc = startUtc;
        EndUtc = endUtc;
        Status = AppointmentStatus.Scheduled;
        UpdateNotes(notes);
    }

    public Guid Id { get; internal set; }
    public Guid PatientId { get; private set; }
    public AppointmentStatus Status { get; private set; }
    // ... other properties with private set

    [NotMapped]
    public List<DomainEvent> DomainEvents { get; } = new List<DomainEvent>();

    // Business method with state transition and domain event
    public void Complete(string? notes = null, DateTime? completedAtUtc = null)
    {
        if (Status == AppointmentStatus.Cancelled)
            throw new InvalidOperationException("Cannot complete a cancelled appointment");

        if (Status == AppointmentStatus.Completed)
            return; // Idempotent

        Status = AppointmentStatus.Completed;
        CompletedUtc = completedAtUtc ?? DateTime.UtcNow;
        Notes = notes;

        DomainEvents.Add(new AppointmentCompletedEvent(Id, PatientId, DoctorId, CompletedUtc.Value, Notes));
    }

    public void Cancel(string reason, DateTime? cancelledAtUtc = null)
    {
        if (Status == AppointmentStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed appointment");

        if (Status == AppointmentStatus.Cancelled)
            return; // Idempotent

        Status = AppointmentStatus.Cancelled;
        CancelledUtc = cancelledAtUtc ?? DateTime.UtcNow;
        CancellationReason = reason;

        DomainEvents.Add(new AppointmentCancelledEvent(Id, PatientId, DoctorId, CancelledUtc.Value, CancellationReason));
    }

    private static void ValidateDateTime(DateTime dateTime, string parameterName)
    {
        if (dateTime.Kind != DateTimeKind.Utc)
            throw new ArgumentException("DateTime must be in UTC", parameterName);
    }
}
```

#### Avoid These Patterns

- Public setters on domain objects (except for ORMs in private setters)
- Anemic domain models (objects with only getters/setters and no behavior)
- Business logic in external services when it belongs in the domain object
- Parameterless constructors unless required by frameworks
- Exposing internal collections directly (use IReadOnlyCollection<T> instead)

#### Framework Considerations

- For Entity Framework, use private set and configure mapping appropriately
- Consider using backing fields for complex validation scenarios
- Use domain events for cross-aggregate communication
- Implement value objects for concepts without identity

Focus on creating rich domain models that encapsulate behavior and protect their own invariants.

## Key Technologies and Patterns

- **MediatR**: Request/response pattern with pipeline behaviors for cross-cutting concerns
- **FluentValidation**: Request validation with automatic registration
- **ErrorOr**: Consistent error handling without exceptions
- **Entity Framework Core**: Data access with domain event dispatching
- **Domain Events**: Raised by entities, handled by separate event handlers under `<FeatureArea>/EventHandlers/`
- **Minimal APIs**: .NET 9 endpoint routing with `RouteGroupBuilder` and `ValidationFilter<T>`

## Configuration and Setup

- **Database**: Defaults to in-memory; toggle via `UseInMemoryDatabase` in appsettings.json
- **Swagger**: Available at root URL (/) in development
- **CORS**: Configured to allow any origin for development
- **Health Checks**: Available at `/health` endpoint
- **Sample Requests**: HTTP files in `requests/Healthcare/**` for manual testing
  - `requests/Healthcare/Appointments/BookAppointment.http`
  - `requests/Healthcare/Appointments/RescheduleAppointment.http`
  - `requests/Healthcare/Appointments/GetAppointments.http`
  - `requests/Healthcare/Prescriptions/IssuePrescription.http`

## Pipeline Behaviors

Cross-cutting concerns handled via MediatR pipeline:

- `AuthorizationBehaviour`: Security validation
- `PerformanceBehaviour`: Performance monitoring
- `ValidationBehaviour`: FluentValidation integration
- `LoggingBehaviour`: Request/response logging

## Domain Event Handling

Entities can raise domain events by adding them to `DomainEvents` collection. Events are automatically dispatched after `SaveChangesAsync()` in `ApplicationDbContext`.

## Code Quality Settings

The project enforces:
- `TreatWarningsAsErrors=true`
- `EnforceCodeStyleInBuild=true`
- StyleCop analyzers for consistent code style
- EditorConfig for formatting standards

## Integration Testing

### Overview

Integration tests verify the complete request/response cycle including HTTP endpoints, validation, business logic, and database operations. The test infrastructure uses `WebApplicationFactory` for in-memory hosting and provides complete test isolation.

### Test Infrastructure

**Location**: `tests/Application.IntegrationTests/`

**Key Components**:
- `CustomWebApplicationFactory`: Configures in-memory database for testing
- `IntegrationTestBase`: Base class providing test lifecycle management and database reset
- `ResponseHelper`: Utilities for parsing and asserting ProblemDetails responses
- `HttpClientExtensions`: Extension methods for common HTTP operations

### Running Integration Tests

```bash
# Run all integration tests
dotnet test tests/Application.IntegrationTests/Application.IntegrationTests.csproj

# Run specific test class
dotnet test --filter "FullyQualifiedName~BookAppointmentTests"

# Run specific test method
dotnet test --filter "BookAppointment_WithValidData_Returns201CreatedWithAppointmentDetails"
```

### Test Isolation Strategy

Each test class inherits from `IntegrationTestBase` which:
1. Creates a fresh database before each test (`InitializeAsync`)
2. Seeds deterministic test data (patients, doctors with known GUIDs)
3. Cleans up resources after each test (`DisposeAsync`)

This ensures complete test isolation - no test can affect another test's state.

### Test Data Builders

Use immutable test data builders to create request payloads with sensible defaults:

```csharp
// Basic usage with defaults
var command = new BookAppointmentTestDataBuilder().Build();

// Customize specific properties via fluent API
var command = new BookAppointmentTestDataBuilder()
    .WithPatientId(TestSeedData.SecondPatientId)
    .WithStartTime(DateTimeOffset.UtcNow.AddDays(5))
    .WithDuration(45)
    .WithNotes("Follow-up appointment")
    .Build();

// Use helper methods for common scenarios
var command = new BookAppointmentTestDataBuilder()
    .WithNonExistentPatient()  // Tests 404 scenarios
    .Build();

var command = new BookAppointmentTestDataBuilder()
    .WithTooShortDuration()    // Tests validation errors
    .Build();
```

**Available Builders**:

- `BookAppointmentTestDataBuilder`: For appointment booking
- `RescheduleAppointmentTestDataBuilder`: For appointment rescheduling
- `CompleteAppointmentTestDataBuilder`: For appointment completion
- `CancelAppointmentTestDataBuilder`: For appointment cancellation
- `IssuePrescriptionTestDataBuilder`: For prescription issuance

**Deterministic Test Data** (`TestSeedData`):
- `DefaultPatientId`, `SecondPatientId`, `ThirdPatientId`: Known patient GUIDs
- `DefaultDoctorId`, `SecondDoctorId`, `ThirdDoctorId`: Known doctor GUIDs
- `NonExistentId`: For testing 404 scenarios

### Writing Integration Tests

#### 1. Create Test Class

```csharp
public class MyFeatureTests : IntegrationTestBase
{
    public MyFeatureTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task MyFeature_WithValidData_Returns201Created()
    {
        // Test implementation
    }
}
```

#### 2. Follow Arrange-Act-Assert Pattern

```csharp
[Fact]
public async Task BookAppointment_WithValidData_Returns201CreatedWithAppointmentDetails()
{
    // Arrange - Prepare test data
    var command = new BookAppointmentTestDataBuilder().Build();

    // Act - Execute HTTP request
    var response = await Client.PostAsJsonAsync("/api/healthcare/appointments", command);

    // Assert - Verify response and side effects
    response.StatusCode.Should().Be(HttpStatusCode.Created);

    var result = await response.Content.ReadFromJsonAsync<BookAppointmentResult>();
    result.Should().NotBeNull();
    result!.Id.Should().NotBeEmpty();

    // Verify database state
    var savedAppointment = await DbContext.Appointments
        .FirstOrDefaultAsync(a => a.Id == result.Id);
    savedAppointment.Should().NotBeNull();
}
```

#### 3. Test Validation Errors

```csharp
[Fact]
public async Task BookAppointment_WithInvalidData_Returns400WithValidationErrors()
{
    // Arrange
    var command = new BookAppointmentTestDataBuilder()
        .WithTooShortDuration()
        .Build();

    // Act
    var response = await Client.PostAsJsonAsync("/api/healthcare/appointments", command);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
    ResponseHelper.IsValidationError(problemDetails).Should().BeTrue();
    ResponseHelper.HasValidationError(problemDetails, "End").Should().BeTrue();

    var errorMessage = ResponseHelper.GetFirstValidationError(problemDetails, "End");
    errorMessage.Should().Contain("at least 10 minutes");
}
```

#### 4. Test Business Logic Errors

```csharp
[Fact]
public async Task RescheduleAppointment_Within24Hours_Returns400()
{
    // Arrange - Book appointment starting in 23 hours
    var startTime = DateTimeOffset.UtcNow.AddHours(23);
    var bookCommand = new BookAppointmentTestDataBuilder()
        .WithStartTime(startTime)
        .Build();

    var bookResponse = await Client.PostAsJsonAsync("/api/healthcare/appointments", bookCommand);
    var bookResult = await bookResponse.Content.ReadFromJsonAsync<BookAppointmentResult>();

    // Try to reschedule (should fail due to 24-hour rule)
    var rescheduleCommand = new RescheduleAppointmentTestDataBuilder()
        .WithAppointmentId(bookResult!.Id)
        .Build();

    // Act
    var response = await Client.PostAsJsonAsync(
        $"/api/healthcare/appointments/{bookResult.Id}/reschedule",
        rescheduleCommand);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);
    var errorMessage = ResponseHelper.GetFirstValidationError(
        problemDetails,
        "Appointment.RescheduleWindowClosed");
    errorMessage.Should().Contain("within 24 hours");
}
```

### Test Naming Convention

Use descriptive test names following the pattern:
`MethodName_Scenario_ExpectedBehavior`

**Examples**:
- `BookAppointment_WithValidData_Returns201Created`
- `BookAppointment_WithNonExistentPatientId_Returns404NotFound`
- `BookAppointment_WithOverlappingDoctorAppointment_Returns409Conflict`
- `RescheduleAppointment_Within24Hours_Returns400WithValidationError`

### ResponseHelper Utilities

The `ResponseHelper` provides methods for working with ProblemDetails:

```csharp
// Parse ProblemDetails from response
var problemDetails = await ResponseHelper.GetProblemDetailsAsync(response);

// Check error type
ResponseHelper.IsValidationError(problemDetails);     // 400
ResponseHelper.IsNotFoundError(problemDetails);       // 404
ResponseHelper.IsConflictError(problemDetails);       // 409
ResponseHelper.IsUnprocessableEntityError(problemDetails); // 422

// Get validation errors
ResponseHelper.HasValidationError(problemDetails, "PropertyName");
ResponseHelper.GetFirstValidationError(problemDetails, "PropertyName");
ResponseHelper.GetAllValidationErrors(problemDetails, "PropertyName");
ResponseHelper.GetValidationErrorCount(problemDetails);
```

### Best Practices

1. **Test One Thing**: Each test should verify a single behavior or scenario
2. **Use Builders**: Leverage test data builders to minimize test setup code
3. **Verify Side Effects**: Don't just test HTTP responses - verify database state changed correctly
4. **Test Edge Cases**: Include boundary conditions, null handling, empty collections
5. **Use FluentAssertions**: Provides readable assertions with helpful error messages
6. **Test Error Paths**: Validation errors, not found, conflicts, business rule violations
7. **Isolate Tests**: Never depend on execution order or shared state between tests

### Common Test Patterns

**Testing Created Resources**:
```csharp
var response = await Client.PostAsJsonAsync("/api/prescriptions", command);
response.StatusCode.Should().Be(HttpStatusCode.Created);

// Verify Location header
response.Headers.Location.Should().NotBeNull();
response.Headers.Location!.ToString().Should().Contain($"/api/prescriptions/{result.Id}");

// Verify database persistence
var saved = await DbContext.Prescriptions.FindAsync(result.Id);
saved.Should().NotBeNull();
```

**Testing Conflicts**:
```csharp
// Create first resource
var firstResponse = await Client.PostAsJsonAsync("/api/appointments", firstCommand);
firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

// Try to create conflicting resource
var conflictResponse = await Client.PostAsJsonAsync("/api/appointments", conflictingCommand);
conflictResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
```

**Testing Domain State Changes**:
```csharp
// Perform operation that changes state
var response = await Client.PostAsJsonAsync($"/api/appointments/{id}/reschedule", command);

// Verify domain object updated
var appointment = await DbContext.Appointments.FindAsync(id);
appointment!.Status.Should().Be(AppointmentStatus.Rescheduled);
```

### When to Write Integration Tests vs Unit Tests

**Integration Tests** - Use for:
- Full HTTP endpoint testing (request → validation → handler → database → response)
- Testing interactions between multiple components
- Verifying database operations and queries
- Testing error responses and problem details
- End-to-end feature validation

**Unit Tests** - Use for:
- Testing individual validators
- Testing domain object behavior in isolation
- Testing helper/utility methods
- Testing business logic without database dependencies
- Testing edge cases with mocked dependencies

### Troubleshooting

**Tests fail with "Entity already tracked"**:
- Don't manually call `InitializeAsync()` in tests - it's called automatically
- Each test gets a fresh DbContext scope

**Tests fail with timezone issues**:
- Use `builder.BuildValues()` to get the actual values being sent
- Compare UTC times: `result.StartUtc.Should().BeCloseTo(expected.UtcDateTime, ...)`

**Tests fail intermittently**:
- Ensure tests don't depend on execution order
- Check for hardcoded dates/times - use relative times (e.g., `DateTimeOffset.UtcNow.AddDays(7)`)
- Verify test isolation - each test should reset database state

## Unit Testing

### Unit Test Overview

Unit tests verify individual components in isolation without external dependencies like databases or HTTP. The project uses xUnit, FluentAssertions, and NSubstitute for mocking.

### Unit Test Infrastructure

**Location**: `tests/Application.UnitTests/`

**Key Components**:

- **xUnit**: Testing framework with `[Fact]` and `[Theory]` attributes
- **FluentAssertions**: Readable assertions with detailed error messages
- **NSubstitute**: Mocking framework for interfaces and dependencies
- **FluentValidation.TestHelper**: Specialized testing for validators

### Running Unit Tests

```bash
# Run all unit tests
dotnet test tests/Application.UnitTests/Application.UnitTests.csproj

# Run specific test class
dotnet test --filter "FullyQualifiedName~AppointmentTests"

# Run specific test method
dotnet test --filter "Complete_ScheduledAppointment_SetsStatusAndTimestamp"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

### Test Categories

#### 1. Domain Object Tests

Test domain entities and their business logic in isolation.

**Location**: `tests/Application.UnitTests/Domain/Healthcare/`

**Example - Testing Domain Methods**:
```csharp
public class AppointmentTests
{
    private static readonly DateTime _baseTimeUtc = DateTime.UtcNow;
    private readonly Guid _patientId = Guid.NewGuid();
    private readonly Guid _doctorId = Guid.NewGuid();
    private readonly DateTime _validStartUtc = _baseTimeUtc.AddHours(1);
    private readonly DateTime _validEndUtc = _baseTimeUtc.AddHours(2);

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
        appointment.Notes.Should().Be("Patient checked in and seen");
    }

    [Fact]
    public void Complete_CancelledAppointment_ThrowsInvalidOperationException()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);
        appointment.Cancel("Patient cancelled");

        // Act & Assert
        var act = () => appointment.Complete("Trying to complete cancelled");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot complete a cancelled appointment");
    }

    [Fact]
    public void Complete_AlreadyCompleted_IsIdempotent()
    {
        // Arrange
        var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);
        appointment.Complete("First completion");
        var firstCompletedUtc = appointment.CompletedUtc;

        // Act - complete again
        appointment.Complete("Second completion");

        // Assert - status remains completed, timestamp unchanged
        appointment.Status.Should().Be(AppointmentStatus.Completed);
        appointment.CompletedUtc.Should().Be(firstCompletedUtc);
        appointment.Notes.Should().Be("First completion"); // Notes not updated
    }
}
```

**Key Patterns**:

- Test state transitions (e.g., Scheduled → Completed)
- Test validation and exceptions
- Test idempotency
- Test edge cases (null values, boundary conditions)
- Test invariant protection

#### 2. Validator Tests

Test FluentValidation validators using FluentValidation.TestHelper.

**Location**: `tests/Application.UnitTests/Scheduling/` or `tests/Application.UnitTests/Medications/`

**Example - Testing Validators**:
```csharp
public class CompleteAppointmentValidatorTests
{
    private readonly CompleteAppointmentCommandValidator _validator;

    public CompleteAppointmentValidatorTests()
    {
        _validator = new CompleteAppointmentCommandValidator();
    }

    [Fact]
    public void Should_Have_Error_When_AppointmentId_Is_Empty()
    {
        // Arrange
        var command = new CompleteAppointmentCommand(
            Guid.Empty,
            "Test notes");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AppointmentId)
            .WithErrorMessage("AppointmentId is required");
    }

    [Fact]
    public void Should_Have_Error_When_Notes_Exceed_1024_Characters()
    {
        // Arrange
        var longNotes = new string('A', 1025);
        var command = new CompleteAppointmentCommand(
            Guid.NewGuid(),
            longNotes);

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Notes)
            .WithErrorMessage("Notes cannot exceed 1024 characters");
    }

    [Fact]
    public void Should_Not_Have_Error_When_All_Fields_Are_Valid()
    {
        // Arrange
        var command = new CompleteAppointmentCommand(
            Guid.NewGuid(),
            "Valid completion notes");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
```

**FluentValidation TestHelper Methods**:

- `TestValidate(command)`: Execute validation
- `ShouldHaveValidationErrorFor(x => x.Property)`: Assert property has error
- `WithErrorMessage(message)`: Assert specific error message
- `ShouldNotHaveValidationErrorFor(x => x.Property)`: Assert property has no error
- `ShouldNotHaveAnyValidationErrors()`: Assert no validation errors

#### 3. Behavior/Pipeline Tests

Test MediatR pipeline behaviors in isolation.

**Location**: `tests/Application.UnitTests/Common/Behaviours/`

**Example - Testing ValidationBehaviour**:
```csharp
public class ValidationBehaviorTests
{
    [Fact]
    public async Task InvokeValidationBehavior_WhenValidatorResultIsNotValid_ShouldReturnListOfErrors()
    {
        // Arrange
        var mockValidator = Substitute.For<IValidator<TestRequest>>();
        var failures = new List<ValidationFailure>
        {
            new ValidationFailure("Property1", "Error 1"),
            new ValidationFailure("Property2", "Error 2")
        };
        mockValidator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), default)
            .Returns(new ValidationResult(failures));

        var behavior = new ValidationBehaviour<TestRequest, ErrorOr<int>>(
            new[] { mockValidator });

        // Act
        var result = await behavior.Handle(
            new TestRequest(),
            () => Task.FromResult(ErrorOr<int>.From(1)),
            CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
        result.Errors[0].Code.Should().Be("Property1");
        result.Errors[1].Code.Should().Be("Property2");
    }
}
```

#### 4. Helper/Utility Tests

Test helper methods and utilities.

**Location**: `tests/Application.UnitTests/Common/`

**Example - Testing MinimalApiProblemHelper**:
```csharp
public class MinimalApiProblemHelperTests
{
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

    [Fact]
    public void Problem_WithSingleValidationError_ReturnsValidationProblem()
    {
        // Arrange
        var errors = new List<Error>
        {
            Error.Validation("Field", "Field is required")
        };

        // Act
        var result = MinimalApiProblemHelper.Problem(errors);

        // Assert
        var problemResult = result.Should().BeOfType<ProblemHttpResult>().Subject;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Extensions.Should().ContainKey("errors");
    }
}
```

### Unit Test Naming Convention

Use descriptive test names following the pattern:
`MethodName_Scenario_ExpectedBehavior`

**Examples**:

- `Complete_ScheduledAppointment_SetsStatusAndTimestamp`
- `Complete_CancelledAppointment_ThrowsInvalidOperationException`
- `Complete_AlreadyCompleted_IsIdempotent`
- `Should_Have_Error_When_AppointmentId_Is_Empty`
- `Problem_WithNotFoundError_ReturnsProblemWithStatus404`

### FluentAssertions Best Practices

FluentAssertions provides readable, expressive assertions:

```csharp
// Basic assertions
result.Should().NotBeNull();
result.Should().Be(expected);
result.Should().BeEquivalentTo(expected);

// Strings
result.Should().Be("expected");
result.Should().Contain("substring");
result.Should().StartWith("prefix");
result.Should().BeNullOrEmpty();

// Numbers
count.Should().Be(5);
value.Should().BeGreaterThan(0);
value.Should().BeLessThanOrEqualTo(100);
value.Should().BeInRange(1, 10);

// DateTime
timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
timestamp.Should().BeOnOrAfter(startTime);

// Collections
list.Should().HaveCount(3);
list.Should().Contain(item);
list.Should().BeEmpty();
list.Should().ContainSingle(x => x.Id == expectedId);

// Exceptions
var act = () => myObject.DoSomething();
act.Should().Throw<InvalidOperationException>()
    .WithMessage("Cannot complete a cancelled appointment");

// Boolean
result.Should().BeTrue();
result.Should().BeFalse();

// Nullability
result.Should().BeNull();
result.Should().NotBeNull();

// Types
result.Should().BeOfType<AppointmentResult>();
result.Should().BeAssignableTo<IResult>();
```

### Mocking with NSubstitute

When testing components with dependencies, use NSubstitute for mocking:

```csharp
// Create mock
var mockService = Substitute.For<IMyService>();

// Setup method return
mockService.GetSomething(Arg.Any<Guid>()).Returns(expectedValue);

// Setup async method
mockService.GetSomethingAsync(Arg.Any<Guid>())
    .Returns(Task.FromResult(expectedValue));

// Setup exception throwing
mockService.DoSomething(Arg.Any<int>())
    .Throws(new InvalidOperationException("Error message"));

// Verify method was called
mockService.Received(1).DoSomething(Arg.Any<int>());
mockService.DidNotReceive().DoSomethingElse();

// Capture arguments
Guid capturedId = Guid.Empty;
mockService.DoSomething(Arg.Do<Guid>(x => capturedId = x));
```

### Testing Domain Events

```csharp
[Fact]
public void Complete_RaisesAppointmentCompletedEvent()
{
    // Arrange
    var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);

    // Act
    appointment.Complete("Completed successfully");

    // Assert
    appointment.DomainEvents.Should().ContainSingle();
    appointment.DomainEvents[0].Should().BeOfType<AppointmentCompletedEvent>();

    var domainEvent = (AppointmentCompletedEvent)appointment.DomainEvents[0];
    domainEvent.AppointmentId.Should().Be(appointment.Id);
    domainEvent.CompletedUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
}
```

### Unit Tests vs Integration Tests - When to Use Which

**Unit Tests** - Use for:

- **Domain object behavior**: Testing business logic, state transitions, validation
- **Validators**: Testing FluentValidation rules in isolation
- **Helper/utility methods**: Pure functions without side effects
- **Pipeline behaviors**: MediatR behaviors with mocked dependencies
- **Value objects**: Immutable objects with equality logic
- **Edge cases**: Boundary conditions, null handling, exceptional paths

**Integration Tests** - Use for:

- **Full HTTP endpoint testing**: Request → validation → handler → database → response
- **Database operations**: Queries, inserts, updates, deletes
- **Feature validation**: Complete user workflows
- **Error responses**: ProblemDetails formatting and status codes
- **Component interactions**: Multiple services working together

**Rule of Thumb**: If it needs a database or HTTP, use integration tests. If it's pure logic, use unit tests.

### Unit Testing Best Practices

1. **Test One Thing**: Each test should verify a single behavior
2. **Arrange-Act-Assert**: Follow AAA pattern for clear test structure
3. **Descriptive Names**: Test names should describe scenario and expected outcome
4. **No Test Logic**: Tests should be simple and straightforward
5. **Fast Execution**: Unit tests should complete in milliseconds
6. **Isolated Tests**: No dependencies on other tests or execution order
7. **No External Dependencies**: Mock databases, HTTP, file system, time
8. **Test Public API**: Test public methods, not implementation details
9. **Avoid Magic Numbers**: Use named constants or variables
10. **Test Edge Cases**: Null, empty, boundary values, exceptions

### Common Unit Test Patterns

**Testing State Transitions**:
```csharp
[Fact]
public void Cancel_ScheduledAppointment_ChangesStatusToCancelled()
{
    // Arrange
    var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);

    // Act
    appointment.Cancel("Patient requested cancellation");

    // Assert
    appointment.Status.Should().Be(AppointmentStatus.Cancelled);
    appointment.CancelledUtc.Should().NotBeNull();
    appointment.CancellationReason.Should().Be("Patient requested cancellation");
}
```

**Testing Validation**:
```csharp
[Fact]
public void Complete_NotesExceed1024Characters_ThrowsArgumentException()
{
    // Arrange
    var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);
    var longNotes = new string('x', 1025);

    // Act & Assert
    var act = () => appointment.Complete(longNotes);
    act.Should().Throw<ArgumentException>()
        .WithMessage("Notes cannot exceed 1024 characters*")
        .And.ParamName.Should().Be("notes");
}
```

**Testing Idempotency**:
```csharp
[Fact]
public void Complete_AlreadyCompleted_DoesNotChangeState()
{
    // Arrange
    var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);
    appointment.Complete("First completion");
    var originalTimestamp = appointment.CompletedUtc;

    // Act - attempt second completion
    appointment.Complete("Second attempt");

    // Assert - state unchanged
    appointment.CompletedUtc.Should().Be(originalTimestamp);
    appointment.Notes.Should().Be("First completion");
}
```

**Testing Invariant Protection**:
```csharp
[Fact]
public void Cancel_CompletedAppointment_ThrowsInvalidOperationException()
{
    // Arrange
    var appointment = Appointment.Schedule(_patientId, _doctorId, _validStartUtc, _validEndUtc);
    appointment.Complete("Completed");

    // Act & Assert
    var act = () => appointment.Cancel("Trying to cancel");
    act.Should().Throw<InvalidOperationException>()
        .WithMessage("Cannot cancel a completed appointment");
}
```

### Unit Test Troubleshooting

**Test fails with "Expected X but found Y"**:

- Check the specific assertion message - FluentAssertions provides detailed output
- Use `.Should().BeEquivalentTo()` for object comparisons (ignores order)
- Use `.Should().Be()` for value comparisons

**Test fails intermittently**:

- Avoid `DateTime.Now` - use fixed times or relative calculations
- Don't depend on test execution order
- Ensure no shared state between tests

**Mock not returning expected value**:

- Check argument matchers: `Arg.Any<T>()`, `Arg.Is<T>(x => x.Id == expectedId)`
- Verify setup is called before test execution
- Use `Returns()` for sync methods, `ReturnsAsync()` for async

**Validator test not finding error**:

- Check property expression: `x => x.PropertyName` must match command property
- Verify validator is registered correctly
- Use `TestValidate()` not `Validate()`

**Tests are slow**:

- Unit tests should be fast (< 100ms each)
- Ensure you're not accidentally hitting database or HTTP
- Check for Thread.Sleep or unnecessary delays
