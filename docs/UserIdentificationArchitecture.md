# User Identification in Vertical Slice Architecture

This document explains how user identification and authentication are implemented in this Vertical Slice Architecture project.

## Overview

The user identification system in this architecture is designed to be **flexible and extensible** while following the vertical slice pattern. Currently, the architecture provides the infrastructure for user identification but **does not include a complete authentication system** out of the box.

## Architecture Components

### 1. Core Identity Infrastructure

#### ICurrentUserService Interface
**Location**: `src/Application/Common/Interfaces/ICurrentUserService.cs`

```csharp
public interface ICurrentUserService
{
    string? UserId { get; }
}
```

This interface provides access to the current user's identifier throughout the application.

#### CurrentUserService Implementation
**Location**: `src/Application/Infrastructure/Services/CurrentUserService.cs`

```csharp
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
```

This service extracts the user ID from the current HTTP context's claims. It expects the user to be authenticated using standard ASP.NET Core authentication mechanisms.

### 2. Authorization Infrastructure

#### AuthorizeAttribute
**Location**: `src/Application/Common/Security/AuthorizeAttribute.cs`

A custom authorization attribute that can be applied to MediatR commands/queries:

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class AuthorizeAttribute : Attribute
{
    public string Roles { get; set; } = string.Empty;
    public string Policy { get; set; } = string.Empty;
}
```

#### AuthorizationBehaviour
**Location**: `src/Application/Common/Behaviours/AuthorizationBehaviour.cs`

A MediatR pipeline behavior that automatically checks authorization for requests decorated with the `AuthorizeAttribute`:

```csharp
public class AuthorizationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var authorizeAttributes = request.GetType().GetCustomAttributes<AuthorizeAttribute>();

        if (authorizeAttributes.Any())
        {
            // Must be authenticated user
            if (_currentUserService.UserId == null)
            {
                throw new UnauthorizedAccessException();
            }
        }

        // User is authorized / authorization not required
        return next();
    }
}
```

### 3. Auditing Integration

#### Database Context Integration
**Location**: `src/Application/Infrastructure/Persistence/ApplicationDbContext.cs`

The `ApplicationDbContext` automatically tracks user information in audit fields:

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
    {
        switch (entry.State)
        {
            case EntityState.Added:
                entry.Entity.CreatedBy = _currentUserService.UserId;
                entry.Entity.Created = _dateTime.Now;
                break;
            case EntityState.Modified:
                entry.Entity.LastModifiedBy = _currentUserService.UserId;
                entry.Entity.LastModified = _dateTime.Now;
                break;
        }
    }
    
    // ... domain events handling
    return await base.SaveChangesAsync(cancellationToken);
}
```

#### AuditableEntity Base Class
**Location**: `src/Application/Common/AuditableEntity.cs`

```csharp
public abstract class AuditableEntity
{
    public DateTime Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}
```

### 4. Dependency Injection Configuration

**Location**: `src/Application/ConfigureServices.cs`

```csharp
public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
{
    // ... other services

    services.AddSingleton<ICurrentUserService, CurrentUserService>();

    return services;
}

public static IServiceCollection AddApplication(this IServiceCollection services)
{
    services.AddMediatR(options =>
    {
        // ... other behaviors
        options.AddOpenBehavior(typeof(AuthorizationBehaviour<,>));
    });

    return services;
}
```

**Location**: `src/Api/Program.cs`

```csharp
builder.Services.AddHttpContextAccessor(); // Required for CurrentUserService
```

## Current State: What's Missing

The current architecture provides the **infrastructure** for user identification but does **NOT** include:

1. **Authentication endpoints** (login, register, logout)
2. **User management features** (user entities, user creation, password management)
3. **JWT token handling** or other authentication mechanisms
4. **Identity provider integration** (ASP.NET Core Identity, IdentityServer, Auth0, etc.)

## How User Identification Works

### Without Authentication (Current Default)
- `CurrentUserService.UserId` returns `null`
- No authorization checks are enforced
- Audit fields (`CreatedBy`, `LastModifiedBy`) are set to `null`

### With Authentication (When Implemented)
1. User authenticates through your chosen authentication mechanism
2. Authentication middleware sets claims in `HttpContext.User`
3. `CurrentUserService` extracts the `NameIdentifier` claim
4. Authorization behavior checks user authentication for protected endpoints
5. Audit fields are automatically populated with the user ID

## Implementation Examples

### Example 1: Protecting a Feature with Authorization

```csharp
// In a feature file, e.g., Features/TodoItems/CreateTodoItem.cs

[Authorize] // Require authentication
public record CreateTodoItemCommand(int ListId, string? Title) : IRequest<ErrorOr<int>>;

// Or with roles
[Authorize(Roles = "Admin,Manager")]
public record DeleteAllTodoItemsCommand : IRequest<ErrorOr<Unit>>;
```

### Example 2: Accessing Current User in a Handler

```csharp
public class CreateTodoItemHandler : IRequestHandler<CreateTodoItemCommand, ErrorOr<int>>
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateTodoItemHandler(ApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ErrorOr<int>> Handle(CreateTodoItemCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        
        // Use currentUserId for business logic, filtering, etc.
        var todoItem = new TodoItem
        {
            Title = request.Title,
            ListId = request.ListId,
            // CreatedBy will be automatically set by ApplicationDbContext
        };

        _context.TodoItems.Add(todoItem);
        await _context.SaveChangesAsync(cancellationToken);

        return todoItem.Id;
    }
}
```

## Integration Points for Authentication

To add complete user authentication to this architecture, you would typically:

### Option 1: ASP.NET Core Identity
Add Identity to `Program.cs` and create user management features following the vertical slice pattern.

### Option 2: JWT Bearer Authentication
Configure JWT authentication in `Program.cs` and create login/register endpoints.

### Option 3: External Identity Provider
Integrate with Auth0, Azure AD, or similar services.

The beauty of this architecture is that the user identification infrastructure is already in place - you just need to add your preferred authentication mechanism, and everything else will work automatically.

## Testing Support

The testing infrastructure includes mock support for user identification:

**Location**: `tests/Application.IntegrationTests/Testing.cs`

```csharp
public static Task<string> RunAsDefaultUserAsync()
{
    s_currentUserId = "test@local";
    return Task.FromResult(s_currentUserId);
}
```

This allows you to test features that require user authentication by simulating a logged-in user.

## Summary

The user identification in this Vertical Slice Architecture is positioned as:

1. **Cross-cutting concern** handled by infrastructure services
2. **Automatic auditing** through the database context
3. **Authorization enforcement** through MediatR pipeline behaviors
4. **Flexible foundation** ready for any authentication mechanism

The architecture provides all the plumbing for user identification while leaving the choice of authentication method to the implementer, maintaining the flexibility and modularity that Vertical Slice Architecture is known for.