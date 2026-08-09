using System.Runtime.CompilerServices;

namespace VerticalSliceArchitecture.Application.ArchitectureTests;

/// <summary>
/// Locates repository files from the test assembly.
/// </summary>
/// <remarks>
/// The repository root is resolved from the compile-time path of this source file rather than
/// from the test assembly's output directory. That keeps the documentation and slice-source
/// checks working without copying files into bin/, and lets snapshot-style tests rewrite a file
/// in place at its real location.
/// </remarks>
internal static class RepoPaths
{
    /// <summary>Gets the absolute path of the repository root.</summary>
    internal static string Root { get; } = ResolveRoot();

    /// <summary>Gets the absolute path of the Application project directory.</summary>
    internal static string ApplicationProject { get; } = Path.Combine(Root, "src", "Application");

    // Makes a path readable in assertion messages by showing it relative to the repo root.
    internal static string Relative(string absolutePath) =>
        Path.GetRelativePath(Root, absolutePath).Replace('\\', '/');

    // This file lives at <root>/tests/Application.ArchitectureTests/RepoPaths.cs, so the root is
    // two directories up. CallerFilePath is filled in by the compiler at the call site, which is
    // inside this same file - so the relative hop is always correct.
    private static string ResolveRoot([CallerFilePath] string thisFile = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", ".."));
}