# Spec Requirements Document

> Spec: Minimal API Refactor
> Created: 2025-10-01
> Status: Planning

## Overview

Refactor existing Healthcare feature endpoints from ASP.NET Core controllers to .NET 9 Minimal APIs while maintaining the current error handling, validation, and MediatR integration patterns. This migration will leverage Minimal API best practices to achieve cleaner, more maintainable code with reduced complexity and improved performance.

## User Stories

### Healthcare API Consumer Migration

As a healthcare API consumer, I want to continue using the same API endpoints with identical behavior, so that my existing integrations remain functional without modifications.

The refactored endpoints will maintain the exact same HTTP routes, request/response contracts, and error handling behavior. Consumers will experience no breaking changes - only potentially improved performance. All existing validation rules, authorization checks, and business logic will remain intact through the MediatR handlers.

### Developer Experience Enhancement

As a developer maintaining the VSA template, I want to work with cleaner, more focused endpoint definitions using Minimal APIs, so that I can understand and modify the codebase more efficiently.

The new Minimal API structure will provide better separation of concerns with endpoint definitions clearly separated from business logic. Developers will benefit from reduced boilerplate code, improved testability through better isolation, and alignment with modern .NET 9 patterns while preserving the vertical slice architecture principles.

## Spec Scope

1. **Endpoint Migration Strategy** - Convert BookAppointment and RescheduleAppointment controllers to Minimal API endpoints with route groups
2. **Error Handling Adapter** - Create a reusable error handling mechanism that preserves ApiControllerBase.Problem() behavior for Minimal APIs
3. **Validation Pipeline Integration** - Ensure FluentValidation continues to work seamlessly with automatic validation through filters or middleware
4. **MediatR Integration Pattern** - Establish a clean pattern for sending MediatR commands/queries from Minimal API endpoints
5. **Feature Organization Structure** - Define how Minimal API endpoints fit within the existing vertical slice file organization

## Out of Scope

- Migration of Todo feature endpoints (reference implementation remains with controllers)
- Changes to existing MediatR handlers, validators, or domain logic
- Modifications to the underlying infrastructure or persistence layer
- Authentication/authorization implementation changes
- Breaking changes to API contracts or routes

## Expected Deliverable

1. Healthcare endpoints (BookAppointment, RescheduleAppointment) successfully migrated to Minimal APIs with identical API behavior
2. Reusable error handling utilities that match current Problem Details responses for validation, conflicts, and other errors
3. Clean endpoint organization pattern that can be extended for future Healthcare features while maintaining VSA principles

## Spec Documentation

- Tasks: @.agent-os/specs/2025-10-01-minimal-api-refactor/tasks.md
- Technical Specification: @.agent-os/specs/2025-10-01-minimal-api-refactor/sub-specs/technical-spec.md
