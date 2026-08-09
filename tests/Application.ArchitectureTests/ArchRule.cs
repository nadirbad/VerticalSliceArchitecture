namespace VerticalSliceArchitecture.Application.ArchitectureTests;

/// <summary>
/// Reporting helper for architecture rules.
/// </summary>
internal static class ArchRule
{
    // Fails the test with a message that states the rule, shows the canonical shape, and lists
    // every violation.
    //
    // The guidance is deliberately verbose. These failures are usually read by someone - or
    // something - that has just written code in the wrong shape, so the message has to teach the
    // right shape rather than only reporting that a rule was broken.
    //
    // This does not use FluentAssertions' "because" parameter on purpose: that string is run
    // through string.Format, and the guidance passed in here contains braces from C# snippets
    // that would be misread as format placeholders.
    internal static void Assert(IReadOnlyList<string> violations, string rule, string guidance)
    {
        if (violations.Count == 0)
        {
            return;
        }

        var detail = string.Join(Environment.NewLine, violations.Select(violation => "  - " + violation));
        var count = violations.Count;

        Xunit.Assert.Fail(
            $"""
             Architecture rule broken: {rule}

             {guidance}

             Violations ({count}):
             {detail}
             """);
    }
}