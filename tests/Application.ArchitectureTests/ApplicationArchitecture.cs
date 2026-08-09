using System.Runtime.CompilerServices;

using MediatR;

namespace VerticalSliceArchitecture.Application.ArchitectureTests;

/// <summary>
/// Reflection helpers shared by the architecture tests.
/// </summary>
internal static class ApplicationArchitecture
{
    /// <summary>The root namespace of the assembly whose conventions are under test.</summary>
    internal const string RootNamespace = "VerticalSliceArchitecture.Application";

    /// <summary>
    /// Namespaces directly beneath the root that are shared scaffolding rather than feature slices.
    /// Everything else directly beneath the root is a slice.
    /// </summary>
    private static readonly string[] SharedSegments = ["Common", "Domain", "Infrastructure"];

    /// <summary>Gets the assembly under test, anchored on a type guaranteed to live in it.</summary>
    internal static Assembly TargetAssembly { get; } = typeof(DependencyInjection).Assembly;

    /// <summary>Gets every hand-written type in the assembly, excluding compiler-generated ones.</summary>
    internal static IReadOnlyList<Type> Types { get; } = TargetAssembly
        .GetTypes()
        .Where(type => !IsCompilerGenerated(type))
        .ToList();

    /// <summary>Gets every MediatR request (command or query) declared in the assembly.</summary>
    internal static IReadOnlyList<Type> Requests { get; } = Types
        .Where(type => RequestInterfaceOf(type) is not null)
        .ToList();

    /// <summary>
    /// Gets every MediatR request handler declared in the assembly.
    /// </summary>
    /// <remarks>
    /// Deliberately matches <see cref="IRequestHandler{TRequest,TResponse}"/> only. Domain event
    /// handlers implement <see cref="INotificationHandler{TNotification}"/> and are legitimately
    /// declared as top-level types, so they must not be judged against the slice-envelope rules.
    /// </remarks>
    internal static IReadOnlyList<Type> RequestHandlers { get; } = Types
        .Where(type => type.GetInterfaces().Any(IsRequestHandlerInterface))
        .ToList();

    /// <summary>Gets the names of the feature slices found in the assembly.</summary>
    internal static IReadOnlyList<string> Slices { get; } = Types
        .Select(SliceOf)
        .OfType<string>()
        .Distinct()
        .OrderBy(name => name, StringComparer.Ordinal)
        .ToList();

    // Returns the slice a type belongs to, or null for shared scaffolding and for types sitting
    // directly in the root namespace.
    internal static string? SliceOf(Type type)
    {
        var containingNamespace = type.Namespace;
        var prefix = RootNamespace + ".";

        if (containingNamespace is null || !containingNamespace.StartsWith(prefix, StringComparison.Ordinal))
        {
            return null;
        }

        var segment = containingNamespace[prefix.Length..].Split('.')[0];

        return SharedSegments.Contains(segment, StringComparer.Ordinal) ? null : segment;
    }

    // Returns the closed IRequest<T> interface a type implements, if any.
    internal static Type? RequestInterfaceOf(Type type) => type
        .GetInterfaces()
        .FirstOrDefault(candidate => candidate.IsGenericType
            && candidate.GetGenericTypeDefinition() == typeof(IRequest<>));

    // Returns the response type a request declares, or null.
    internal static Type? ResponseTypeOf(Type request) =>
        RequestInterfaceOf(request)?.GetGenericArguments()[0];

    // Returns true when a type is a public static class - the slice envelope shape. A static class
    // is emitted as both abstract and sealed.
    internal static bool IsPublicStaticClass(Type type) =>
        type.IsClass && type.IsAbstract && type.IsSealed && type.IsPublic;

    // Returns true when a nested type is declared internal.
    internal static bool IsNestedInternal(Type type) => type.IsNestedAssembly;

    // Renders a type for assertion messages, dropping the shared root namespace and using dots for
    // nesting so the output reads the way the code does.
    internal static string Describe(Type type)
    {
        var name = type.FullName ?? type.Name;
        var prefix = RootNamespace + ".";

        if (name.StartsWith(prefix, StringComparison.Ordinal))
        {
            name = name[prefix.Length..];
        }

        return name.Replace('+', '.');
    }

    // Returns the types a given type refers to: base type, interfaces, attributes, member
    // signatures, and method-body local variables.
    //
    // Local variables are included because reflection cannot see method bodies, and a cross-slice
    // dependency very often appears only as a local. The remaining gap - a call chain whose result
    // is never stored in a local - is covered by the source-level using-directive check in
    // SliceConventions.
    internal static IEnumerable<Type> ReferencedTypes(Type type)
    {
        const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        if (type.BaseType is not null)
        {
            yield return type.BaseType;
        }

        foreach (var contract in type.GetInterfaces())
        {
            yield return contract;
        }

        foreach (var attribute in type.GetCustomAttributesData())
        {
            yield return attribute.AttributeType;
        }

        foreach (var field in type.GetFields(Members))
        {
            yield return field.FieldType;
        }

        foreach (var property in type.GetProperties(Members))
        {
            yield return property.PropertyType;
        }

        foreach (var method in type.GetMethods(Members))
        {
            yield return method.ReturnType;

            foreach (var parameter in method.GetParameters())
            {
                yield return parameter.ParameterType;
            }

            foreach (var local in LocalVariableTypes(method))
            {
                yield return local;
            }
        }

        foreach (var constructor in type.GetConstructors(Members))
        {
            foreach (var parameter in constructor.GetParameters())
            {
                yield return parameter.ParameterType;
            }

            foreach (var local in LocalVariableTypes(constructor))
            {
                yield return local;
            }
        }
    }

    // Unwraps arrays, by-ref and generic types so that Task<List<Foo>> also yields Foo. Without
    // this, a dependency hidden inside a generic argument is invisible.
    internal static IEnumerable<Type> Unwrap(Type type)
    {
        var current = type;

        while (current.IsArray || current.IsByRef || current.IsPointer)
        {
            current = current.GetElementType()!;
        }

        yield return current;

        if (!current.IsGenericType)
        {
            yield break;
        }

        foreach (var argument in current.GetGenericArguments())
        {
            foreach (var nested in Unwrap(argument))
            {
                yield return nested;
            }
        }
    }

    private static bool IsRequestHandlerInterface(Type contract) =>
        contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequestHandler<,>);

    private static bool IsCompilerGenerated(Type type) =>
        type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
        || (type.DeclaringType is not null && IsCompilerGenerated(type.DeclaringType));

    private static List<Type> LocalVariableTypes(MethodBase method)
    {
        try
        {
            var body = method.GetMethodBody();

            return body is null
                ? []
                : body.LocalVariables.Select(local => local.LocalType).ToList();
        }
        catch (NotSupportedException)
        {
            // Abstract, extern and some generated methods have no retrievable body.
            return [];
        }
    }
}