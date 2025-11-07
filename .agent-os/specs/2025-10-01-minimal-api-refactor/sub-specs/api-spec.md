# API Specification

This is the API specification for the spec detailed in @.agent-os/specs/2025-10-01-minimal-api-refactor/spec.md

> Created: 2025-10-01
> Version: 1.0.0

## Endpoints

### POST /api/healthcare/appointments

**Purpose:** Book a new healthcare appointment between a patient and doctor
**Parameters:**
- Body: `BookAppointmentCommand` (JSON)
  - `patientId` (Guid, required)
  - `doctorId` (Guid, required)
  - `start` (DateTimeOffset, required)
  - `end` (DateTimeOffset, required)
  - `notes` (string, optional)
**Response:**
- 201 Created: `BookAppointmentResult` with Location header
  - `id` (Guid)
  - `startUtc` (DateTime)
  - `endUtc` (DateTime)
**Errors:**
- 400 Bad Request: Validation errors (invalid dates, missing fields)
- 409 Conflict: Scheduling conflict detected

### POST /api/healthcare/appointments/{appointmentId}/reschedule

**Purpose:** Reschedule an existing appointment to a new time slot
**Parameters:**
- Path: `appointmentId` (Guid, required)
- Body: `RescheduleAppointmentCommand` (JSON)
  - `appointmentId` (Guid, required - must match path parameter)
  - `newStart` (DateTimeOffset, required)
  - `newEnd` (DateTimeOffset, required)
  - `reason` (string, optional)
**Response:**
- 200 OK: `RescheduleAppointmentResult`
  - `id` (Guid)
  - `startUtc` (DateTime)
  - `endUtc` (DateTime)
  - `previousStartUtc` (DateTime)
  - `previousEndUtc` (DateTime)
**Errors:**
- 400 Bad Request: Validation errors or path/body mismatch
- 404 Not Found: Appointment not found
- 409 Conflict: New time slot conflict

## Minimal API Implementation

### Endpoint Registration Pattern

```csharp
app.MapGroup("/api/healthcare")
   .WithTags("Healthcare")
   .MapHealthcareEndpoints();

app.MapGroup("/api/healthcare/appointments")
   .WithTags("Appointments")
   .MapAppointmentEndpoints()
   .AddEndpointFilter<ValidationFilter>();
```

### Endpoint Handler Signature

```csharp
// BookAppointment
app.MapPost("/appointments", async (
    BookAppointmentCommand command,
    ISender mediator,
    MinimalApiProblemHelper problemHelper) =>
{
    var result = await mediator.Send(command);
    return result.Match(
        success => Results.Created($"/api/healthcare/appointments/{success.Id}", success),
        errors => problemHelper.Problem(errors));
});

// RescheduleAppointment
app.MapPost("/appointments/{appointmentId}/reschedule", async (
    Guid appointmentId,
    RescheduleAppointmentCommand command,
    ISender mediator,
    MinimalApiProblemHelper problemHelper) =>
{
    if (appointmentId != command.AppointmentId)
        return Results.BadRequest(new { error = "Route appointmentId does not match command AppointmentId" });

    var result = await mediator.Send(command);
    return result.Match(
        success => Results.Ok(success),
        errors => problemHelper.Problem(errors));
});
```

## Migration Notes

- All endpoints maintain identical routes and contracts as current controller implementation
- Problem Details responses preserve existing error format and status codes
- Validation continues through FluentValidation with automatic pipeline execution
- MediatR handlers remain unchanged - only endpoint layer is refactored
- OpenAPI documentation will be generated through WithOpenApi() extension methods
