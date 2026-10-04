---
applyTo: '**'
---

# Mutation Testing Playbook (Mississippi)

Governing thought: Use mutation testing as a proportionate quality signal while prioritizing correct delivery, maintainability, and meaningful unit-test coverage.

> Drift check: Open `eng/src/agent-scripts/test-project-quality.ps1`, `mutation-test-mississippi-solution.ps1`, `summarize-mutation-survivors.ps1`, and `stryker-config.json` before use; their options and results describe tooling behavior, not mandatory repository score targets.

## Rules (RFC 2119)

- Agents **MUST** treat mutation testing as an additional quality signal rather than a hard quality gate or completion criterion unless the user explicitly makes it part of the task's acceptance criteria. Why: A formal repository-wide mutation-testing standard is still being developed.
- Agents **MUST NOT** impose a mandatory mutation-score threshold or a maintain-or-raise requirement based on tooling recommendations, configuration, or reported percentages. Why: The repository currently has no mandatory mutation-score threshold.
- Agents **MUST** prioritize the requested feature or fix, correctness, maintainability, and strong conventional unit-test coverage. Why: These are the primary engineering outcomes.
- Agents **SHOULD** keep mutation scores healthy on new features by adding or strengthening meaningful tests where this is straightforward and proportionate. Why: Mutation testing can improve assertion quality naturally as part of the work.
- Agents **MUST NOT** spend significant time or tokens closing mutation gaps or repeatedly chasing surviving mutants unless explicitly asked. Why: Mutation work should not consume the majority of feature or fix delivery effort.
- Agents **SHOULD** defer costly or historical mutation gaps to dedicated follow-up work after reporting them. Why: Consistent mutation coverage is being built gradually and improved systematically.
- Agents **SHOULD** use `test-project-quality.ps1 -SkipMutation` for routine test and coverage validation. Why: The focused script includes Stryker unless this switch is supplied.
- Before a chosen mutation run, agents **MUST** run `dotnet tool restore` and a clean build. Why: Prevents invalid runs.
- Agents **SHOULD** select a focused run and bound mutation effort before starting. Why: A solution-wide run can be expensive and is not required for ordinary completion.
- Agents **MUST** report mutation execution status, available scores, report paths from script output, and significant gaps identified. Why: Quality signals need traceable evidence even when they do not block delivery.
- Skipped, failed, interrupted, or incomplete mutation runs **MUST NOT** be reported as passing. Why: Optional execution does not justify overstating validation.
- Mutation score claims **MUST** identify the scope supported by valid reports, including any incomplete target coverage. Why: Available evidence from a failed or partial run cannot establish results for unreported targets.
- Agents **MUST** distinguish a tooling threshold failure from build/test failures and from the task's acceptance criteria. Why: An optional Stryker command can return nonzero without creating a repository-wide score gate.
- Production code **MUST NOT** be changed solely to kill mutants unless the mutant is provably unkillable via tests; any such change **MUST** be justified. Why: Protects intended behavior.
- Build warnings/test failures **MUST** be fixed before continuing mutation work. Why: Keeps gates stable.
- Agents **SHOULD** reuse existing reports with the summarizer's `-SkipMutationRun` option before considering another run. Why: Understanding evidence often costs less than rerunning Stryker.

## Scope and Audience

All agents planning, implementing, testing, or reviewing repository changes. Stryker work currently targets Mississippi solution projects; Samples do not require mutation testing.

## At-a-Glance Quick-Start

- Default tests and coverage: `pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject <Name> -SkipMutation`
- Optional focused mutation run: `pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject <Name>`
- Optional solution baseline: `pwsh ./eng/src/agent-scripts/mutation-test-mississippi-solution.ps1`
- Inspect existing evidence: `pwsh ./eng/src/agent-scripts/summarize-mutation-survivors.ps1 -SkipMutationRun -RunPath <run-directory>`
- Routine full pipeline: `pwsh ./go.ps1`; add `-IncludeMutation` only when mutation execution is intended.

## Core Principles

- Mutation testing is being adopted gradually; historical gaps belong in future dedicated improvement work.
- Spend the majority of effort on the requested engineering outcome, correctness, maintainability, and meaningful unit-test coverage.
- A useful assertion improvement is worthwhile; eliminating every survivor is not an ordinary completion condition.
- Kill mutants with tests, not behavior changes.
- Reporting a gap preserves visibility without committing the current task to close it.

## Proportionate Workflow

1. Deliver and validate the requested behavior with conventional tests first.
2. Decide whether a focused mutation run or existing report will resolve a useful uncertainty within the task's scope and budget.
3. If running Stryker, restore tools and build cleanly. Prefer a completed run for usable evidence; if it becomes impractical, stop and report the incomplete status and reason.
4. Inspect significant survivors and add targeted assertions when straightforward. Reassess before rerunning; do not loop until a percentage is reached.
5. Report the command and scope, execution status, available score and report paths, significant gaps, and any deferred follow-up. State explicitly when mutation was not run.

Configured Stryker thresholds can affect report colors and command exit status. They are tooling settings, not the repository's acceptance standard. Report such failures accurately without changing thresholds merely to obtain a green result; continue the required build and conventional test validation separately. A mutation finding that exposes a real correctness defect still warrants normal defect handling.

## References

- Canonical testing guidance: `.github/instructions/testing.instructions.md`
- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
