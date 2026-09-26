---
applyTo: '**'
---

# Testing, Coverage, and Mutation Strategy

Governing thought: Default to fast, deterministic L0 tests with strong coverage, using mutation testing proportionately as an additional quality signal.

> Drift check: Confirm script parameters in `eng/src/agent-scripts/` (or `./go.ps1`) before running; scripts are authoritative for order and options.

## Rules (RFC 2119)

- Test projects **MUST** use xUnit Core Framework v3 and xUnit `Assert`, with the repository's Microsoft.Testing.Platform runner. Support libraries **MUST** remain libraries and use assertion-only or extensibility packages when needed. Why: Keeps execution and shared contracts consistent.
- Test commands **MUST** preserve nonempty execution checks and per-project TRX evidence; use canonical scripts rather than VSTest-only logger or collector arguments. Why: MTP uses different runner and coverage options.
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
- Agents **MUST** follow the [mutation-testing policy](mutation-testing.instructions.md) for proportionate effort, execution, and reporting. Why: Mutation testing is an additional quality signal with no mandatory repository score threshold or ordinary completion gate.
- Agents **SHOULD** improve mutation strength where meaningful assertions are straightforward to add within the requested work. Why: Strong conventional unit tests remain the priority while mutation coverage develops gradually.
- Test code **MUST** honor zero-warnings policy (no suppressions/`#pragma`/`NoWarn`). Why: Test quality equals production quality.
- Legacy improvement tasks **MUST NOT** edit production code without approval and **MUST** keep work inside `tests/`; warnings/failures **MUST** be fixed immediately. Why: Assumes production behavior is correct until tests prove otherwise.
- Package references in tests **MUST** follow Central Package Management (no `Version` attributes). Why: Prevents drift and NU10xx noise.

- Agents **MUST** use [verify-change](../../.agents/skills/verify-change/SKILL.md) when selecting, running, or assessing change-validation checks. Why: Required gates and evidence interpretation need one maintained procedure.

## Scope and Audience

Applies to all test authors across Mississippi and Samples solutions, including mutation work and legacy test improvements.

## Change verification

Use [verify-change](../../.agents/skills/verify-change/SKILL.md) with the
[local check bindings](../agent-guidance/verify-change-bindings.md) for check selection,
execution, evidence reuse, and status assessment. If skill discovery is
unavailable or applicability is unclear, read both linked files directly.
The Rules above remain effective independently of skill activation; a
targeted or prerequisite check does not replace required completion gates.

## Optional Mutation Work

For explicit mutation execution or report assessment, read
[run-mutation-testing](../../.agents/skills/run-mutation-testing/SKILL.md) and
the [local mutation bindings](../agent-guidance/mutation-testing-bindings.md).
If discovery is unavailable, read the linked skill directly. Keep mutation
optional unless the task makes it acceptance criteria, and report its status,
scope, valid results, paths, and significant gaps under the mutation policy.

## References

- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
- Build rules: `.github/instructions/build-rules.instructions.md`
- Spring placement and scheduling: `samples/Spring/TESTING.md`
