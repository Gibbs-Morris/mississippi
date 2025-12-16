---
applyTo: '**'
---

# Testing Strategy and Quality Gates

Governing thought: Use the L0–L4 model with fast, deterministic L0 tests by default and strict coverage/mutation targets.

## Rules (RFC 2119)

- Test projects **MUST** follow `<Product>.<Feature>.L0Tests` … `L4Tests` naming; any project ending with `Tests` is treated as a test project with shared packages/settings.
- New tests **MUST** default to L0; L1 **SHOULD** be added only when light infrastructure is required. Tests **MUST** be deterministic/isolated (no wall-clock sleeps or shared mutable state).
- Coverage: changed code **MUST** target 100%, maintain ≥80% overall (aim 95–100%), and **MUST NOT** regress touched files.
- Mississippi projects **MUST** run mutation testing and maintain or raise the score; samples are exempt. Test projects **MUST** honor the zero-warnings policy and repo analyzers.
- Tests **MUST** use PascalCase verb-phrase names; follow repo guidance for async patterns, DI seams, and mocking (xUnit + Moq + coverlet; Allure optional per Allure instructions).

## Quick Start

- Default to L0 (pure in-memory); use L1 for minimal infra and higher levels for deployments only when needed.
- Run scripts:
  - `pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1`
  - `pwsh ./eng/src/agent-scripts/unit-test-sample-solution.ps1`
  - `pwsh ./eng/src/agent-scripts/mutation-test-mississippi-solution.ps1` (allow ~30 minutes)
  - `pwsh ./go.ps1` for the full pipeline

## Review Checklist

- [ ] Project naming/scope correct; L0 default honored; tests deterministic.
- [ ] Coverage meets goals with no regression on touched code; mutation targets met for Mississippi.
- [ ] Zero-warnings policy observed in test projects.
- [ ] Test names and patterns align with repo conventions (async, DI seams, mocking, Allure when used).
