---
applyTo: "tests/**/*"
---

# Testing Strategy

## Scope
Test levels L0-L4, conventions, coverage. Mutation specifics in mutation-testing file.

## Quick-Start
L0 (pure unit): in-memory, fast, deterministic. L1 (light infra): file/DB, isolated. L2 (functional): test deployments. L3 (E2E): prod-like, Playwright. L4 (prod): synthetic checks.

## Test Project Conventions
Project naming: `<Product>.<Feature>.L0Tests` through `.L4Tests`. Legacy naming (`.Tests`, `.UnitTests`, `.IntegrationTests`) SHOULD migrate to L0-L4 over time. Projects ending in `Tests` automatically get xUnit, runner, coverlet, Allure, Moq. InternalsVisibleTo configured for all test projects.

## Frameworks
Unit: xUnit. Mocking: Moq. Coverage: coverlet. UI/API: Playwright (L3). Mutation: Stryker.NET (Mississippi only).

## Coverage Targets
Mississippi: 80% minimum (SonarCloud gate), 95-100% target. Samples: minimal examples. Aim 100% on changed code. No regressions on touched files.

## Test Levels

### L0 - Pure Unit
Scope: business logic, pure functions, orchestration seams. Dependencies: in-memory only, test doubles/mocks. Speed: sub-50ms per test. Requirements: deterministic, no I/O, no timers, no network, constructor-only DI seams.

### L1 - Light Infrastructure
Scope: unit-level with minimal infra (local file system, in-proc DB, mock HTTP). Dependencies: ephemeral and isolated per test. Requirements: no cross-test interference, bound execution time, parallel-safe.

### L2 - Functional
Scope: feature slices against test deployments. Dependencies: test environment URLs, seeded data, mocked third-party systems. Requirements: idempotent, self-cleaning, parallelizable, clear diagnostics.

### L3 - End-to-End
Scope: real user flows, cross-service, UI journeys with Playwright. Dependencies: prod-like environment. Requirements: resilient selectors, capture traces/screenshots on failure, tag scenarios.

### L4 - Production Checks
Scope: synthetic monitoring, gated smoke tests (read-only or harmless). Dependencies: live endpoints. Requirements: strictly non-destructive, tight timeouts, align with SLO/SLA.

## Anti-Patterns
❌ External state in L0. ❌ Slow/flaky tests. ❌ Missing coverage on new code. ❌ Warnings in tests. ❌ Cross-test interference.

## Enforcement
PR gates: L0 (required), L1 (where present). Coverage collected via coverlet. See mutation-testing file for Stryker.
