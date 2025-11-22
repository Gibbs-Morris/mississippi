---
applyTo: '**'
---

# Test Improvement Workflow (Legacy/Non‑TDD Code)

Governing thought: This guide defines a practical, repeatable loop to raise unit test coverage and mutation score on legacy projects through iterative test additions while maintaining zero-warnings quality standards.

## Rules (RFC 2119)

- Agents **MUST NOT** edit anything outside the `tests/` folder unless explicitly approved.  
  Why: Assumes production code is correct until tests demonstrate a defect.
- Agents **MUST** fix all new warnings/errors immediately before proceeding.  
  Why: Zero-warnings policy applies to test code; maintains quality standards.
- Agents **MUST** aim for 100% coverage on changed code paths with no regressions.  
  Why: New test additions should not decrease existing coverage.
- Agents **MUST** maintain mutation score at or above 80% for Mississippi projects.  
  Why: Ensures test quality through mutation testing validation.
- Agents **MUST NOT** disable warnings or suppress analyzer rules in test files.  
  Why: Test code must meet the same quality standards as production code.
- Agents **SHOULD** use `-NoBuild` flag after initial build for faster iteration.  
  Why: Speeds up test-only iterations while maintaining quality through separate build checks.
- Agents **SHOULD** rerun summarizer scripts to sync coverage gaps and mutation survivors.  
  Why: Automated task generation prevents manual scratchpad management errors.

## Scope and Audience

**Audience:** Developers improving test coverage on legacy code that wasn't written with TDD.

**In scope:** Test improvement workflow, coverage targets, mutation testing, quality gates.

**Out of scope:** New feature development, production code refactoring without approval.

> **Drift check:** Before running any PowerShell script referenced here, open the script in `eng/src/agent-scripts/` (or the specified path) to confirm its current behavior matches this guidance. Treat this document as best-effort context—the scripts remain the source of truth for step ordering and options.

## At-a-Glance Improvement Loop

1) Prepare tools once.

```powershell
pwsh -NoLogo -NoProfile -ExecutionPolicy Bypass -Command "dotnet tool restore"
```

1. Establish baseline (tests + coverage only), then add tests to reach target coverage.

```powershell
pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject <YourTestProject> -SkipMutation
```

1. Build-only check anytime to surface warnings/errors.

```powershell
dotnet build ./tests/<YourTestProject>/<YourTestProject>.csproj -c Release -warnaserror
```

1. Add mutation testing; improve assertions/branches until threshold is met (Mississippi only).

```powershell
pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject <YourTestProject>
```

1. Iterate quickly using `-NoBuild` after the initial build, but keep separate build checks for zero‑warnings.

## Purpose

This workflow provides a systematic approach to improving test coverage and mutation scores on legacy code through iterative, quality-focused test additions.

## Core Principles

- Read all `.github/instructions/*.instructions.md` files before starting to align with repository-wide standards
- Cross-reference testing.instructions.md and build-rules.instructions.md
- Assume production code is correct until tests demonstrate a defect
- Use automated summarizer scripts for coverage gaps and mutation survivors
- The scratchpad is ephemeral and ignored by Git

## Critical: Zero Warnings in Tests

Test code quality matters because:

- Tests are documentation showing how to use code correctly
- Tests demonstrate patterns and best practices
- Test code is production code that runs in CI
- Poor test code creates technical debt

Guidelines:

- Fix test code warnings immediately
- Never disable warnings or suppress analyzer rules in tests
- Never use `#pragma warning disable` in tests
- Maintain analyzer and StyleCop cleanliness everywhere

## Targets (per Build/Testing rules)

- Coverage: default target 95% unless otherwise specified. Absolute minimum remains 80% per Testing rules; aim for 100% on changed code paths with no regressions.
- Mutation score: default target 80% unless otherwise specified (Samples are exempt from mutation testing; see Testing rules).
- Flexibility: If a task sets explicit targets (e.g., 100% or 95%), follow those. Some legacy code may be difficult to unit‑test without refactoring; document constraints and request approval before proposing production changes.

## Script Overview: `eng/src/agent-scripts/test-project-quality.ps1`

Parameters:

- `-TestProject <Name|Path>`: Test project name (e.g., `Core.Tests`) or a direct path to the test `.csproj` (or its directory).
- `-SkipMutation`: Skip Stryker mutation testing for faster loops.
- `-SourceProject <path>`: Manually point to the source `.csproj` when inference from `<ProjectReference>` is ambiguous.
- `-Configuration Release` (default) and `-NoBuild` for faster re‑runs once built.

Notes:

- The script runs `dotnet test`, which builds the test project by default. If you pass `-NoBuild`, run an explicit `dotnet build` to surface any new warnings/errors as part of the zero‑new‑warnings policy.

Outputs (machine‑readable): `RESULT`, `TEST_TOTAL`, `TEST_PASSED`, `TEST_FAILED`, `TEST_SKIPPED`, `COVERAGE`, and when not skipping mutation: `MUTATION_SCORE`, `MUTATION_RESULT`. These outputs are designed for AI agents in both Cursor and GitHub Copilot to parse and act on consistently.

Artifacts:

- Test results and coverage: `.scratchpad/coverage-test-results/<TestProject>/` (includes `test_results.trx` and `coverage.cobertura.xml`).
- Mutation reports: latest under `.scratchpad/mutation-test-results/**/` (JSON/Markdown/HTML).

## Step‑by‑Step Improvement Loop

1) Prepare once

```powershell
pwsh -NoLogo -NoProfile -ExecutionPolicy Bypass -Command "dotnet tool restore"
```

1. Quick baseline (tests + coverage only)

```powershell
pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject <YourTestProject> -SkipMutation
```

- If coverage is below your target (default 95%), identify gaps using `.scratchpad/coverage-test-results/<YourTestProject>/coverage.cobertura.xml` in your IDE or a coverage viewer. Prioritize:
  - Core business logic (L0 tests), edge cases, error paths, branches, and boundary conditions.
  - Recently changed files to prevent regressions.
- Add tests under `tests/<YourTestProject>/` only. Re‑run the same command until coverage meets your target (default `≥ 95%`) and all tests pass.

Build‑only check (surface warnings/errors)

```powershell
dotnet build ./tests/<YourTestProject>/<YourTestProject>.csproj -c Release -warnaserror
```

- Run this at any point to quickly catch new compiler/analyzer warnings or errors. Note: `dotnet test` already builds unless you use `-NoBuild`.

1. Add mutation testing (quality of assertions and branches)

```powershell
pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject <YourTestProject>
```

- If `MUTATION_SCORE < 80%`, open the latest Stryker report in `.scratchpad/mutation-test-results/**/` and focus on surviving mutants. Typical fixes:
  - Strengthen assertions (not just happy path; verify behavior, state, and interactions).
  - Add missing branch and exception-path coverage.
  - Use representative inputs (boundary values, null/empty, min/max).
  - Replace overly broad mocks with fakes or refine setups to exercise real logic.
- Add tests under `tests/` only and re‑run until `MUTATION_SCORE ≥ 80%`.

1. Tight loop for speed

- After the initial build, you can append `-NoBuild` to speed up iterations:

```powershell
pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject <YourTestProject> -SkipMutation -NoBuild
pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject <YourTestProject> -NoBuild
```

- When using `-NoBuild`, run a separate build to enforce the zero‑new‑warnings policy:

```powershell
dotnet build ./tests/<YourTestProject>/<YourTestProject>.csproj -c Release -warnaserror
```

1. Ambiguity resolution

- If the script cannot infer the source project (multiple `<ProjectReference>` entries or none under `/src/`), provide it explicitly:

```powershell
pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject <YourTestProject> -SourceProject ./src/<Project>/<Project>.csproj
```

## When Tests Imply Production Changes

If, after solid tests, behavior appears incorrect or untestable due to design constraints:

- Document the failing scenario and proposed change.
- Request explicit approval before editing any file outside `tests/`.
- Once approved, follow the standard Build Rules: update tests first, then implement, and run the full pipeline.

## Tips

- Prefer L0 tests (pure in‑memory) for speed and determinism; only use light infra (L1) when necessary.
- Use `Theory` with data sets for input spaces; exercise both success and failure paths.
- Keep tests isolated, deterministic, and fast; avoid wall‑clock sleeps, random, and external state.
