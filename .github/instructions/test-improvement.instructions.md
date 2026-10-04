---
applyTo: '**'
---

# Legacy Test Improvement Loop

Governing thought: Improve meaningful unit-test coverage on legacy code, addressing mutation gaps proportionately and keeping production code untouched unless explicitly approved.

> Drift check: Confirm command flags in `eng/src/agent-scripts/test-project-quality.ps1` before use; script behavior is authoritative.

## Rules (RFC 2119)

- Work **MUST** stay under `tests/` unless explicit approval is given to change production code. Why: Assumes existing behavior is correct until tests prove otherwise.
- New warnings/errors **MUST** be fixed immediately; test code **MUST NOT** add suppressions or `NoWarn`. Why: Zero-warnings applies to tests.
- Changed code paths **MUST** aim for 100% conventional test coverage with no regressions. Why: Protects quality while improving legacy areas.
- Agents **MUST** follow the [mutation-testing policy](mutation-testing.instructions.md), including reporting significant historical gaps and deferring disproportionate remediation unless explicitly asked. Why: There is no mandatory repository mutation-score threshold.
- After the first clean build, agents **SHOULD** use `-NoBuild` for faster loops but **MUST** still run a build with `-warnaserror`. Why: Keeps iteration fast without skipping gates.
- Coverage and mutation gap tasks **SHOULD** be synced from existing reports with summarizer scripts, using `-SkipMutationRun` for mutation reports. Why: Keeps scratchpad deterministic without triggering unnecessary mutation runs.

## Scope and Audience

Agents improving tests on legacy/non-TDD areas.

## At-a-Glance Quick-Start

- Restore tools: `dotnet tool restore`
- Fast loop: `pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject <Name> -SkipMutation`
- Optional mutation (Mississippi): `pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject <Name>`
- Speed up after first build: add `-NoBuild`; still run `dotnet build ... -warnaserror`
- Refresh tasks from reports: rerun `summarize-coverage-gaps.ps1` and `summarize-mutation-survivors.ps1 -SkipMutationRun`

## Core Principles

- Tests-only edits; deterministic, isolated tests.
- Tight loops with quality gates intact.
- Use automation outputs for coverage/mutation backlog rather than manual tracking.

## References

- Canonical testing guidance: `.github/instructions/testing.instructions.md`
- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
