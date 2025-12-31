# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Architecture Overview

This is a .NET 9 Vertical Slice Architecture example that organizes code by features rather than technical layers. The solution consists of 2 projects:

- **src/Api**: ASP.NET Core entry point with DI, middleware, and hosting
- **src/Application**: Contains all features, domain entities, infrastructure, and shared concerns organized by vertical slices

The project uses **.NET 9 Minimal APIs** instead of MVC controllers. Each feature keeps endpoint handlers, request/response types, validation, and MediatR handlers together in a single file (e.g., `Scheduling/BookAppointment.cs` contains endpoint, command, validator, and handler).

### Current Domain: Healthcare (Appointments)

The codebase implements a simplified Healthcare domain focused on appointment scheduling:

- **Scheduling**: Appointment booking, completion, cancellation, and querying

### Project Structure

```text
src/Application/
├── Domain/                    # Domain entities, value objects, events
│   ├── Appointment.cs
│   ├── Patient.cs
│   ├── Doctor.cs
│   ├── SchedulingPolicies.cs
│   └── Events/
│       ├── AppointmentBookedEvent.cs
│       └── AppointmentCompletedEvent.cs
├── Scheduling/                # Appointment feature slice
│   ├── AppointmentEndpoints.cs
│   ├── BookAppointment.cs
│   ├── CompleteAppointment.cs
│   ├── CancelAppointment.cs
│   ├── GetAppointments.cs
│   └── GetAppointmentById.cs
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
            success => Results.Created($"/api/appointments/{success.Id}", success),
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

        group.MapPost("/{appointmentId}/complete", CompleteAppointmentEndpoint.Handle)
            .WithName("CompleteAppointment");

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

        apiGroup.MapGroup("/appointments")
            .WithTags("Appointments")
            .MapAppointmentEndpoints();

        return app;
    }
}
```

### Domain Objects

When writing domain objects in C#, follow these patterns and principles:

#### Property Design

- Use public get; private set; for all domain properties
- Only use public get; set; for DTOs, view models, or simple data containers

#### Constructor Patterns

- Create constructors that establish valid object state from the beginning
- Use constructor parameters for required properties
- Validate all inputs in constructors and throw descriptive exceptions for invalid data
- Consider using factory methods for complex object creation

#### Encapsulation

- Keep business logic inside the domain object
- Expose behavior through methods, not public setters
- Protect object invariants at all times

#### Method Design

- Create methods that represent business operations (e.g., Complete(), Cancel())
- Methods should maintain object validity and enforce business rules
- Use descriptive method names that reflect domain language
- Return domain events or results rather than void when appropriate

Example Structure (from Appointment domain entity)

```csharp
public class Appointment : AuditableEntity, IHasDomainEvent
{
    // Factory method - preferred over public constructor
    public static Appointment Schedule(Guid patientId, Guid doctorId, DateTime startUtc, DateTime endUtc, string? notes = null)
    {
        var appointment = new Appointment(patientId, doctorId, startUtc, endUtc, notes);
        appointment.DomainEvents.Add(new AppointmentBookedEvent(...));
        return appointment;
    }

    // Private constructor enforces use of factory method
    private Appointment(Guid patientId, Guid doctorId, DateTime startUtc, DateTime endUtc, string? notes)
    {
        // Validation and initialization...
    }

    public Guid Id { get; internal set; }
    public Guid PatientId { get; private set; }
    public AppointmentStatus Status { get; private set; }

    [NotMapped]
    public List<DomainEvent> DomainEvents { get; } = new List<DomainEvent>();

    public void Complete(string? notes = null, DateTime? completedAtUtc = null)
    {
        if (Status == AppointmentStatus.Cancelled)
            throw new InvalidOperationException("Cannot complete a cancelled appointment");

        if (Status == AppointmentStatus.Completed)
            return; // Idempotent

        Status = AppointmentStatus.Completed;
        CompletedUtc = completedAtUtc ?? DateTime.UtcNow;
        DomainEvents.Add(new AppointmentCompletedEvent(...));
    }

    public void Cancel(string reason, DateTime? cancelledAtUtc = null)
    {
        if (Status == AppointmentStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed appointment");

        if (Status == AppointmentStatus.Cancelled)
            return; // Idempotent

        Status = AppointmentStatus.Cancelled;
        CancelledUtc = cancelledAtUtc ?? DateTime.UtcNow;
    }
}
```

## Key Technologies and Patterns

- **MediatR**: Request/response pattern with pipeline behaviors for cross-cutting concerns
- **FluentValidation**: Request validation with automatic registration
- **ErrorOr**: Consistent error handling without exceptions
- **Entity Framework Core**: Data access with domain event dispatching
- **Domain Events**: Raised by entities, dispatched after SaveChangesAsync()
- **Minimal APIs**: .NET 9 endpoint routing with `RouteGroupBuilder` and `ValidationFilter<T>`

## Configuration and Setup

- **Database**: Defaults to in-memory; toggle via `UseInMemoryDatabase` in appsettings.json
- **Swagger**: Available at root URL (/) in development
- **Health Checks**: Available at `/health` endpoint
- **Sample Requests**: HTTP files in `requests/Healthcare/Appointments/` for manual testing

## Pipeline Behaviors

Cross-cutting concerns handled via MediatR pipeline:

- `PerformanceBehaviour`: Performance monitoring (logs slow requests > 500ms)
- `ValidationBehaviour`: FluentValidation integration
- `LoggingBehaviour`: Request/response logging

## Code Quality Settings

The project enforces:
- `TreatWarningsAsErrors=true`
- `EnforceCodeStyleInBuild=true`
- StyleCop analyzers for consistent code style
- EditorConfig for formatting standards

## Testing

### Integration Tests

Integration tests verify the complete request/response cycle including HTTP endpoints, validation, business logic, and database operations.

**Location**: `tests/Application.IntegrationTests/`

**Key Components**:
- `CustomWebApplicationFactory`: Configures in-memory database for testing
- `IntegrationTestBase`: Base class providing test lifecycle management
- `ResponseHelper`: Utilities for parsing ProblemDetails responses

**Running Tests**:

```bash
dotnet test tests/Application.IntegrationTests/Application.IntegrationTests.csproj
dotnet test --filter "FullyQualifiedName~BookAppointmentTests"
```

**Test Data Builders**:

- `BookAppointmentTestDataBuilder`: For appointment booking
- `CompleteAppointmentTestDataBuilder`: For appointment completion
- `CancelAppointmentTestDataBuilder`: For appointment cancellation

### Unit Tests

Unit tests verify individual components in isolation.

**Location**: `tests/Application.UnitTests/`

**Key Components**:

- xUnit testing framework
- FluentAssertions for readable assertions
- NSubstitute for mocking
- FluentValidation.TestHelper for validator testing

**Running Tests**:

```bash
dotnet test tests/Application.UnitTests/Application.UnitTests.csproj
dotnet test --filter "FullyQualifiedName~AppointmentTests"
```

### Test Naming Convention

Use descriptive test names following the pattern: `MethodName_Scenario_ExpectedBehavior`

Examples:
- `BookAppointment_WithValidData_Returns201Created`
- `Complete_CancelledAppointment_ThrowsInvalidOperationException`
- `Should_Have_Error_When_PatientId_Is_Empty`

### When to Use Which

**Unit Tests**: Domain object behavior, validators, helper methods, pipeline behaviors
**Integration Tests**: HTTP endpoints, database operations, complete feature workflows
