using ErrorOr;

using FluentValidation;

using MediatR;

namespace VerticalSliceArchitecture.Application.ArchitectureTests;

/// <summary>
/// Executable definition of the vertical slice shape.
/// </summary>
/// <remarks>
/// This file is the authority on how a slice is written. Prose in AGENTS.md points here on
/// purpose: prose drifts silently when the code changes, a test does not.
/// <para>
/// The canonical shape, as implemented by every slice in <c>src/Application/Scheduling</c>:
/// </para>
/// <code>
/// public static class BookAppointment                 // envelope, named after the use case
/// {
///     public record Command(...) : IRequest&lt;ErrorOr&lt;Result&gt;&gt;;
///     public record Result(...);
///     internal static class Endpoint { public static Task&lt;IResult&gt; Handle(...) }
///     internal sealed class Validator : AbstractValidator&lt;Command&gt; { }
///     internal sealed class Handler(...) : IRequestHandler&lt;Command, ErrorOr&lt;Result&gt;&gt; { }
/// }
/// </code>
/// </remarks>
public class SliceConventions
{
    [Fact]
    public void Requests_MustBeNestedInAPublicStaticEnvelope()
    {
        const string Guidance = """
            Write the whole use case in one file, wrapped in an envelope:

                public static class BookAppointment
                {
                    public record Command(...) : IRequest<ErrorOr<Result>>;
                }

            The envelope is what makes the slice self-describing: 'BookAppointment.Command' reads
            as the use case, and every part of the feature is reachable from one name. Do not
            declare a top-level 'BookAppointmentCommand' type.
            """;

        var violations = new List<string>();

        foreach (var request in ApplicationArchitecture.Requests)
        {
            var envelope = request.DeclaringType;
            var described = ApplicationArchitecture.Describe(request);

            if (envelope is null)
            {
                violations.Add($"{described} is a top-level type");
                continue;
            }

            if (request.Name is not ("Command" or "Query"))
            {
                violations.Add($"{described} must be named 'Command' or 'Query'");
            }

            if (!ApplicationArchitecture.IsPublicStaticClass(envelope))
            {
                violations.Add($"{ApplicationArchitecture.Describe(envelope)} must be a public static class");
            }

            if (ApplicationArchitecture.SliceOf(envelope) is null)
            {
                var envelopeName = ApplicationArchitecture.Describe(envelope);
                violations.Add($"{envelopeName} is not inside a feature slice namespace");
            }
        }

        ArchRule.Assert(
            violations,
            "every command and query is nested inside a public static envelope named after the use case",
            Guidance);
    }

    [Fact]
    public void Requests_MustReturnErrorOr()
    {
        const string Guidance = """
            Business failures are returned, never thrown:

                public record Command(...) : IRequest<ErrorOr<Result>>;

            Handlers return Error.NotFound / Error.Conflict / Error.Validation, and
            MinimalApiProblemHelper.Problem turns them into the right HTTP status. A handler that
            throws for an expected business outcome bypasses that mapping and surfaces as a 500.
            """;

        var violations = ApplicationArchitecture.Requests
            .Where(IsNotErrorOrReturning)
            .Select(request =>
            {
                var response = ApplicationArchitecture.ResponseTypeOf(request);
                return $"{ApplicationArchitecture.Describe(request)} returns {response?.Name ?? "nothing"}";
            })
            .ToList();

        ArchRule.Assert(violations, "every command and query returns ErrorOr<T>", Guidance);
    }

    [Fact]
    public void RequestHandlers_MustBeInternalSealedAndNestedWithTheirRequest()
    {
        const string Guidance = """
            The handler belongs inside the same envelope as its request:

                public static class BookAppointment
                {
                    public record Command(...) : IRequest<ErrorOr<Result>>;

                    internal sealed class Handler(ApplicationDbContext context)
                        : IRequestHandler<Command, ErrorOr<Result>> { }
                }

            'internal' keeps the handler out of other slices' reach - callers go through MediatR,
            never by calling the handler directly. Domain event handlers are exempt: they
            implement INotificationHandler<T> and are legitimately top-level types.
            """;

        var violations = new List<string>();

        foreach (var handler in ApplicationArchitecture.RequestHandlers)
        {
            var described = ApplicationArchitecture.Describe(handler);

            if (handler.Name != "Handler")
            {
                violations.Add($"{described} must be named 'Handler'");
            }

            if (!handler.IsSealed)
            {
                violations.Add($"{described} must be sealed");
            }

            if (!ApplicationArchitecture.IsNestedInternal(handler))
            {
                violations.Add($"{described} must be internal and nested inside its envelope");
            }

            foreach (var request in HandledRequests(handler))
            {
                if (handler.DeclaringType != request.DeclaringType)
                {
                    var requestName = ApplicationArchitecture.Describe(request);
                    violations.Add($"{described} handles {requestName} from a different envelope");
                }
            }
        }

        ArchRule.Assert(
            violations,
            "every request handler is an internal sealed 'Handler' nested beside the request it handles",
            Guidance);
    }

    [Fact]
    public void Requests_MustHaveASiblingValidator()
    {
        const string Guidance = """
            Input validation lives with the request it validates:

                internal sealed class Validator : AbstractValidator<Command>
                {
                    public Validator() => RuleFor(v => v.PatientId).NotEmpty();
                }

            Validators are discovered by assembly scanning (AddValidatorsFromAssembly with
            includeInternalTypes: true) and run in ValidationBehaviour before the handler, so a
            missing validator fails open - the request reaches the handler unchecked. If a request
            genuinely needs no rules, add a Validator with no rules to make that explicit.
            """;

        var violations = new List<string>();

        foreach (var request in ApplicationArchitecture.Requests)
        {
            var envelope = request.DeclaringType;

            if (envelope is null)
            {
                continue; // Already reported by the envelope rule.
            }

            var expectedBase = typeof(AbstractValidator<>).MakeGenericType(request);

            var hasValidator = envelope
                .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
                .Any(nested => nested.Name == "Validator" && nested.BaseType == expectedBase);

            if (!hasValidator)
            {
                var described = ApplicationArchitecture.Describe(request);
                violations.Add($"{described} has no sibling 'Validator : AbstractValidator<{request.Name}>'");
            }
        }

        ArchRule.Assert(
            violations,
            "every command and query has a sibling Validator in the same envelope",
            Guidance);
    }

    [Fact]
    public void Slices_MustNotReferenceTypesFromAnotherSlice()
    {
        const string Guidance = """
            A slice may depend on Domain, Common and Infrastructure - never on another slice.
            Sharing types between slices is what turns "change one feature without touching
            others" back into a layered codebase, and it is the coupling most readily introduced
            in the name of reuse.

            When two slices appear to need the same thing:
              - duplicate the DTO (slices are allowed to disagree about shape), or
              - move the concept into Domain if it is genuinely a domain rule, or
              - communicate through a domain event rather than a direct call.
            """;

        var violations = new List<string>();

        foreach (var type in ApplicationArchitecture.Types)
        {
            var owningSlice = ApplicationArchitecture.SliceOf(type);

            if (owningSlice is null)
            {
                continue;
            }

            var foreign = ApplicationArchitecture.ReferencedTypes(type)
                .SelectMany(ApplicationArchitecture.Unwrap)
                .Distinct()
                .Select(referenced => (Type: referenced, Slice: ApplicationArchitecture.SliceOf(referenced)))
                .Where(candidate => candidate.Slice is not null && candidate.Slice != owningSlice);

            foreach (var (referencedType, slice) in foreign)
            {
                var source = ApplicationArchitecture.Describe(type);
                var target = ApplicationArchitecture.Describe(referencedType);
                violations.Add($"{source} references {target} from slice '{slice}'");
            }
        }

        ArchRule.Assert(
            violations.Distinct().ToList(),
            "slices do not reference each other's types",
            Guidance);
    }

    [Fact]
    public void Slices_MustNotImportAnotherSlicesNamespace()
    {
        const string Guidance = """
            This is the source-level companion to the reflection-based slice isolation rule.
            Reflection cannot see method bodies, so a cross-slice call whose result is never
            assigned to a local would otherwise slip through; a fully qualified name or a using
            directive cannot.
            """;

        var violations = new List<string>();
        var slices = ApplicationArchitecture.Slices;

        foreach (var slice in slices)
        {
            var sliceDirectory = Path.Combine(RepoPaths.ApplicationProject, slice);

            if (!Directory.Exists(sliceDirectory))
            {
                continue;
            }

            foreach (var file in SourceFilesIn(sliceDirectory))
            {
                var text = File.ReadAllText(file);
                var others = slices.Where(candidate => candidate != slice);

                foreach (var other in others)
                {
                    var foreignNamespace = $"{ApplicationArchitecture.RootNamespace}.{other}";

                    if (text.Contains(foreignNamespace, StringComparison.Ordinal))
                    {
                        violations.Add($"{RepoPaths.Relative(file)} refers to slice '{other}'");
                    }
                }
            }
        }

        ArchRule.Assert(
            violations.Distinct().ToList(),
            "no slice source file names another slice's namespace",
            Guidance);
    }

    private static bool IsNotErrorOrReturning(Type request)
    {
        var response = ApplicationArchitecture.ResponseTypeOf(request);

        return response is not { IsGenericType: true }
            || response.GetGenericTypeDefinition() != typeof(ErrorOr<>);
    }

    private static IEnumerable<Type> HandledRequests(Type handler) => handler
        .GetInterfaces()
        .Where(contract => contract.IsGenericType
            && contract.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
        .Select(contract => contract.GetGenericArguments()[0]);

    private static IEnumerable<string> SourceFilesIn(string directory)
    {
        var obj = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}";
        var bin = $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}";

        return Directory
            .EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains(obj, StringComparison.Ordinal)
                && !path.Contains(bin, StringComparison.Ordinal));
    }
}