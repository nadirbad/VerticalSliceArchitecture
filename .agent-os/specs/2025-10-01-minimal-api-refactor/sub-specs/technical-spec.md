# Technical Specification

This is the technical specification for the spec detailed in @.agent-os/specs/2025-10-01-minimal-api-refactor/spec.md

> Created: 2025-10-01
> Version: 1.0.0

## Technical Requirements

### Minimal API Endpoint Structure

- Implement endpoints using route groups with MapGroup() for /api/healthcare prefix
- Use static extension methods on WebApplication/IEndpointRouteBuilder for clean organization
- Separate endpoint definitions into dedicated classes following REPR pattern (Request, Execute, Present, Respond)
- Maintain feature-based file organization within Features/Healthcare/Appointments folder
- Leverage typed Results API (Results<T1, T2, ...>) for compile-time safety

### Error Handling Implementation

- Create MinimalApiProblemHelper static class to replicate ApiControllerBase.Problem() behavior
- Map ErrorOr error types to appropriate HTTP status codes (409 Conflict, 400 Bad Request, 404 Not Found, 403 Forbidden, 500 Internal Server Error)
- Generate RFC 7807 Problem Details responses matching current controller implementation
- Support both single errors and validation error collections with ModelStateDictionary equivalent

### Validation Pipeline Integration

- Implement IEndpointFilter for automatic FluentValidation execution
- Create ValidationFilter<TRequest> that resolves IValidator<TRequest> from DI container
- Return validation errors as Problem Details with 400 status code
- Ensure validation runs before MediatR handler execution
- Maintain current validation error message format and structure

### MediatR Integration Pattern

- Inject ISender directly into endpoint handlers via dependency injection
- Use typed endpoint handlers with explicit parameter binding
- Maintain async/await patterns for all MediatR Send operations
- Preserve command/query separation with appropriate HTTP verbs
- Support route parameter binding alongside request body binding

### Endpoint Organization Strategy

- Create HealthcareEndpoints static class containing MapHealthcareEndpoints extension method
- Group related endpoints (Appointments, future Prescriptions) using nested route groups
- Maintain single-file vertical slice where possible (endpoint + command + validator + handler)
- Use partial classes if file size becomes unwieldy
- Register all Healthcare endpoints with single call in Program.cs

### Response Formatting

- Use TypedResults for all responses (Created, Ok, Problem, ValidationProblem)
- Maintain current response DTOs and contracts
- Preserve Location header generation for created resources
- Support content negotiation through ASP.NET Core defaults
- Ensure OpenAPI/Swagger documentation generation works correctly

### Testing Considerations

- Minimal API endpoints must be testable via WebApplicationFactory
- Maintain ability to unit test validators and handlers independently
- Support integration testing with in-memory database
- Preserve existing test coverage for business logic
- Add new tests for endpoint routing and parameter binding

## External Dependencies

**Microsoft.AspNetCore.Http.Results** - Enhanced typed results for Minimal APIs
- **Justification:** Provides compile-time safety and better IntelliSense for API responses, reducing runtime errors

**Microsoft.AspNetCore.OpenApi** - OpenAPI document generation for Minimal APIs
- **Justification:** Required for Swagger/OpenAPI documentation generation with Minimal APIs, replacing controller-based annotations
