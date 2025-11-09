---
applyTo: "tests/**/*"
---

# Testing Strategy

## Scope
Test levels L0-L4, conventions, coverage. Mutation specifics in mutation-testing file.

## Quick-Start
L0 (pure unit): in-memory, fast, deterministic. L1 (light infra): file/DB, isolated. L2 (functional): test deployments. L3 (E2E): prod-like, Playwright. L4 (prod): synthetic checks.

## Core Principles
Test project naming: `Feature.L0Tests` to `Feature.L4Tests`. Coverage: aim 100% on changes, ≥80% minimum, target 95-100%. Frameworks: xUnit, Moq, coverlet, Playwright. Zero-warnings apply to tests.

## Coverage Targets
Mississippi: 80% minimum (SonarCloud gate), 95-100% target. Samples: minimal examples. No regressions on touched files.

## Anti-Patterns
❌ External state in L0. ❌ Slow/flaky tests. ❌ Missing coverage on new code. ❌ Warnings in tests.

## Enforcement
PR gates: L0 (required), L1 (where present). Coverage collected via coverlet. See mutation-testing file for Stryker.
