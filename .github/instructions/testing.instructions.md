---
applyTo: '**'
---

# Testing Strategy and Quality Gates

Governing thought: This document defines the Mississippi repository's testing strategy using an L0–L4 layered model to keep feedback fast while ensuring end-to-end confidence.

## Rules (RFC 2119)

- Test projects **MUST** follow naming conventions: `<Product>.<Feature>.L0Tests` through `L4Tests`.  
  Why: Enables consistent tooling, analyzers, and InternalsVisibleTo configuration.
- Developers **MUST** default to L0 tests for new unit tests; L1 **SHOULD** be added only when light infrastructure is necessary.  
  Why: Keeps feedback fast and tests deterministic.
- Tests **MUST** be deterministic and isolated; tests **MUST NOT** use wall-clock sleeps or shared mutable state.  
  Why: Prevents flaky tests and ensures reliable CI results.
- Developers **MUST** aim for 100% coverage on changed code; **MUST** maintain ≥80% minimum overall.  
  Why: Ensures code paths are tested; 80% is absolute minimum per SonarCloud gate.
- Developers **SHOULD** target 95–100% coverage where technically feasible.  
  Why: Higher coverage reduces defect risk.
- Tests **MUST NOT** decrease coverage on touched files.  
  Why: Prevents coverage regressions.
- Developers **MUST** run mutation testing for Mississippi projects and maintain or raise the score.  
  Why: Validates test quality through mutation survival analysis.
- Test projects **MUST** apply zero-warnings policy; target framework, analyzers, and StyleCop rules apply equally.  
  Why: Test code quality standards match production code standards.

## Scope and Audience

**Audience:** Developers writing tests for Mississippi framework and sample applications.

**In scope:** Test organization, naming conventions, coverage targets, mutation testing, test levels L0-L4.

**Out of scope:** Specific test implementations, mocking patterns (see csharp.instructions.md).

## Purpose

This document complements Build Rules and Quality Standards, clarifying how tests are organized, named, and executed across CI using a layered testing model.

## At-a-Glance Quick-Start

- Default to L0 for new unit tests; add L1 only when light infra is necessary.
- Name projects and tests per conventions; keep tests deterministic and isolated.
- Aim for 100% coverage on changed code, maintain ≥80% minimum overall; target 95–100% where feasible.
- For Mississippi projects, run mutation testing and keep or raise the score.
- Use the provided scripts to run tests and mutation checks.

## Test Levels at a Glance

| Level  | Description                                                                    | Dependencies                                         | Where It Runs                                                                                 |
| ------ | ------------------------------------------------------------------------------ | ---------------------------------------------------- | --------------------------------------------------------------------------------------------- |
| **L0** | Pure unit tests — in‑memory, blazing‑fast                                      | Assembly-only, no external state                     | Inline with dev and PR builds (Microsoft Learn [1], Microsoft for Developers [2])             |
| **L1** | Slightly richer unit tests — may involve SQL, file system, minimal infra       | Code + lightweight environment                       | Still run early, often in CI (Microsoft for Developers [2], Microsoft Learn [3])              |
| **L2** | Functional tests against test deployments — with key stubs/mocks               | Deployed services, with mocks for heavy dependencies | Typically in daily rolling CI pipelines (Microsoft for Developers [2], Microsoft Learn [3])   |
| **L3** | End-to-end tests against production-like instances — real UI flows, full stack | Production-like deployment, often UI-driven          | Triggered per release or as “test in prod” (Microsoft for Developers [2])                     |
| **L4** | Restricted integration tests in production — synthetic checks/smoke paths      | Fully deployed system in true production             | Final gatekeeper before or after deployment (Microsoft Learn [3])                             |

[1]: https://learn.microsoft.com/en-us/answers/questions/28698/azure-datacenter-tier-certification?utm_source=chatgpt.com
[2]: https://devblogs.microsoft.com/bharry/testing-in-a-cloud-delivery-cadence/?utm_source=chatgpt.com
[3]: https://learn.microsoft.com/en-us/devops/develop/shift-left-make-testing-fast-reliable?utm_source=chatgpt.com

## Test Project Conventions

These conventions match `Directory.Build.props` so analyzers, InternalsVisibleTo, and tooling work consistently:

- Project name suffixes: use one project per level when appropriate
  - `<Product>.<Feature>.L0Tests`
  - `<Product>.<Feature>.L1Tests`
  - `<Product>.<Feature>.L2Tests`
  - `<Product>.<Feature>.L3Tests`
  - `<Product>.<Feature>.L4Tests`
  - Legacy naming: generic `<…>.Tests`, `<…>.UnitTests`, and `<…>.IntegrationTests` are supported for compatibility but should be migrated to L0–L4 naming over time
  - Prefer per-level projects for clarity and analyzer consistency; migrate legacy projects when touched
- Any project ending with `Tests` is treated as a test project and pulls common test packages (xUnit, runner, coverlet, Allure, Moq)
- InternalsVisibleTo is already configured for `.Tests`, `.UnitTests`, `.IntegrationTests`, and `.L0Tests` … `.L4Tests` so tests can exercise internal members safely
- Target framework, analyzers, and zero-warnings policy apply to test projects as well

## Frameworks and Tools

- Unit testing: xUnit (Microsoft.NET.Test.Sdk + xunit.runner.visualstudio)
- Mocking: Moq
- Coverage: coverlet.collector (collected in CI)
- Reporting: Allure.Xunit (optional annotations/labels)
- UI and API journeys: Playwright (used primarily at L3; optionally at L2 for API flows)
- Mutation testing: Stryker.NET (Mississippi solution only)

## Coverage and Mutation Targets

- Coverage
  - Aim for 100% unit test coverage on new and changed code paths
  - Absolute minimum: 80% lines/branches for Mississippi projects (SonarCloud gate); target 95–100% where technically feasible
  - No coverage regressions: touched files should not decrease in coverage
- Mutation testing
  - Run Stryker.NET for Mississippi projects; keep or raise the mutation score
  - Default thresholds align with repository `stryker-config.json`: high 90, low 80, break 80; avoid score regressions on changed areas
  - Treat surviving mutants as defects in test quality and address them; prefer targeted tests over muting/ignoring

## Level Details and Guardrails

### L0 — Pure unit tests

- Scope: business logic, pure functions, orchestration seams; no I/O, no timers sleeping, no network
- Dependencies: in-memory only; replace collaborators with test doubles/mocks
- Speed: sub-50ms typical per test; designed for PR and local tight loops
- Runs: always in PR builds for the Mississippi solution; subject to mutation testing requirements
- Requirements:
  - Deterministic, no wall-clock sleeps; prefer fake clocks and injected time sources
  - No file system, no environment variables, no network; constructor-only DI seams
  - Prefer records/immutability friendly patterns from C# guidelines

### L1 — Unit/component with light infra

- Scope: still “unit-level” but may touch minimal infra: local file system, in-proc databases, mock HTTP handlers
- Dependencies: ephemeral and isolated per test; use temp directories and in-memory or containerized dev DBs spun up per run
- Runs: included in CI for early signal; keep fast and parallel-safe
- Requirements:
  - No cross-test interference; isolate state and ports
  - Bound execution time; avoid flakiness; prefer fakes/spies over real cloud services
  - Keep external processes optional so the suite remains reliable on dev and CI agents

### L2 — Functional against test deployments

- Scope: validate feature slices against deployed test environments; mock or stub heavy/expensive dependencies
- Dependencies: test deployment URL(s), seeded test data, controlled mocks for third-party systems
- Runs: scheduled or on-demand pipelines (not required for every PR)
- Requirements:
  - Idempotent and self-cleaning; avoid global state
  - Parallelizable where possible; shard by feature
  - Clear diagnostics and assertions that align with logging rules

### L3 — End-to-end in prod-like

- Scope: real user flows, cross-service and UI journeys using Playwright; full stack with real storage and messaging
- Dependencies: prod-like environment with realistic configuration
- Runs: per release or as “test in prod” prior to broad exposure
- Requirements:
  - Use Playwright for UI and API request flows; prefer resilient selectors and stable routes over brittle timing
  - Tag with scenarios and components for reporting (Allure labels optional)
  - Capture traces/screenshots/video on failure for triage

### L4 — Restricted checks in production

- Scope: synthetic monitoring and gated smoke tests that run safely in production (read-only or harmless writes)
- Dependencies: live endpoints and observability hooks
- Runs: immediately post-deploy and/or continuously on a cadence
- Requirements:
  - Strictly non-destructive; use dedicated synthetic accounts/resources
  - Tight timeouts and clear rollback signals
  - Align with SLO/SLA dashboards for alerting

## Where Tests Live

- Mississippi solution (core libraries): emphasize L0 (required) and L1 (selectively). Mutation testing applies here per Build Rules.
- Samples solution (apps + examples): include L0/L1 examples only; mutation testing is not required.
- L2/L3/L4 projects may live under `tests/` or `samples/` as dedicated test applications; wire them into scheduled CI rather than PR gates.

## Benchmarks

Benchmarks are performance measurements and are not part of the L0–L4 correctness test levels.

Use dedicated `*.Benchmarks` console projects and follow `.github/instructions/benchmarks.instructions.md`.

## Patterns and Practices

- xUnit
  - Prefer method naming: `MethodName_Should_Outcome_GivenCondition`
  - Use `Theory` + inline/member data for input spaces
  - Use `IClassFixture`/`CollectionDefinition` to manage shared expensive setup without test interdependence
  - Async all the way; no `.Result`/`.Wait()`; time-bound operations with cancellation tokens
- Mocks/Fakes
  - Default to simple fakes or hand-rolled stubs at L0; use Moq where interaction verification matters
  - Verify behavior and state; avoid over-specifying interactions
- File system and time
  - Use temp directories via the test framework; ensure cleanup
  - Inject clocks and random sources; avoid `DateTime.UtcNow`/`new Random()` in code under test
- Playwright (L3, optionally L2)
  - Keep selectors resilient (data-test-id over CSS classes)
  - Use APIRequestContext for API-level flows; avoid UI when API suffices
  - Capture artifacts (trace/screenshots) on failure; close contexts reliably

## CI Expectations and Quality Gates

- Zero warnings policy applies to tests; treat analyzer violations as build blockers
- PR gates: L0 (required) and L1 (where present) for Mississippi; Samples keep minimal, illustrative tests
- Coverage: collected via coverlet; aim 100%, minimum 80% on Mississippi projects (target 95–100%); no regressions on touched code
- Mutation testing: Mississippi solution only via Stryker.NET; keep or raise mutation score when touching logic; fix surviving mutants. Expect Stryker runs to take up to 30 minutes and allow them to finish.

## Tooling and Scripts

> **Drift check:** Before running any PowerShell script referenced here, open the script in `eng/src/agent-scripts/` (or the specified path) to confirm its current behavior matches this guidance. Treat this document as best-effort context—the scripts remain the source of truth for step ordering and options.

Use the PowerShell scripts documented in Build Rules to run tests and mutation checks:

- `eng/src/agent-scripts/unit-test-mississippi-solution.ps1`
- `eng/src/agent-scripts/unit-test-sample-solution.ps1`
- `eng/src/agent-scripts/mutation-test-mississippi-solution.ps1` (plan for up to 30 minutes and wait for the run to finish; do not cancel early)
- `eng/src/agent-scripts/orchestrate-solutions.ps1` (also via `./go.ps1`)

## Author and Reviewer Checklists

Author

- [ ] Added/updated L0 tests for all new or changed logic
- [ ] Considered L1 tests when infra seams are involved
- [ ] Tests are deterministic, isolated, and fast
- [ ] Assertions cover behavior and edge cases; no sleeps or time flakiness
- [ ] For UI/API journeys, added Playwright tests at L3 (or L2 API flows) where valuable
- [ ] Coverage meets goals (target 100%, minimum 80%; aim 95–100%); no regressions on touched code
- [ ] Mutation score maintained or improved; surviving mutants addressed or justified
- [ ] If notable gaps remain (e.g., coverage), rerun `summarize-coverage-gaps.ps1` to sync pending tasks automatically (see Agent Scratchpad). Mutation survivors already generate per-mutant tasks via `summarize-mutation-survivors.ps1`; verify they exist by rerunning the script instead of creating them manually.

Reviewer

- [ ] L0 present for core logic; L1 used judiciously
- [ ] No external calls in L0; minimal, isolated infra in L1
- [ ] Tests follow naming, async, and DI seam patterns
- [ ] Coverage target met (≥80%, aiming for 95–100%) with no regressions on touched files
- [ ] Mutation score non-regression; surviving mutants handled appropriately
- [ ] For remaining gaps, confirm a scratchpad task exists for follow-up where appropriate (coverage and mutation gaps should already be mirrored automatically by their summarizer scripts)
- [ ] Playwright usage is resilient and artifacts are captured on failure

## Non-Goals and Clarifications

- Do not promote heavyweight, slow tests into PR gates; keep PR feedback fast
- Prefer moving complex E2E checks to L3 with Playwright rather than bloating L1/L2
- Samples are examples: they demonstrate testing patterns but aren’t subject to mutation testing

---
This guidance aligns with Build Rules and Quality Standards and with repository-wide analyzers and conventions. Use it with the C#, Logging, Orleans, and Project instructions to keep tests reliable and fast.
