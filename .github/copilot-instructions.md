# Copilot instructions

The full conventions for this repository are in [`AGENTS.md`](../AGENTS.md) at the repository root.
Read it before proposing changes. What follows is the short version, inlined because Copilot does
not follow file references.

## Project shape

.NET 10 Vertical Slice Architecture template, healthcare appointment scheduling domain.

- `src/Api` — host: DI, middleware, Swagger.
- `src/Application` — slices, domain, infrastructure, shared concerns.
- `src/Application/Scheduling` — the feature slice; new features become sibling folders.

Minimal APIs, MediatR, FluentValidation, ErrorOr, EF Core 10 (in-memory by default, PostgreSQL for
real use).

## The slice shape

One use case per file, wrapped in a `public static` envelope named after the use case. From
`src/Application/Scheduling/BookAppointment.cs`:

```csharp
public static class BookAppointment
{
    public record Command(...) : IRequest<ErrorOr<Result>>;

    public record Result(Guid Id, DateTime StartUtc, DateTime EndUtc);

    internal static class Endpoint
    {
        public static Task<IResult> Handle(Command command, ISender mediator) { }
    }

    internal sealed class Validator : AbstractValidator<Command> { }

    internal sealed class Handler(ApplicationDbContext context)
        : IRequestHandler<Command, ErrorOr<Result>> { }
}
```

Do not flatten these members into top-level types. Route registration goes in the feature's
endpoints file, named after the envelope.

## Rules

- A slice never references another slice. Duplicate the DTO, move the concept to `Domain`, or use
  a domain event.
- `Domain/` depends on no framework — no MediatR, EF Core, ASP.NET, or FluentValidation. Mapping
  lives in `Infrastructure/Persistence/Configurations`.
- Domain objects are rich: public getters, private setters, behaviour through methods, creation
  through factory methods, invariants enforced by throwing.
- Every request returns `ErrorOr<T>`. Business failures are returned, never thrown;
  `MinimalApiProblemHelper.Problem` maps them to 404 / 409 / 400.
- Every request has a sibling `Validator`. Handlers access data through `ApplicationDbContext`
  directly — there is no repository. Prefer `AsNoTracking()` for queries.
- Every persisted entity has an `IEntityTypeConfiguration`.
- A file's name matches the first type declared in it.

## These rules are enforced

`dotnet test` runs architecture, HTTP-contract, and documentation checks alongside the unit and
integration tests; `dotnet build` treats analyzer warnings as errors. Suggestions that break a
convention will fail the build rather than merely disagree with this file.

Do not resolve a failure by suppressing the rule that found it.
