using Microsoft.EntityFrameworkCore;

using VerticalSliceArchitecture.Application.Infrastructure.Persistence;

namespace VerticalSliceArchitecture.Application.ArchitectureTests;

/// <summary>
/// Executable definition of what the domain layer is allowed to know and expose.
/// </summary>
public class DomainConventions
{
    /// <summary>
    /// Assemblies the domain must not touch. Depending on any of these drags a delivery or
    /// persistence concern into the model and makes the domain untestable in isolation.
    /// </summary>
    private static readonly string[] ForbiddenAssemblyPrefixes =
    [
        "MediatR",
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
        "FluentValidation",
    ];

    [Fact]
    public void Domain_MustNotDependOnFrameworks()
    {
        const string Guidance = """
            Types under Domain/ may use the BCL, Common, and other Domain types - nothing else.
            No MediatR, no EF Core, no ASP.NET, no FluentValidation.

            The domain is where the business rules live, and it has to be provable without a
            database or an HTTP request. EF Core mapping belongs in
            Infrastructure/Persistence/Configurations, so entities stay free of mapping
            attributes; validation of untrusted input belongs in a slice's Validator, while the
            domain enforces its own invariants by throwing from constructors and methods.
            """;

        var violations = new List<string>();

        foreach (var type in DomainTypes())
        {
            var offenders = ApplicationArchitecture.ReferencedTypes(type)
                .SelectMany(ApplicationArchitecture.Unwrap)
                .Distinct()
                .Select(referenced => (referenced, Assembly: referenced.Assembly.GetName().Name ?? string.Empty))
                .Where(candidate => IsForbidden(candidate.Assembly));

            foreach (var (referenced, assembly) in offenders)
            {
                var described = ApplicationArchitecture.Describe(type);
                violations.Add($"{described} depends on {referenced.Name} ({assembly})");
            }
        }

        ArchRule.Assert(
            violations.Distinct().ToList(),
            "the domain depends on no framework",
            Guidance);
    }

    [Fact]
    public void DomainTypes_MustNotExposePublicSetters()
    {
        const string Guidance = """
            State changes go through behaviour, not assignment:

                public DateTime StartUtc { get; private set; }

                public void Cancel(string reason) { ... }   // enforces the rules, raises the event

            A public setter lets a caller move an appointment into a state the aggregate would
            never have allowed, so the invariant stops being an invariant.

            Two things are deliberately still legal: 'internal set' on Id, which EF Core and the
            tests need as a seam, and 'init' accessors, which cannot mutate an object after
            construction. Audit fields inherited from AuditableEntity are also exempt - the
            DbContext writes them - which is why this rule only inspects declared properties.
            """;

        const BindingFlags Declared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        var violations = DomainTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .SelectMany(type => type
                .GetProperties(Declared)
                .Where(IsPubliclyAssignable)
                .Select(property =>
                    $"{ApplicationArchitecture.Describe(type)}.{property.Name} has a public setter"))
            .ToList();

        ArchRule.Assert(violations, "domain types expose no public setters", Guidance);
    }

    [Fact]
    public void EveryPersistedEntity_MustHaveAnEntityTypeConfiguration()
    {
        const string Guidance = """
            Add a configuration under Infrastructure/Persistence/Configurations:

                public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
                {
                    public void Configure(EntityTypeBuilder<Appointment> builder) { ... }
                }

            ApplicationDbContext picks these up with ApplyConfigurationsFromAssembly, so an entity
            without one is silently mapped by EF Core's conventions instead: no explicit lengths,
            no indexes, no constraints. That is how a uniqueness guarantee the code believes it
            has - such as the doctor time-range index BookAppointment relies on to close its
            double-booking race - quietly fails to exist in the database.
            """;

        var configured = ApplicationArchitecture.Types
            .SelectMany(type => type.GetInterfaces())
            .Where(contract => contract.IsGenericType
                && contract.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>))
            .Select(contract => contract.GetGenericArguments()[0])
            .ToHashSet();

        var violations = PersistedEntities()
            .Where(entity => !configured.Contains(entity))
            .Select(entity => $"{ApplicationArchitecture.Describe(entity)} has no IEntityTypeConfiguration")
            .ToList();

        ArchRule.Assert(
            violations,
            "every entity exposed as a DbSet has an explicit IEntityTypeConfiguration",
            Guidance);
    }

    private static bool IsForbidden(string assemblyName) => ForbiddenAssemblyPrefixes
        .Any(prefix => assemblyName.StartsWith(prefix, StringComparison.Ordinal));

    private static IEnumerable<Type> DomainTypes()
    {
        var domainNamespace = ApplicationArchitecture.RootNamespace + ".Domain";

        return ApplicationArchitecture.Types.Where(type => type.Namespace is not null
            && (type.Namespace == domainNamespace
                || type.Namespace.StartsWith(domainNamespace + ".", StringComparison.Ordinal)));
    }

    private static IEnumerable<Type> PersistedEntities() => typeof(ApplicationDbContext)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(property => property.PropertyType.IsGenericType
            && property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
        .Select(property => property.PropertyType.GetGenericArguments()[0]);

    // Returns true for a property a caller could assign to after construction. An 'init' accessor
    // is public but cannot mutate an existing instance, so it does not count.
    private static bool IsPubliclyAssignable(PropertyInfo property)
    {
        var setter = property.SetMethod;

        if (setter is null || !setter.IsPublic)
        {
            return false;
        }

        var isInitOnly = setter.ReturnParameter
            .GetRequiredCustomModifiers()
            .Any(modifier => modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit");

        return !isInitOnly;
    }
}