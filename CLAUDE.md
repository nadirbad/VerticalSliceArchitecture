# CLAUDE.md

The conventions for this repository live in one place, and it is not this file.

@AGENTS.md

Read it before writing code here. It describes the slice shape, the rules that hold everywhere,
and — importantly — which test enforces each rule. When a convention is unclear, the test named
alongside it is the authority.

## Claude-specific notes

- **Verify before declaring done.** `dotnet build` treats analyzer warnings as errors and
  `dotnet test` includes the architecture, contract, and documentation checks. A change is not
  finished until both pass.
- **`dotnet test tests/Application.ArchitectureTests` runs in well under a second.** Use it as a
  fast check while working; it catches shape mistakes long before the full suite.
- **Never resolve a failure by suppressing the rule that found it.** See "Working agreements" in
  AGENTS.md. If a guardrail is genuinely wrong, change the test and the documentation together and
  say why.
- **If you change the HTTP surface on purpose**, regenerate the contract snapshot with
  `UPDATE_SNAPSHOTS=1 dotnet test tests/Application.IntegrationTests` and review the resulting diff
  before committing it. Do not regenerate it to silence a failure you did not intend.
