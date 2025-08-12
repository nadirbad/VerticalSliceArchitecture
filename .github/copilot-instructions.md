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
