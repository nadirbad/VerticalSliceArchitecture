# AGENTS.md

Instructions for coding agents and humans working in this repository. This is the canonical file;
`CLAUDE.md` and `.github/copilot-instructions.md` point here so the same rules are never restated
in two places.

## The one rule that explains the rest

**Every convention in this file has an executable counterpart.** If a rule is worth stating, it is
worth failing the build over — so the tests, not this file, are the authority:

| Convention | Enforced by |
|---|---|
| The slice shape | `tests/Application.ArchitectureTests/SliceConventions.cs` |
| What the domain may depend on and expose | `tests/Application.ArchitectureTests/DomainConventions.cs` |
| This file staying true | `tests/Application.ArchitectureTests/DocumentationTests.cs` |
| The public HTTP contract | `tests/Application.IntegrationTests/OpenApiContractTests.cs` |
| Every slice being reachable | `tests/Application.IntegrationTests/RoutingCompletenessTests.cs` |
| File name matching its type | analyzer `SA1649`, a build error via `TreatWarningsAsErrors` |

When something here is unclear, read the test. When a rule feels wrong, change the test and this
file together — never weaken one silently.

Run them with `dotnet test`. Rule failures print the canonical shape and every violation.

## What this project is

A .NET 10 template demonstrating Vertical Slice Architecture with a healthcare appointment
scheduling domain. Code is organised by business capability, not technical layer. A feature's
endpoint, request, validation, and handler live together in one file.

- `src/Api` — host: DI, middleware, Swagger.
- `src/Application` — everything else: slices, domain, infrastructure, shared concerns.
- `src/Application/Scheduling` — the one feature slice; add new slices as sibling folders.
- `src/Application/Domain` — rich entities, domain events, scheduling policies.
- `src/Application/Common` — pipeline behaviours, filters, base classes.
- `src/Application/Infrastructure` — DbContext, EF configurations, migrations.

Stack: ASP.NET Core Minimal APIs, MediatR, FluentValidation, ErrorOr, EF Core 10 (in-memory by
default, PostgreSQL for real use), xUnit + FluentAssertions + NSubstitute.

## The slice shape

Everything for one use case goes in one file, wrapped in a `public static` envelope named after
the use case. Excerpted from `src/Application/Scheduling/BookAppointment.cs`:

```csharp
public static class BookAppointment
{
    public record Command(
        Guid PatientId,
        Guid DoctorId,
        DateTimeOffset Start,
        DateTimeOffset End,
        string? Notes) : IRequest<ErrorOr<Result>>;

    public record Result(Guid Id, DateTime StartUtc, DateTime EndUtc);

    internal static class Endpoint
    {
        public static async Task<IResult> Handle(Command command, ISender mediator)
        {
            var result = await mediator.Send(command);

            return result.Match(
                success => Results.Created($"/api/appointments/{success.Id}", success),
                errors => MinimalApiProblemHelper.Problem(errors));
        }
    }

    internal sealed class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(v => v.PatientId).NotEmpty();
    }

    internal sealed class Handler(ApplicationDbContext context)
        : IRequestHandler<Command, ErrorOr<Result>>
    {
        public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
        {
            // ...
        }
    }
}
```

The envelope is the point: `BookAppointment.Command` reads as the use case, and every part of the
feature is reachable from one name. Do not flatten these members into top-level types — the
architecture tests reject it, and so does this file's own drift check.

Members are `internal` deliberately. Callers reach a slice through MediatR or HTTP, never by
calling its handler directly, and `internal` is what makes that structural rather than aspirational.

Route registration lives in the feature's endpoints file (`Scheduling/SchedulingEndpoints.cs`),
where each route is named after its envelope:

```csharp
group.MapPost("/", BookAppointment.Endpoint.Handle)
    .WithName("BookAppointment")
    .Produces<BookAppointment.Result>(StatusCodes.Status201Created)
    .ProducesValidationProblem()
    .ProducesProblem(StatusCodes.Status409Conflict)
    .AddEndpointFilter<ValidationFilter<BookAppointment.Command>>();
```

Request flow: HTTP → Minimal API endpoint → `ValidationFilter` → MediatR → `ValidationBehaviour`
→ handler → `ErrorOr` result → HTTP response.

## Rules that hold everywhere

**Slices are independent.** A slice may depend on `Domain`, `Common`, and `Infrastructure` — never
on another slice. When two slices seem to need the same type, duplicate the DTO, move the concept
into `Domain` if it is genuinely a domain rule, or communicate through a domain event. Sharing
types between slices is what turns "change one feature without touching others" back into a
layered codebase.

**The domain depends on no framework.** Types under `Domain/` use the BCL, `Common`, and each
other. No MediatR, no EF Core, no ASP.NET, no FluentValidation. Mapping belongs in
`Infrastructure/Persistence/Configurations`, so entities carry no mapping attributes.

**Domain objects are rich, not anemic.** Public getters, private setters. Behaviour through
methods (`appointment.Complete()`, `appointment.Cancel(reason)`), creation through factory methods
(`Appointment.Schedule(...)`). Invariants are validated in constructors and methods, and violations
throw. `internal set` on `Id` is a deliberate seam for EF Core and tests.

**Business failures are returned, not thrown.** Every request returns `ErrorOr<T>`. Handlers return
`Error.NotFound`, `Error.Conflict`, or `Error.Validation`, and `MinimalApiProblemHelper.Problem`
maps them to 404 / 409 / 400. Throwing for an expected outcome surfaces as a 500.

**Every request has a validator.** Validators are discovered by assembly scanning and run before
the handler, so a missing one fails open — the request reaches the handler unchecked. If a request
genuinely needs no rules, add an empty `Validator` to say so on purpose.

**Every persisted entity has an `IEntityTypeConfiguration`.** Without one, EF Core silently falls
back to conventions: no lengths, no indexes, no constraints — which is how an index the code
relies on quietly fails to exist.

## Naming

- Files: `BookAppointment.cs`, `GetAppointments.cs` — named for the use case, and the file name
  must match the first type in it.
- Envelope members: `Command`, `Query`, `Result`, `Handler`, `Validator`, `Endpoint`.
- Route names match the envelope class name.
- Tests: `{ClassUnderTest}Tests.cs`, methods `{Method}_{Scenario}_{ExpectedBehavior}`.

## Domain rules (healthcare scheduling)

- Appointment duration: minimum 10 minutes, maximum 8 hours.
- Minimum booking advance notice: 15 minutes.
- All times are UTC; the domain rejects any `DateTime` that is not.
- A doctor cannot hold overlapping appointments.
- Completed and Cancelled are terminal states, and both operations are idempotent.
- Cancellation requires a reason.

Constants live in `src/Application/Domain/SchedulingPolicies.cs`. Use them rather than repeating
literals.

## Adding a slice

1. Create `src/Application/{Feature}/{UseCase}.cs` with the envelope shape above.
2. Register the route in the feature's endpoints file, naming it after the envelope.
3. Add domain entities or events under `Domain/` if the concept is genuinely domain-level.
4. Add an EF configuration under `Infrastructure/Persistence/Configurations/` for new entities.
5. Add unit tests under `tests/Application.UnitTests/{Feature}/`.
6. Run `dotnet test`. If the HTTP surface changed on purpose, regenerate the contract snapshot
   with `UPDATE_SNAPSHOTS=1 dotnet test tests/Application.IntegrationTests` and review the diff
   before committing it.

## Commands

```bash
dotnet build                                    # analyzers are errors, not warnings
dotnet test                                     # unit + integration + architecture
dotnet test tests/Application.ArchitectureTests  # conventions only - fast
dotnet format                                   # required; CI verifies with --verify-no-changes
dotnet run --project src/Api/Api.csproj         # Swagger at http://localhost:5206

# Accept an intentional HTTP contract change, then review the diff
UPDATE_SNAPSHOTS=1 dotnet test tests/Application.IntegrationTests

# EF migrations
dotnet ef migrations add "Name" --project src/Application --startup-project src/Api \
  --output-dir Infrastructure/Persistence/Migrations
```

## Working agreements

- **Do not weaken a guardrail to get to green.** Adding `severity = none`, `NoWarn`,
  `#pragma warning disable`, `[SuppressMessage]`, or skipping a test to make a build pass is a
  change to the project's standards, not a fix. If a rule is genuinely wrong, say so and change it
  deliberately — in the test and in this file, together.
- **A failing architecture test is a design signal.** It usually means the code is drifting toward
  layers, not that the rule is inconvenient.
- **Prefer excerpting real code over inventing snippets** in documentation. An excerpt drifts
  visibly as a diff; a paraphrase drifts silently. That is how this file got out of step with the
  code before these tests existed.
