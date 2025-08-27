# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Architecture Overview

This is a .NET 9 Vertical Slice Architecture example that organizes code by features rather than technical layers. The solution consists of 2 projects:

- **src/Api**: ASP.NET Core entry point with DI, middleware, and hosting. Controllers live in the Application project.
- **src/Application**: Contains all features, domain entities, infrastructure, and shared concerns organized under `Features/**` by vertical slices.

Each feature keeps HTTP endpoints, request/response types, validation, and MediatR handlers together in a single file (e.g., `Features/TodoItems/CreateTodoItem.cs` contains controller, command, validator, and handler).

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

1. Create file under `src/Application/Features/<Area>/<VerbNoun>.cs`
2. Include all related code in one file: controller, command/query, validator, handler, and DTOs
3. Controllers inherit `ApiControllerBase` and use explicit routes (e.g., `[HttpPost("/api/todo-items")]`)
4. Use `ErrorOr<T>` return types for consistent error handling
5. Access data via `ApplicationDbContext` directly (no repository pattern)
6. Add validators with FluentValidation (auto-registered with `includeInternalTypes: true`)

### Example Feature Structure

```csharp
// Controller
public class CreateTodoItemController : ApiControllerBase
{
    [HttpPost("/api/todo-items")]
    public async Task<IActionResult> Create(CreateTodoItemCommand command) { /* ... */ }
}

// Command/Query
public record CreateTodoItemCommand(int ListId, string? Title) : IRequest<ErrorOr<int>>;

// Validator
internal sealed class CreateTodoItemCommandValidator : AbstractValidator<CreateTodoItemCommand> { /* ... */ }

// Handler
internal sealed class CreateTodoItemCommandHandler : IRequestHandler<CreateTodoItemCommand, ErrorOr<int>> { /* ... */ }

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

Example Structure

```csharp
public class DomainEntity
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public Status Status { get; private set; }

    public DomainEntity(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        Id = Guid.NewGuid();
        Name = name;
        Status = Status.Active;
    }

    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Name cannot be empty", nameof(newName));

        Name = newName;
    }

    public void Deactivate()
    {
        if (Status == Status.Inactive)
            throw new InvalidOperationException("Entity is already inactive");

        Status = Status.Inactive;
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
- **Domain Events**: Raised by entities, handled by separate event handlers under `Features/.../EventHandlers`

## Configuration and Setup

- **Database**: Defaults to in-memory; toggle via `UseInMemoryDatabase` in appsettings.json
- **Swagger**: Available at root URL (/) in development
- **CORS**: Configured to allow any origin for development
- **Health Checks**: Available at `/health` endpoint
- **Sample Requests**: HTTP files in `requests/**` for manual testing

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