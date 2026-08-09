using System.Text.RegularExpressions;

namespace VerticalSliceArchitecture.Application.ArchitectureTests;

/// <summary>
/// Keeps the instruction files honest.
/// </summary>
/// <remarks>
/// Instruction files are read by coding agents as if they were authoritative, so a stale one is
/// worse than none: the wrong shape arrives with the credibility of a checked-in convention.
/// Nothing fails when prose becomes false, which is exactly why prose rots. These tests give the
/// documentation the same failure mode as the code.
/// </remarks>
public class DocumentationTests
{
    /// <summary>Members a slice envelope is allowed to expose, as referenced from documentation.</summary>
    private const string EnvelopeMembers = "Command|Query|Result|Handler|Validator|Endpoint";

    /// <summary>Instruction files that must describe the codebase as it actually is.</summary>
    private static readonly string[] DocumentationFiles =
    [
        "AGENTS.md",
        "CLAUDE.md",
        "README.md",
        Path.Combine(".github", "copilot-instructions.md"),
    ];

    /// <summary>
    /// Concepts this codebase has removed. Naming any of them means the file is describing a
    /// version of the project that no longer exists.
    /// </summary>
    private static readonly string[] RemovedConcepts =
    [
        "ApiControllerBase",
        "TodoItem",
        "SQL Server",
        "CompletedAt",
        "MapEndpoint",
    ];

    private static readonly Regex CSharpFence = new(
        "```csharp\\s*\\r?\\n(.*?)```",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex EnvelopeReference = new(
        $"\\b([A-Z][A-Za-z0-9]*)\\.({EnvelopeMembers})\\b",
        RegexOptions.Compiled);

    [Fact]
    public void CanonicalInstructionFile_Exists()
    {
        const string Guidance = """
            AGENTS.md is the single source of truth for how this codebase is written, and the
            file every coding agent is pointed at. CLAUDE.md and .github/copilot-instructions.md
            are thin pointers to it so that the same rules cannot be restated - and then drift -
            in three places.
            """;

        var path = Path.Combine(RepoPaths.Root, "AGENTS.md");

        var violations = File.Exists(path)
            ? new List<string>()
            : ["AGENTS.md is missing from the repository root"];

        ArchRule.Assert(violations, "the canonical instruction file exists", Guidance);
    }

    [Fact]
    public void Docs_MustOnlyReferenceTypesThatExist()
    {
        const string Guidance = """
            Every type named in a C# example must resolve against the compiled assembly. If an
            example shows 'BookAppointment.Command', that nested type has to exist.

            When this fails the documentation is describing code that is not there. Fix the
            example rather than the test - and prefer excerpting a real file over inventing a
            snippet, because an excerpt drifts visibly as a diff.
            """;

        var violations = new List<string>();

        foreach (var (file, content) in ReadDocumentationFiles())
        {
            foreach (Match fence in CSharpFence.Matches(content))
            {
                foreach (Match reference in EnvelopeReference.Matches(fence.Groups[1].Value))
                {
                    var envelopeName = reference.Groups[1].Value;
                    var memberName = reference.Groups[2].Value;

                    if (!EnvelopeMemberExists(envelopeName, memberName))
                    {
                        violations.Add($"{file} references '{reference.Value}', which does not exist");
                    }
                }
            }
        }

        ArchRule.Assert(
            violations.Distinct().ToList(),
            "documentation only names types that exist in the code",
            Guidance);
    }

    [Fact]
    public void Docs_MustNotUseFlattenedTypeNames()
    {
        const string Guidance = """
            This codebase nests the parts of a use case inside an envelope:

                BookAppointment.Command      not     BookAppointmentCommand
                BookAppointment.Handler      not     BookAppointmentCommandHandler
                GetAppointments.Query        not     GetAppointmentsQuery

            The flattened spelling is the shape these instruction files used to describe while the
            code used the nested one, which is precisely the drift these tests exist to prevent.

            The forbidden names are derived from the real slices, so adding a slice automatically
            forbids the wrong spelling of its members - nothing to maintain by hand.
            """;

        var flattenedNames = ApplicationArchitecture.Requests
            .Select(request => request.DeclaringType?.Name)
            .OfType<string>()
            .Distinct()
            .SelectMany(envelope => EnvelopeMembers
                .Split('|')
                .Select(member => envelope + member))
            .ToList();

        var violations = new List<string>();

        foreach (var (file, content) in ReadDocumentationFiles())
        {
            violations.AddRange(flattenedNames
                .Where(name => content.Contains(name, StringComparison.Ordinal))
                .Select(name => $"{file} uses the flattened name '{name}'"));
        }

        ArchRule.Assert(violations, "documentation uses the nested envelope naming", Guidance);
    }

    [Fact]
    public void Docs_MustNotMentionRemovedConcepts()
    {
        const string Guidance = """
            These names belong to earlier versions of this template - the Todo domain, MVC
            controllers, SQL Server - or to properties that have since been renamed. An agent
            reading them will reproduce a codebase that no longer exists.

            Describe what is here now. Do not add migration notes or "previously this was..."
            asides to the instruction files: this rule is a plain substring check, and it is
            deliberately absolute so there is no escape hatch to argue about.
            """;

        var violations = new List<string>();

        foreach (var (file, content) in ReadDocumentationFiles())
        {
            violations.AddRange(RemovedConcepts
                .Where(concept => content.Contains(concept, StringComparison.Ordinal))
                .Select(concept => $"{file} mentions '{concept}'"));
        }

        ArchRule.Assert(violations, "documentation does not describe removed concepts", Guidance);
    }

    private static bool EnvelopeMemberExists(string envelopeName, string memberName)
    {
        var envelope = ApplicationArchitecture.Types
            .FirstOrDefault(type => type.Name == envelopeName && !type.IsNested);

        return envelope?.GetNestedType(memberName, BindingFlags.Public | BindingFlags.NonPublic) is not null;
    }

    private static IEnumerable<(string File, string Content)> ReadDocumentationFiles()
    {
        foreach (var relativePath in DocumentationFiles)
        {
            var absolutePath = Path.Combine(RepoPaths.Root, relativePath);

            if (File.Exists(absolutePath))
            {
                yield return (relativePath.Replace('\\', '/'), File.ReadAllText(absolutePath));
            }
        }
    }
}