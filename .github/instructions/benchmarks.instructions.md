---
applyTo: '**'
---

# Benchmark Projects and BenchmarkDotNet

Governing thought: Benchmarks run in isolated `*.Benchmarks` console projects, opt-in outside PR gates.

## Rules (RFC 2119)

- Benchmark projects **MUST** be named `<Product>.<Feature>.Benchmarks`, use `Microsoft.NET.Sdk`, and set `<OutputType>Exe</OutputType>`; they **MUST NOT** end with `Tests` or live under `tests/`.
- Inputs **SHOULD** be deterministic; benchmarks **SHOULD NOT** run in PR gates by default.
- BenchmarkDotNet packages **MUST** be added via Central Package Management with no version attributes in project files.

## Quick Start

```powershell
dotnet run -c Release --project benchmarks/<Product>.<Feature>.Benchmarks/<Product>.<Feature>.Benchmarks.csproj
pwsh ./benchmarks.ps1 -- --filter *Reducers*
```

## Review Checklist

- [ ] Project naming/location correct; console SDK with OutputType Exe.
- [ ] Deterministic inputs; excluded from PR gates by default.
- [ ] Packages added via CPM without versions.
