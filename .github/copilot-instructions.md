# Copilot instructions for VerticalSliceArchitecture

Use these project-specific guidelines to propose changes and generate code that fits this repo.

## Big picture

- Solution has 2 projects:
  - `src/Api` (ASP.NET Core entrypoint hosting DI, middleware, Swagger). Controllers live in the Application project.
  - `src/Application` (all features, domain, infrastructure, and shared concerns). Code is organized by vertical slices under `Features/**`.
- Each feature keeps the HTTP endpoint, request/response types, validation, and MediatR handler together in one file. Example: `Features/TodoItems/CreateTodoItem.cs` contains:
  - `CreateTodoItemController : ApiControllerBase` with explicit route attribute
  - `record CreateTodoItemCommand : IRequest<ErrorOr<int>>`
  - `AbstractValidator<CreateTodoItemCommand>` (FluentValidation)
  - `IRequestHandler<,>` that uses `ApplicationDbContext`
- Cross-cutting behaviors are provided via MediatR pipeline: `AuthorizationBehaviour`, `PerformanceBehaviour`, `ValidationBehaviour`.
- Persistence via EF Core in `Infrastructure/Persistence/ApplicationDbContext.cs`. Domain events are collected and dispatched after `SaveChangesAsync`.
- Default DB is in-memory; set `UseInMemoryDatabase=false` and configure `DefaultConnection` in `src/Api/appsettings.json` to use SQL Server.

## Conventions and patterns

- Add new endpoints under `src/Application/Features/<Feature>/<Action>.cs`. Keep controller, command/query, validator, handler, and DTOs together unless the file becomes unwieldy.
- Controllers should inherit `ApiControllerBase` and typically use explicit absolute routes, e.g. `[HttpPost("/api/todo-items")]` or `[HttpGet("/api/todo-lists")]` (see existing features).
- Handlers access data via `ApplicationDbContext` directly; no repository abstraction. Prefer `AsNoTracking()` for queries.
- Return types use ErrorOr: commands return `ErrorOr<T>`, queries return `ErrorOr<VM>` and map entities to read models/records in the same file.
- Add validators with `includeInternalTypes: true` (already wired). Validation errors flow via `ErrorOr` and `ApiControllerBase.Problem`.
- Use domain events from entities (e.g., add `TodoItemCreatedEvent` to `entity.DomainEvents`). Handlers for domain events live under `Features/.../EventHandlers`.

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

## Key files

- `src/Api/Program.cs` – DI, middleware (Swagger, CORS any-origin, ProblemDetails), health checks.
- `src/Application/ConfigureServices.cs` – registers MediatR + pipeline behaviors + validators; configures EF Core (in-memory or SQL Server).
- `src/Application/Common/ApiControllerBase.cs` – Mediator access and consistent error handling using ErrorOr.
- `src/Application/Infrastructure/Persistence/ApplicationDbContext.cs` – DbSets, auditing, domain event dispatch.

## Developer workflows

- Build: `dotnet build` (or VS Code task "build").
- Run API: `dotnet run --project src/Api/Api.csproj` (or task "watch"). Swagger UI is at `/`.
- Tests: `dotnet test tests/Application.UnitTests/Application.UnitTests.csproj`; integration tests require a SQL instance.
- EF Core migrations (from repo root):
  - Add: `dotnet ef migrations add <Name> --project src/Application --startup-project src/Api --output-dir Infrastructure/Persistence/Migrations`
  - Update DB: `dotnet ef database update --project src/Application --startup-project src/Api`
- Formatting/analysis: `dotnet format` (or `--verify-no-changes`).

## Integration points

- Packages: MediatR, FluentValidation, ErrorOr, EF Core, Swashbuckle (Swagger).
- Sample REST requests are under `requests/**` for quick manual testing.

## When adding a new feature

- Create a single file under `Features/<Area>/<VerbNoun>.cs` that mirrors the existing Todo examples.
- Use explicit routes, ErrorOr results, validators, and `ApplicationDbContext`. Add domain events if applicable.
- Keep controllers thin; push logic into the handler, and map entities to DTOs/VMs in the same file.
