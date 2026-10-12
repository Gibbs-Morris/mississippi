---
applyTo: '**'
---

# Benchmark Projects

Governing thought: Benchmarks are opt-in performance checks in dedicated `*.Benchmarks` console projects kept out of PR gates.

> Drift check: Review `Directory.Build.props`/`Directory.Packages.props` before wiring benchmarks; they define BenchmarkDotNet defaults.

## Rules (RFC 2119)

- Covered contributors **MUST** read and apply [the complete global policy](../agent-guidance/global-policy.md) under [root instruction loading](../../AGENTS.md#instruction-loading), retaining the audience and task conditions below. Why: These obligations remain mandatory independently of skill selection.

## Scope and Audience

Developers adding or running BenchmarkDotNet projects.
