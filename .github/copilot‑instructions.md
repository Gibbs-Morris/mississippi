---
applyTo: '**'
---

# Copilot Instructions

Governing thought: Copilot responses must follow repository guardrails—shared policies, SOLID verification, canonical build/test scripts, and CPM/versionless package management.

> Drift check: Build/test commands live in `./go.ps1` and `eng/src/agent-scripts/`; `Directory.Build.props`/`Directory.Packages.props` define MSBuild/CPM settings. Open them before advising.

## Rules (RFC 2119)

- Copilot **MUST** follow all repository instruction files, especially shared guardrails, C#, naming, logging, and testing guidance. Why: Keeps suggestions compliant.
- Build/tidy guidance **MUST** use canonical scripts: `pwsh ./go.ps1` for full pipeline; `pwsh ./clean-up.ps1` to format/tidy; extra formatters **MUST NOT** be assumed. Why: Ensures consistent gates.
- For local iteration speed, Copilot **SHOULD** prefer `pwsh ./clean-up-targeted.ps1` with `-Files` or `-FileListPath` to clean only changed files, then **MUST** run full `pwsh ./clean-up.ps1` before completion/handoff. Why: Preserves canonical gates while reducing local feedback time.
- When build warnings include StyleCop/formatting issues (SA1137 indentation, SA1517 blank lines, SA1000 spacing, etc.), agents **MUST** run `pwsh ./clean-up.ps1` first rather than manually fixing formatting. Why: ReSharper CleanupCode applies `Directory.DotSettings` rules (expression bodies, brace placement, blank lines, wrapping, member ordering) consistently and fixes most formatting warnings automatically—manual fixes often introduce new violations or miss related issues.
- Package changes **MUST** use `dotnet add/remove package`; `Directory.Packages.props` **MUST** hold versions and project `PackageReference` items **MUST NOT** specify `Version`. Why: CPM compliance.
- After touching `.Abstractions`, Copilot **MUST** follow abstractions-project rules (create/use abstractions when triggers apply). Why: Maintains contract/implementation split.
- All generated/refactored code **MUST** match Microsoft C# conventions (file-scoped namespaces, expression bodies when beneficial, nullable guidance) and **MUST** verify SOLID after each change, fixing violations immediately. Why: Prevents design debt.
- Before answering usage/run questions, Copilot **MUST** consult `README.md` and treat it as authoritative for public APIs/env vars/examples. Why: Avoids drift.
- Copilot **SHOULD** prioritize public APIs from README when suggesting symbols and **SHOULD** respond concisely with file paths/lines when referencing code. Why: Improves traceability.
- When work spans many small fixes, Copilot **SHOULD** stage via `.scratchpad/tasks` per scratchpad rules and **MUST NOT** reference `.scratchpad/` from source/tests. Why: Enables safe coordination.
- Agents **MUST NOT** commit directly to `main`; changes **MUST** flow through a pull request. Why: Preserves review/audit quality.
- Branch naming **MUST** follow `GitVersion.yml` patterns; agents **SHOULD** prefer `topic/<name>` for small single-user changes and `feature/<name>` for larger work. Why: Keeps versioning and branch intent consistent.
- On `topic/*`, `feature/*`, and `hotfix/*`, agents **SHOULD** commit in small logical increments (about 5-10 changed files as a guide, not a hard cap); large mechanical changes (for example, global renames) **MAY** exceed this when they are one coherent change. Why: Improves decision traceability during development.

## Scope and Audience

These rules apply to Copilot chat/search responses for this repository.

## At-a-Glance Quick-Start

- Use shared guardrails and C#/naming/logging/testing instructions as the baseline.
- Build/test with `pwsh ./go.ps1`; tidy with `pwsh ./clean-up.ps1`.
- **When you see StyleCop/formatting warnings (SA1xxx), run cleanup first**—don't manually fix indentation/spacing.
- Manage packages with `dotnet add/remove package`; never add `Version` attributes.
- Verify SOLID after each C# change; fix violations before proceeding.
- Use README as the source of truth for usage guidance.
- Never commit directly to `main`; use PR flow and GitVersion branch naming.
- Prefer small logical commits on `topic/*`, `feature/*`, and `hotfix/*` (guide: ~5-10 files; flexible for coherent large renames).

## Core Principles

- Canonical scripts/configs trump inferred behavior.
- SOLID and repository guardrails keep generated code review-ready.
- CPM + DI/logging patterns prevent drift across suggestions.

## References

- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
- C#/naming/logging/testing: see respective instruction files under `.github/instructions/`

## Cleanup Script Details

The `./clean-up.ps1` script runs JetBrains ReSharper CleanupCode on both solutions using settings from `Directory.DotSettings`. It automatically fixes:

- **Formatting**: Indentation, spacing, blank lines (SA1137, SA1517, SA1000, etc.)
- **Braces**: Required braces for `if`/`for`/`foreach`/`while` statements
- **Expression bodies**: Converts to expression-bodied members where configured
- **Member ordering**: Reorders members per xUnit test class patterns and standard layouts
- **Line wrapping**: Chops long argument lists, method chains, and parameters
- **Trailing commas**: Adds trailing commas in multiline lists

Run `pwsh ./clean-up.ps1` after making code changes and before committing to ensure formatting compliance. The script processes both `mississippi.slnx` and `samples.slnx`.

For faster local loops, use targeted cleanup first:

- Explicit files: `pwsh ./clean-up-targeted.ps1 -Files src/Foo/Bar.cs,tests/FooTests.cs`
- File list: `pwsh ./clean-up-targeted.ps1 -FileListPath .scratchpad/cleanup-files.txt`
- Changed-vs-main mode: `pwsh ./clean-up-targeted.ps1`

Measured sample (3 runs, 20 changed files, `jb cleanupcode --no-build`):

- Targeted average: `59.448s`
- Full cleanup average (mississippi + samples): `607.558s`
- Approximate speed-up: `10.22x` (~`90.2%` faster)

Use targeted cleanup to iterate quickly, then run full cleanup before final handoff.
