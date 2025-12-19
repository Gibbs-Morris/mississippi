---
applyTo: '**'
---

# Benchmark Projects

Governing thought: Benchmarks are opt-in performance checks in dedicated `*.Benchmarks` console projects kept out of PR gates.

> Drift check: Review `Directory.Build.props`/`Directory.Packages.props` before wiring benchmarks; they define BenchmarkDotNet defaults.

## Rules (RFC 2119)

- Benchmark projects **MUST** be named `<Product>.<Feature>.Benchmarks` and **MUST NOT** end with `Tests`; they **MUST** use `Microsoft.NET.Sdk` with `<OutputType>Exe</OutputType>`. Why: Separates them from test projects and enables BenchmarkDotNet.
- Benchmark projects **SHOULD** live under `benchmarks/` and **SHOULD NOT** sit under `tests/`. Why: Keeps correctness tests and performance tooling separate.
- Benchmarks **SHOULD** avoid non-deterministic inputs (random without seed, wall-clock sleeps, network). Why: Makes regressions detectable.
- Benchmarks **SHOULD NOT** be wired into PR gates by default. Why: Avoid flaky perf signals.
- BenchmarkDotNet packages **MUST** be added via Central Package Management (no `Version` attributes). Why: CPM compliance.

## Scope and Audience

Developers adding or running BenchmarkDotNet projects.

## At-a-Glance Quick-Start

- Run a benchmark:  
  `dotnet run -c Release --project benchmarks/<Product>.<Feature>.Benchmarks/<Product>.<Feature>.Benchmarks.csproj`
- Run all by convention: `pwsh ./benchmarks.ps1`
- Pass BenchmarkDotNet args after `--`, e.g., `pwsh ./benchmarks.ps1 -- --filter *Reducers*`

## Core Principles

- Keep benchmarks isolated, deterministic, and out of PR gates.
- Use CPM and repo SDK defaults for predictable builds.

## References

- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
