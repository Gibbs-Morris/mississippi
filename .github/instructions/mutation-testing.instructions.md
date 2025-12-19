---
applyTo: '**'
---

# Mutation Testing Playbook (Mississippi)

Governing thought: Run Stryker after a clean build, let it finish, and close survivors through targeted tests without altering production behavior.

> Drift check: Open `eng/src/agent-scripts/mutation-test-mississippi-solution.ps1` and `summarize-mutation-survivors.ps1` before use; script outputs remain authoritative.

## Rules (RFC 2119)

- Agents **MUST** run `dotnet tool restore` and a clean build before mutation tests. Why: Prevents invalid runs.
- Mutation scripts **MUST** be allowed to finish (plan for ~30 minutes); cancelling early **MUST NOT** happen. Why: Scores are invalid otherwise.
- Mutation reports/paths **MUST** be recorded from script output. Why: Provides traceability.
- Production code **MUST NOT** be changed solely to kill mutants unless the mutant is provably unkillable via tests; any such change **MUST** be justified. Why: Protects intended behavior.
- Build warnings/test failures **MUST** be fixed before continuing mutation work. Why: Keeps gates stable.
- Survivors **SHOULD** be tackled using the summarizer (`-SkipMutationRun` for iteration) and **SHOULD** be prioritized by score/impact. Why: Maximizes value per run.

## Scope and Audience

Mutation testing for Mississippi solution projects (Samples are exempt).

## At-a-Glance Quick-Start

- Baseline run: `pwsh ./eng/src/agent-scripts/mutation-test-mississippi-solution.ps1`
- Iterate quickly: `pwsh ./eng/src/agent-scripts/summarize-mutation-survivors.ps1 -SkipMutationRun`
- Focused top-N: add `-Top <N> -GenerateTasks -EmitTestSkeletons`

## Core Principles

- Let Stryker finish; reruns use summarizer when possible.
- Kill mutants with tests, not behavior changes.
- Keep reports and scratchpad tasks in sync.

## References

- Canonical testing guidance: `.github/instructions/testing.instructions.md`
- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
