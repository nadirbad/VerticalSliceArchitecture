# Product Mission

## Pitch

Vertical Slice Architecture Template is a reference implementation that helps .NET developers learn and adopt Vertical Slice Architecture by providing a realistic healthcare domain example demonstrating feature-oriented code organization, rich domain modeling, and modern .NET patterns.

## Users

### Primary Customers

- **Software Developers**: .NET developers seeking practical examples of Vertical Slice Architecture beyond simple CRUD applications
- **Technical Architects**: Architecture decision-makers evaluating VSA as an alternative to traditional layered or Clean Architecture patterns
- **Development Teams**: Teams looking for a starting template to build maintainable, feature-focused applications

### User Personas

**Senior .NET Developer** (30-45 years old)

- **Role:** Lead Developer / Tech Lead
- **Context:** Working on medium to large enterprise applications, experienced with Clean Architecture and DDD but seeking simpler alternatives
- **Pain Points:** Clean Architecture feels over-engineered for most features, too much ceremony jumping between layers, repository pattern adds unnecessary abstraction
- **Goals:** Find a pragmatic architecture that maintains good practices without excessive boilerplate, organize code by business features rather than technical layers

**Mid-Level Developer** (25-35 years old)

- **Role:** Full-Stack Developer / Backend Developer
- **Context:** Building modern web APIs, learning architectural patterns beyond basic MVC
- **Pain Points:** Difficulty understanding where code should live in layered architectures, confusion about repository vs service patterns, seeking clear examples
- **Goals:** Learn practical patterns for organizing feature code, understand domain-driven design principles without overwhelming complexity

**Solutions Architect** (35-50 years old)

- **Role:** Enterprise Architect / Solutions Architect
- **Context:** Responsible for establishing architectural standards across development teams
- **Pain Points:** Need to evaluate different architectural patterns for team adoption, balance between structure and developer velocity
- **Goals:** Understand trade-offs between VSA and traditional architectures, assess maintainability and scalability implications

## The Problem

### Over-Engineered Layered Architectures

Many .NET applications adopt Clean Architecture or n-tier patterns that introduce significant complexity even for simple features. Developers must navigate multiple projects, layers (controllers, services, repositories, domain), and abstractions to implement a single feature. This results in decreased developer productivity and increased cognitive load, especially for teams building standard CRUD applications that don't require such separation.

**Our Solution:** Organize code by vertical feature slices where all related code (endpoint, command/query, validation, handler, domain logic) lives together in a single file, eliminating unnecessary layer jumping.

### Lack of Realistic Architecture Examples

Most architecture templates demonstrate patterns using overly simplistic domains (Todo lists, blog posts) that don't showcase how patterns scale to real-world business complexity. Developers struggle to apply learned patterns when facing realistic business rules, complex validations, and domain events.

**Our Solution:** Provide a healthcare domain example with realistic business scenarios (appointment scheduling with conflict detection, prescription management with refill workflows, audit trails) that demonstrates how VSA handles complexity.

### Repository Pattern Overhead

Traditional approaches mandate repository abstractions even when using modern ORMs like Entity Framework Core that already provide unit of work and change tracking. This doubles the abstraction layers and creates maintenance overhead without clear benefits for most applications.

**Our Solution:** Access `DbContext` directly in handlers, leveraging EF Core's built-in patterns while maintaining testability through in-memory databases and integration tests.

### Unclear Domain Modeling Guidance

Many templates show anemic domain models (properties only, no behavior) or don't demonstrate proper encapsulation, invariant protection, and rich domain modeling in C#. Developers end up with services doing all the logic instead of rich domain objects.

**Our Solution:** Demonstrate proper domain entity design with private setters, factory methods, business rule enforcement, domain events, and clear encapsulation patterns following DDD principles.

## Differentiators

### Feature-First Organization

Unlike Clean Architecture templates that organize by technical layers (Controllers, Services, Repositories, Domain), we organize by business features with all related code in single files. This results in dramatically reduced file navigation—changing a feature touches 1-2 files instead of 4-6 files across multiple projects.

### Real-World Domain Complexity

Unlike Todo-list examples that demonstrate only basic CRUD, we showcase healthcare appointment scheduling with overlapping time conflict detection, prescription workflows with refill limits and expiration handling, and proper audit trails. This demonstrates how patterns scale to production business requirements.

### Pragmatic Over Dogmatic

Unlike architecture templates that mandate abstractions "just in case" (repositories, mediator at boundaries, strict layer separation), we follow "You Aren't Gonna Need It" (YAGNI) and adopt the simplest solution that works—starting with Transaction Script and refactoring to patterns only when code smells emerge, as advocated by Jimmy Bogard.

## Key Features

### Core Architecture Features

- **Vertical Slice Organization:** Features organized under `Features/[Area]/[VerbNoun].cs` with controller, command/query, validator, and handler in single files
- **Rich Domain Models:** Domain entities with private setters, factory methods, business rule enforcement, and invariant protection
- **CQRS with MediatR:** Clear separation of commands and queries using MediatR request/response pattern
- **Result-Based Error Handling:** Consistent error handling using ErrorOr pattern instead of exceptions for business rule violations

### Healthcare Domain Features

- **Appointment Booking:** Schedule appointments with doctor availability conflict detection and time window validation
- **Appointment Rescheduling:** Reschedule existing appointments with audit trail of changes
- **Prescription Issuance:** Doctors issue prescriptions with dosage, directions, expiration dates, and refill limits
- **Medication Refills:** Patients request prescription refills with approval/denial workflow

### Development Experience Features

- **In-Memory Database Default:** Run and test immediately without SQL Server setup, toggle to SQL Server via configuration
- **FluentValidation Integration:** Automatic request validation with clear error messages in pipeline behavior
- **Domain Events:** Entities raise domain events automatically dispatched after SaveChanges for cross-feature communication
- **Pipeline Behaviors:** Cross-cutting concerns (logging, validation, performance monitoring, authorization) handled via MediatR pipeline

### Testing & Quality Features

- **Comprehensive Test Coverage:** Unit tests for business logic, integration tests for end-to-end feature scenarios
- **HTTP Request Files:** Manual testing examples in `requests/**/*.http` files for quick API exploration
- **Code Style Enforcement:** EditorConfig with .NET code style rules, StyleCop analyzers, `TreatWarningsAsErrors=true`
- **Watch Mode Development:** `dotnet watch` for rapid feedback during development
