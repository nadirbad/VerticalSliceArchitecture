# Technical Stack

## Application Framework

**.NET 9** - Latest version of Microsoft's cross-platform framework for building modern applications

## Backend Technologies

### Web Framework

**ASP.NET Core 9** - High-performance web framework for building APIs

- Minimal API hosting model
- Built-in dependency injection
- Middleware pipeline for cross-cutting concerns

### CQRS & Mediator

**MediatR** - Simple mediator implementation for in-process messaging

- Command/Query separation
- Pipeline behaviors for cross-cutting concerns (validation, logging, performance monitoring, authorization)
- Request/response pattern

### Validation

**FluentValidation** - Popular .NET library for building strongly-typed validation rules

- Declarative validation syntax
- Automatic registration with `includeInternalTypes: true`
- Integration with ASP.NET Core pipeline

### Error Handling

**ErrorOr** - Result pattern library for handling errors without exceptions

- Type-safe error handling
- Supports multiple error types (Validation, NotFound, Conflict, etc.)
- Composable error results

## Data Access

### ORM

**Entity Framework Core 9** - Modern object-relational mapper

- Code-first approach
- Migration support
- Change tracking and unit of work pattern
- Direct `DbContext` access (no repository pattern)

### Database

**SQL Server** (production) / **In-Memory Database** (development/testing)

- Toggle via `UseInMemoryDatabase` configuration flag
- SQL Server connection string in appsettings.json
- Azure SQL Edge support for cross-platform development (including Apple Silicon M1/M2)

### Database Configuration

- Default: In-memory database for immediate run without infrastructure
- Production: SQL Server with connection string configuration
- Migrations: Stored in `src/Application/Infrastructure/Persistence/Migrations`

## Project Structure

### Solution Organization

```
src/
├── Api/              # ASP.NET Core entry point
│   ├── Program.cs    # DI, middleware, hosting configuration
│   └── appsettings.json
└── Application/      # All business logic
    ├── Features/     # Vertical slices by business feature
    │   ├── Healthcare/
    │   │   ├── Appointments/
    │   │   └── Prescriptions/
    │   ├── TodoItems/
    │   └── TodoLists/
    ├── Domain/       # Domain entities and value objects
    │   ├── Healthcare/
    │   └── Todos/
    ├── Infrastructure/
    │   └── Persistence/
    └── Common/       # Shared concerns
```

## Testing

### Unit Testing

**xUnit** - Modern testing framework for .NET

- Fact and Theory attributes
- Test isolation
- Parallel test execution

### Assertion Library

**FluentAssertions** - Readable assertion library

- Natural language assertions
- Detailed failure messages

### Mocking

**Moq** - Popular mocking library for .NET

- Interface and virtual method mocking
- Setup and verification

### Integration Testing

- WebApplicationFactory for in-memory API testing
- In-memory database for data access testing
- Full request/response pipeline testing

## Development Tools

### API Documentation

**Swagger/OpenAPI** - Interactive API documentation

- Available at root URL (/) in development
- Automatic schema generation from controllers and DTOs

### HTTP Testing

**HTTP Files** - Manual API testing via `.http` files

- Located in `requests/**` directory
- Works with REST Client VS Code extension

### Code Quality

#### Style Enforcement

- **EditorConfig** - Code style and formatting rules
- **StyleCop Analyzers** - Additional code style rules
- **EnforceCodeStyleInBuild=true** - Build-time style checking
- **TreatWarningsAsErrors=true** - Enforce zero warnings

#### Formatting

**dotnet format** - Built-in code formatter

- Style formatting
- Analyzer rule enforcement
- `--verify-no-changes` for CI/CD validation

## Hosting & Deployment

### Application Hosting

Local development (Kestrel) / Deployment target TBD

- Default ports: HTTP 5000, HTTPS 7098
- Swagger UI at <https://localhost:7098/>

### Database Hosting

Local development: In-memory or SQL Server in Docker (Azure SQL Edge)
Production: SQL Server / Azure SQL Database

### Asset Hosting

N/A (API-only application)

### Deployment Solution

Standard .NET publish process

- `dotnet publish --configuration Release`
- Self-contained or framework-dependent deployments supported

## Code Repository

**GitHub**: <https://github.com/nadirbad/VerticalSliceArchitecture>

## Architecture Patterns

### Vertical Slice Architecture

- Feature-first organization
- All related code in single files (controller, command/query, validator, handler)
- Controllers inherit `ApiControllerBase`
- Explicit route attributes on endpoints

### Domain-Driven Design Principles

- Rich domain models with private setters
- Factory methods for object creation
- Domain events for cross-aggregate communication
- Encapsulation and invariant protection

### CQRS Pattern

- Commands: `IRequest<ErrorOr<T>>` for mutations
- Queries: `IRequest<ErrorOr<ViewModel>>` for reads
- Separate handlers for each operation

### No Repository Pattern

- Direct `DbContext` access in handlers
- `AsNoTracking()` for read-only queries
- EF Core provides built-in unit of work

## Import Strategy

N/A (Backend API only, no frontend JavaScript)

## CSS Framework

N/A (Backend API only)

## UI Component Library

N/A (Backend API only)

## Fonts Provider

N/A (Backend API only)

## Icon Library

N/A (Backend API only)
