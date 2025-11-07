# .NET Code Style Guide

This document defines the C# code style standards for this .NET 9 Vertical Slice Architecture project.

## General Principles

- Follow the EditorConfig settings defined in [.editorconfig](/.editorconfig)
- Use `dotnet format` to ensure consistent formatting before committing
- Treat warnings as errors (`TreatWarningsAsErrors=true`)
- Enforce code style in build (`EnforceCodeStyleInBuild=true`)
- StyleCop analyzers are enabled for consistent code style

## File Organization

### Usings
- Place `using` directives outside namespace
- Separate import directive groups
- Sort System directives first
- Remove unnecessary usings

**Example:**
```csharp
using ErrorOr;

using FluentValidation;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using VerticalSliceArchitecture.Application.Common;
using VerticalSliceArchitecture.Application.Domain.Todos;
using VerticalSliceArchitecture.Application.Infrastructure.Persistence;

namespace VerticalSliceArchitecture.Application.Features.TodoItems;
```

## Naming Conventions

### Types and Namespaces
- **PascalCase** for classes, interfaces, structs, enums, methods, properties, events
- Interfaces: prefix with `I` (e.g., `IRequest`, `IRequestHandler`)
- Type parameters: prefix with `T` (e.g., `TEntity`, `TResult`)

### Fields
- **PascalCase** for public fields
- **_camelCase** for private fields (prefix with underscore)
- **s_camelCase** for private static fields (prefix with `s_`)
- **PascalCase** for constants (public and private)
- **PascalCase** for static readonly fields

### Local Variables and Parameters
- **camelCase** for local variables, parameters, and local constants

**Examples:**
```csharp
public class TodoItem
{
    // Public property
    public int Id { get; private set; }

    // Private field
    private readonly ApplicationDbContext _context;

    // Private static field
    private static readonly string s_defaultStatus = "Pending";

    // Constant
    private const int MaxTitleLength = 200;

    // Method parameter
    public void UpdateTitle(string newTitle)
    {
        // Local variable
        var trimmedTitle = newTitle.Trim();
    }
}
```

## Indentation and Formatting

### Indentation
- Use **4 spaces** for indentation (no tabs)
- Indent block contents
- Indent case contents
- Indent switch labels
- Labels one less than current indentation

### Braces
- **Allman style**: braces on new line for all constructs
- Always use braces, even for single-line blocks

```csharp
if (condition)
{
    DoSomething();
}
else
{
    DoSomethingElse();
}
```

### Spacing
- Space after keywords in control flow statements: `if (`, `for (`, `while (`
- Space after commas: `Foo(x, y, z)`
- Space after semicolons in for statements: `for (int i = 0; i < 10; i++)`
- Space around binary operators: `x + y`, `a == b`
- No space after cast: `(int)value`
- No space before/after dots: `foo.Bar()`
- No space before opening square brackets: `array[0]`

### Line Breaks
- New line before `catch`, `else`, `finally`
- New line before open brace for all constructs
- New line between members in anonymous types and object initializers

## Type and Language Features

### var vs Explicit Types
- Use explicit types (avoid `var`) except when type is apparent
- Prefer `string` over `String`, `int` over `Int32` (language keywords vs BCL types)

```csharp
// Good
int count = 10;
string name = "Todo";
TodoItem item = new TodoItem();

// Acceptable when type is obvious
var items = new List<TodoItem>();
```

### Expression-Bodied Members
- Use for accessors, indexers, lambdas, and properties
- Avoid for constructors, methods, operators, and local functions

```csharp
// Good
public string Name { get; private set; }
public int Count => _items.Count;

// Avoid
public void DoWork() => Console.WriteLine("Working"); // Use block body instead
```

### Pattern Matching
- Prefer pattern matching over `as` with null check
- Prefer pattern matching over `is` with cast
- Use `not` pattern where appropriate
- Use switch expressions for simple mappings

```csharp
// Good
if (obj is TodoItem item)
{
    // Use item
}

// Good
var status = priority switch
{
    1 => "Low",
    2 => "Medium",
    3 => "High",
    _ => "Unknown"
};
```

### Null Checking
- Use null-coalescing operator: `var result = value ?? defaultValue;`
- Use null-conditional operator: `var length = item?.Title?.Length;`
- Use conditional delegate call: `handler?.Invoke();`

## Domain Objects

### Property Design
- Use `public get; private set;` for domain entity properties
- Use `public get; set;` only for DTOs, view models, or data containers
- Properties represent state, not behavior

```csharp
public class Patient
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public DateTime DateOfBirth { get; private set; }
}
```

### Constructor Patterns
- Establish valid object state from the beginning
- Validate all inputs in constructors
- Throw descriptive exceptions for invalid data
- Use constructor parameters for required properties

```csharp
public class Appointment
{
    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid DoctorId { get; private set; }
    public DateTime ScheduledAt { get; private set; }

    public Appointment(Guid patientId, Guid doctorId, DateTime scheduledAt)
    {
        if (patientId == Guid.Empty)
            throw new ArgumentException("Patient ID cannot be empty", nameof(patientId));

        if (doctorId == Guid.Empty)
            throw new ArgumentException("Doctor ID cannot be empty", nameof(doctorId));

        if (scheduledAt < DateTime.UtcNow)
            throw new ArgumentException("Appointment cannot be scheduled in the past", nameof(scheduledAt));

        Id = Guid.NewGuid();
        PatientId = patientId;
        DoctorId = doctorId;
        ScheduledAt = scheduledAt;
    }
}
```

### Encapsulation
- Keep business logic inside domain objects
- Expose behavior through methods, not public setters
- Use private methods for internal operations
- Protect object invariants at all times

```csharp
public class TodoItem
{
    public int Id { get; private set; }
    public string Title { get; private set; }
    public bool Done { get; private set; }

    public void Complete()
    {
        if (Done)
            throw new InvalidOperationException("Todo item is already completed");

        Done = true;
    }

    public void UpdateTitle(string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
            throw new ArgumentException("Title cannot be empty", nameof(newTitle));

        Title = newTitle;
    }
}
```

### Collections
- Expose collections as `IReadOnlyCollection<T>` to prevent external modifications
- Use backing fields for internal collection manipulation

```csharp
private readonly List<TodoItem> _items = new();
public IReadOnlyCollection<TodoItem> Items => _items.AsReadOnly();
```

## Vertical Slice Architecture Patterns

### Feature File Organization
Keep related code together in a single file under `Features/<Area>/<VerbNoun>.cs`:

1. Controller
2. Command/Query (request record)
3. Validator
4. Handler
5. DTOs/View Models (if needed)

```csharp
namespace VerticalSliceArchitecture.Application.Features.TodoItems;

// 1. Controller
public class CreateTodoItemController : ApiControllerBase
{
    [HttpPost("/api/todo-items")]
    public async Task<IActionResult> Create(CreateTodoItemCommand command)
    {
        var result = await Mediator.Send(command);
        return result.Match(id => Ok(id), Problem);
    }
}

// 2. Command
public record CreateTodoItemCommand(int ListId, string? Title) : IRequest<ErrorOr<int>>;

// 3. Validator
internal sealed class CreateTodoItemCommandValidator : AbstractValidator<CreateTodoItemCommand>
{
    public CreateTodoItemCommandValidator()
    {
        RuleFor(v => v.Title).MaximumLength(200).NotEmpty();
    }
}

// 4. Handler
internal sealed class CreateTodoItemCommandHandler(ApplicationDbContext context)
    : IRequestHandler<CreateTodoItemCommand, ErrorOr<int>>
{
    private readonly ApplicationDbContext _context = context;

    public async Task<ErrorOr<int>> Handle(CreateTodoItemCommand request, CancellationToken cancellationToken)
    {
        var entity = new TodoItem
        {
            ListId = request.ListId,
            Title = request.Title,
            Done = false,
        };

        entity.DomainEvents.Add(new TodoItemCreatedEvent(entity));
        _context.TodoItems.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
```

### Controller Conventions
- Inherit from `ApiControllerBase`
- Use explicit absolute routes: `[HttpPost("/api/todo-items")]`
- Keep controllers thin (delegate to MediatR)
- Use primary constructor for dependency injection

### Command/Query Patterns
- Use `record` types for commands and queries
- Implement `IRequest<ErrorOr<T>>` for commands
- Implement `IRequest<ErrorOr<TViewModel>>` for queries
- Use nullable reference types appropriately

### Validation
- Create `internal sealed` validator classes
- Validators are auto-registered with `includeInternalTypes: true`
- Inherit from `AbstractValidator<TCommand>`

### Handlers
- Create `internal sealed` handler classes
- Use primary constructor syntax for dependencies
- Store dependencies in private readonly fields
- Return `ErrorOr<T>` for error handling
- Access data via `ApplicationDbContext` directly (no repository pattern)

### Data Access
- Use `AsNoTracking()` for read-only queries
- Use `ApplicationDbContext` directly
- Dispatch domain events via entity collection

## ErrorOr Pattern

Use `ErrorOr<T>` for consistent error handling without exceptions:

```csharp
public async Task<ErrorOr<TodoItemDto>> Handle(GetTodoItemQuery request, CancellationToken cancellationToken)
{
    var item = await _context.TodoItems
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

    if (item is null)
        return Error.NotFound("TodoItem.NotFound", "Todo item not found");

    return new TodoItemDto(item.Id, item.Title, item.Done);
}
```

## MediatR Patterns

### Request/Response
- Commands return `ErrorOr<T>` where T is typically an ID or void
- Queries return `ErrorOr<TViewModel>` where TViewModel is a DTO or record

### Domain Events
- Add events to entity's `DomainEvents` collection
- Events are dispatched after `SaveChangesAsync()`
- Event handlers live in `Features/.../EventHandlers`

```csharp
// In handler
entity.DomainEvents.Add(new TodoItemCreatedEvent(entity));

// Event handler
internal sealed class TodoItemCreatedEventHandler : INotificationHandler<TodoItemCreatedEvent>
{
    public Task Handle(TodoItemCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Handle event
        return Task.CompletedTask;
    }
}
```

## Async/Await
- Use `async`/`await` for I/O-bound operations
- Pass `CancellationToken` to async methods
- Use `Task<T>` for async methods returning values
- Use `Task` for async methods returning void (in non-event handlers)

## Code Quality

### Avoid
- Anemic domain models (objects with only getters/setters)
- Public setters on domain entities
- Business logic in handlers when it belongs in domain objects
- Repository pattern (use DbContext directly)
- `var` keyword (prefer explicit types)

### Prefer
- Rich domain models with behavior
- Guard clauses for precondition checks
- Primary constructors for simple dependency injection
- Record types for DTOs and immutable data
- Extension methods for reusable query logic

## Testing Considerations

- Use `InternalsVisibleTo` to expose internal classes to test projects
- Validators and handlers are `internal sealed` by design
- Keep feature code testable by separating concerns appropriately

## Code Formatting Command

Before committing, always run:
```bash
dotnet format
```

To verify formatting without changes:
```bash
dotnet format --verify-no-changes
```