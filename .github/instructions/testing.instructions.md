---
applyTo: '**'
---

# Testing, Coverage, and Mutation Strategy

Governing thought: Default to fast, deterministic L0 tests with high coverage and mutation strength; Mississippi projects carry full gates, Samples stay minimal.

> Drift check: Confirm script parameters in `eng/src/agent-scripts/` (or `./go.ps1`) before running; scripts are authoritative for order and options.

## Rules (RFC 2119)

- Test projects **MUST** follow level naming (`<Product>.<Feature>.L0Tests`…`L4Tests`); legacy `*.Tests` **MUST** migrate when touched. Why: Keeps analyzers and InternalsVisibleTo aligned.
- New tests **MUST** default to L0; L1 **SHOULD** be used only when light infra is required. Why: Keeps feedback fast and deterministic.
- When L0 cannot cover a behavior, authors **SHOULD** attempt L1 before moving to L2. Why: Preserves fast feedback and limits infra reliance.
- L2 tests **SHOULD** be used only when real infrastructure is required (HTTP APIs, SignalR, Cosmos/Blob storage, etc.). Why: Keeps lower levels pure and deterministic.
- Each implementation solution **SHOULD** include separate L0Tests, L1Tests, and L2Tests projects. Why: Keeps scopes clear and enables targeted pipelines.
- Each L2 test project **SHOULD** have a companion Aspire AppHost project that provisions required dependencies and emulators. Why: Makes integration tests repeatable and self-contained.
- Browser-driven application journeys **MUST** live in an `L3Tests` project, separate from L2 API and infrastructure tests. Why: Contributors can locate end-to-end behavior by test level without inspecting implementation dependencies.
- Smoke selection **MUST** use a suite label such as `[Trait("Category", "Smoke")]` within the appropriate test level. Why: Smoke describes a small critical-path subset, not a separate test level.
- Shared Aspire test setup **SHOULD** live in a `TestHarness` project without browser dependencies. Why: L2 tests can run without Playwright, and L3 tests can reuse the same deployment setup.
- Tests **MUST** be deterministic/isolated (no sleeps, no shared mutable state, no real network in L0); time **MUST** use `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing` when production code injects `TimeProvider`; random seeds **SHOULD** be fixed or injected. Why: Prevents flakiness and enables reproducible assertions.
- Coverage targets: changed code **MUST** aim for 100% with **MUST NOT** regress coverage on touched files; solution-wide **MUST** stay >=80% and **SHOULD** target 95-100% where feasible. Why: Protects behavior and gates.
- Mississippi projects **MUST** run mutation testing and maintain or raise the score; mutation runs **MUST** be allowed to finish (~30 minutes) and mutation scripts **MUST** be preceded by `dotnet tool restore` and a clean build. Why: Mutation score enforces assertion quality.
- Mutation work **MUST NOT** change production code solely to kill mutants unless the mutant is provably unkillable via tests; any such change **MUST** be justified. Why: Preserves intended behavior.
- Mutation reports **MUST** have paths recorded (use script outputs); summarizer **SHOULD** be used (`-SkipMutationRun` for iteration) and high-impact survivors **SHOULD** be prioritized. Why: Keeps traceability and focus.
- Test code **MUST** honor zero-warnings policy (no suppressions/`#pragma`/`NoWarn`). Why: Test quality equals production quality.
- Legacy improvement tasks **MUST NOT** edit production code without approval and **MUST** keep work inside `tests/`; warnings/failures **MUST** be fixed immediately. Why: Assumes production behavior is correct until tests prove otherwise.
- Package references in tests **MUST** follow Central Package Management (no `Version` attributes). Why: Prevents drift and NU10xx noise.

## Scope and Audience

Applies to all test authors across Mississippi and Samples solutions, including mutation work and legacy test improvements.

## At-a-Glance Quick-Start

- Restore tools once: `dotnet tool restore`
- Fast loop (tests + coverage only): `pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject <Name> -SkipMutation`
- Add mutation (Mississippi): `pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject <Name>`
- Build-only during iteration: `dotnet build ./tests/<Name>/<Name>.csproj -c Release -warnaserror`
- Full pipeline (Mississippi): `pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1` then `mutation-test-mississippi-solution.ps1`
- Integration tests (Samples L2+): `pwsh ./eng/src/agent-scripts/integration-test-sample-solution.ps1`
- Summaries/tasks: rerun `summarize-coverage-gaps.ps1` and `summarize-mutation-survivors.ps1` (use `-SkipMutationRun` when reusing reports)

## Test Level Filtering

Unit test scripts default to running L0 and L1 tests only. Higher levels have separate workflows so their cost and scope remain visible.

- **Default behavior**: `unit-test-mississippi-solution.ps1` and `unit-test-sample-solution.ps1` run L0Tests and L1Tests.
- **Override**: Pass `-TestLevels @('L0Tests','L1Tests','L2Tests')` to include additional levels.
- **Integration tests**: Use `integration-test-sample-solution.ps1` for L2, or pass custom levels explicitly.
- **Spring L3 smoke**: `pwsh ./test-spring.ps1` runs the browser smoke subset in PR and merge-queue checks.
- **Spring L3 full**: `pwsh ./test-spring.ps1 -TestLevel L3 -Suite Full` runs all browser journeys locally; the L3 Tests workflow offers the same suite on demand.

Filter uses `FullyQualifiedName` matching on project naming convention (e.g., `*.L0Tests`, `*.L2Tests`).

## Core Principles

- Prefer L0 (pure, in-memory) for speed; step to L1 then L2 when needed, with L2 backed by real infra via Aspire.
- Determinism first: use `FakeTimeProvider` for time, fix random seeds, isolate file system and ports, avoid sleeps.
- High coverage and mutation strength on changed code; no regressions.
- Mississippi vs Samples: Samples showcase patterns (minimal tests, no mutation gate); Mississippi enforces full gates.
- Use summarizer outputs and scratchpad tasks instead of manual tracking.

## Test Levels Snapshot

| Level | Scope | Dependencies | Typical Run |
| ----- | ----- | ------------ | ----------- |
| L0 | Pure unit, no IO | In-memory only | Always (PR/local) |
| L1 | Light infra | Temp FS, in-proc DB/mocks | Often (PR/local) |
| L2 | Feature API/infrastructure contracts vs test deployment | Aspire AppHost + emulators/services | Separate L2 workflow/local |
| L3 | End-to-end user journeys through the real client or composed public API | Full stack; Playwright for browser journeys | Small smoke subset on PR/merge queue; full suite on demand |
| L4 | Synthetic prod checks | Live endpoints (read-only) | Post-deploy/monitoring |

## Workflows

### Baseline and Coverage

1. Run `test-project-quality.ps1 -SkipMutation` for the target test project.
2. Add tests to hit behavior, edges, and branches; keep determinism.
3. If coverage < target, inspect Cobertura output under `.scratchpad/coverage-test-results/<Project>/`.

### Mutation Loop (Mississippi)

1. Build clean, then run `mutation-test-mississippi-solution.ps1` (or `test-project-quality.ps1` without `-SkipMutation`).
2. Use `summarize-mutation-survivors.ps1 -SkipMutationRun` to rank survivors and sync tasks.
3. Add targeted tests; rerun mutation or summarizer until score maintained or justified.

### Legacy Test Improvements

1. Work only under `tests/` unless explicitly approved to change production code.
2. Use `-NoBuild` on `test-project-quality.ps1` after the first build for speed; still run a build with `-warnaserror`.
3. Fix warnings immediately; keep coverage/mutation targets and update scratchpad tasks for remaining gaps.

## References

- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
- Build rules: `.github/instructions/build-rules.instructions.md`
- Spring placement and scheduling: `samples/Spring/TESTING.md`
