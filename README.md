# Vertical Slice Architecture in .NET 9

A learning template demonstrating **Vertical Slice Architecture** with a healthcare appointment scheduling domain.

![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4)
![License](https://img.shields.io/badge/license-MIT-green)

> **New to Vertical Slice Architecture?** Read my blog post: [A Guide to Vertical Slice Architecture in C# .NET](https://nadirbad.dev/posts/vetical-slice-architecture-dotnet/)

## Important notice ⚠️

This template is still under construction 🚧.

Check out my blog post for a detailed explanation of the concepts and code structure: [A Guide to Vertical Slice Architecture in C# .NET](https://nadirbad.dev/posts/vetical-slice-architecture-dotnet/)

## Give it a Star ⭐

If you find this template useful, please give it a star! It helps others discover this project.

## What is Vertical Slice Architecture?

Instead of organizing code by technical layers (Controllers, Services, Repositories), **Vertical Slice Architecture** organizes code by **features**. Each feature contains everything it needs - endpoint, validation, business logic, and data access - all in one place.

```text
Traditional Layered:              Vertical Slice:
├── Controllers/                  ├── Features/
│   └── AppointmentsController    │   ├── BookAppointment.cs      ← Everything in one file
├── Services/                     │   ├── CancelAppointment.cs
│   └── AppointmentService        │   ├── CompleteAppointment.cs
├── Repositories/                 │   └── GetAppointments.cs
│   └── AppointmentRepository     │
└── Models/                       └── Domain/
    └── Appointment                   └── Appointment.cs
```

**Benefits:**

- Change one feature without touching others
- No jumping between layers - everything is co-located
- Easier to understand, test, and maintain
- Features can evolve independently

## Domain Overview

This template models a **medical clinic appointment scheduling system** where patients book appointments with doctors. The domain enforces real-world constraints: doctors cannot be double-booked, appointments must be scheduled in advance, and appointment lifecycle transitions are controlled (scheduled appointments can be completed or cancelled, but these are terminal states).

### Features & Business Rules

| Feature | Endpoint | Key Business Rules |
|---------|----------|-------------------|
| **Book Appointment** | `POST /api/appointments` | No double-booking doctors, 15-min advance notice, 10min–8hr duration |
| **Get Appointments** | `GET /api/appointments` | Filter by patient, doctor, status, date range; paginated (max 100) |
| **Get by ID** | `GET /api/appointments/{id}` | Returns full appointment with patient/doctor details |
| **Complete** | `POST /api/appointments/{id}/complete` | Cannot complete cancelled appointments; idempotent |
| **Cancel** | `POST /api/appointments/{id}/cancel` | Cannot cancel completed appointments; requires reason; idempotent |

## Quick Start

```bash
# Clone and run
git clone https://github.com/nadirbad/VerticalSliceArchitecture.git
cd VerticalSliceArchitecture
dotnet run --project src/Api/Api.csproj

# Open Swagger UI
open https://localhost:7098
```

## Project Structure

```text
src/
├── Api/                          # ASP.NET Core entry point
│   └── Program.cs                # Minimal hosting setup
│
└── Application/                  # All features and domain logic
    ├── Scheduling/               # ← Feature slice
    │   ├── BookAppointment.cs    #   Command + Validator + Handler + Endpoint
    │   ├── CancelAppointment.cs
    │   ├── CompleteAppointment.cs
    │   ├── GetAppointments.cs
    │   └── GetAppointmentById.cs
    │
    ├── Domain/                   # Domain entities and events
    │   ├── Appointment.cs        #   Rich domain model with business logic
    │   ├── Patient.cs
    │   ├── Doctor.cs
    │   └── Events/
    │
    ├── Common/                   # Shared infrastructure
    │   ├── ValidationFilter.cs
    │   └── MinimalApiProblemHelper.cs
    │
    └── Infrastructure/           # Data access
        └── Persistence/
            └── ApplicationDbContext.cs
```

## Feature Anatomy

Each feature file contains everything needed for that operation:

```csharp
// BookAppointment.cs - Complete feature in one file

// 1. Endpoint Handler
public static class BookAppointmentEndpoint
{
    public static async Task<IResult> Handle(BookAppointmentCommand command, ISender mediator)
    {
        var result = await mediator.Send(command);
        return result.Match(
            success => Results.Created($"/api/appointments/{success.Id}", success),
            errors => MinimalApiProblemHelper.Problem(errors));
    }
}

// 2. Command (Request)
public record BookAppointmentCommand(
    Guid PatientId, Guid DoctorId,
    DateTimeOffset Start, DateTimeOffset End,
    string? Notes) : IRequest<ErrorOr<BookAppointmentResult>>;

// 3. Validator
internal sealed class BookAppointmentCommandValidator : AbstractValidator<BookAppointmentCommand>
{
    public BookAppointmentCommandValidator()
    {
        RuleFor(v => v.PatientId).NotEmpty();
        RuleFor(v => v.DoctorId).NotEmpty();
        RuleFor(v => v.Start).Must(BeInFuture).WithMessage("Must book at least 15 minutes in advance");
        // ... more rules
    }
}

// 4. Handler (Business Logic)
internal sealed class BookAppointmentCommandHandler : IRequestHandler<BookAppointmentCommand, ErrorOr<BookAppointmentResult>>
{
    public async Task<ErrorOr<BookAppointmentResult>> Handle(BookAppointmentCommand request, CancellationToken ct)
    {
        // Check for conflicts, create appointment, save to database
    }
}
```

## Technologies

| Technology                   | Purpose                                      |
| ---------------------------- | -------------------------------------------- |
| **.NET 9 Minimal APIs**      | Lightweight HTTP endpoints                   |
| **MediatR**                  | Request/response pattern, pipeline behaviors |
| **FluentValidation**         | Declarative validation rules                 |
| **ErrorOr**                  | Result pattern for error handling            |
| **Entity Framework Core 9**  | Data access with in-memory or SQL Server     |
| **xUnit + FluentAssertions** | Testing framework                            |

## Development Commands

```bash
# Build
dotnet build

# Run (Swagger at https://localhost:7098)
dotnet run --project src/Api/Api.csproj

# Run tests
dotnet test

# Format code
dotnet format
```

## Sample Data

In development mode, the API automatically seeds sample patients and doctors:

### Patients

| ID                                     | Name        | Email                       |
| -------------------------------------- | ----------- | --------------------------- |
| `11111111-1111-1111-1111-111111111111` | John Smith  | `john.smith@example.com`    |
| `22222222-2222-2222-2222-222222222222` | Jane Doe    | `jane.doe@example.com`      |
| `33333333-3333-3333-3333-333333333333` | Bob Johnson | `bob.johnson@example.com`   |

### Doctors

| ID                                     | Name                | Specialty       |
| -------------------------------------- | ------------------- | --------------- |
| `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa` | Dr. Sarah Wilson    | Family Medicine |
| `bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb` | Dr. Michael Chen    | Cardiology      |
| `cccccccc-cccc-cccc-cccc-cccccccccccc` | Dr. Emily Rodriguez | Pediatrics      |

### Example: Book an Appointment

```bash
curl -X POST https://localhost:7098/api/appointments \
  -H "Content-Type: application/json" \
  -d '{
    "patientId": "11111111-1111-1111-1111-111111111111",
    "doctorId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    "start": "2025-01-15T09:00:00Z",
    "end": "2025-01-15T09:30:00Z",
    "notes": "Annual checkup"
  }'
```

## Database

**Default:** In-memory database (no setup required)

**SQL Server:** Update `src/Api/appsettings.json`:

```json
{
  "UseInMemoryDatabase": false,
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=VerticalSliceDb;..."
  }
}
```

### Azure SQL Edge (Docker)

For local development with SQL Server on any OS (including Apple Silicon):

```bash
# Pull the image
docker pull mcr.microsoft.com/azure-sql-edge:latest

# Run the container
docker run --cap-add SYS_PTRACE -e 'ACCEPT_EULA=1' -e 'MSSQL_SA_PASSWORD=yourStrong(!)Password' -p 1433:1433 --name azuresqledge -d mcr.microsoft.com/azure-sql-edge
```

### Database Migrations

```bash
# Add a new migration
dotnet ef migrations add "MigrationName" --project src/Application --startup-project src/Api --output-dir Infrastructure/Persistence/Migrations

# Apply migrations
dotnet ef database update --project src/Application --startup-project src/Api
```

## Testing

```bash
# Run all tests
dotnet test

# Unit tests - Domain logic, validators
dotnet test tests/Application.UnitTests

# Integration tests - API smoke tests
dotnet test tests/Application.IntegrationTests
```

## Learn More

- [My Blog Post on Vertical Slice Architecture](https://nadirbad.dev/posts/vetical-slice-architecture-dotnet/)
- [Jimmy Bogard: Vertical Slice Architecture](https://jimmybogard.com/vertical-slice-architecture/)
- [Derek Comartin: Organizing Code by Feature](https://codeopinion.com/organizing-code-by-feature-using-vertical-slices/)

## License

[MIT License](./LICENSE)
