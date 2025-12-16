---
applyTo: '**'
---

# Benchmark Projects and BenchmarkDotNet

Governing thought: Benchmarks are opt-in performance measurements (not correctness tests) and live in dedicated `*.Benchmarks` console projects that can be run on demand without affecting PR test gates.

## Rules (RFC 2119)

- Benchmark projects **MUST** be named `<Product>.<Feature>.Benchmarks` and **MUST NOT** end with `Tests`.  
  Why: Projects ending in `Tests` are treated as test projects (packages, settings, and CI behaviors differ).
- Benchmark projects **MUST** use `Microsoft.NET.Sdk` and **MUST** set `<OutputType>Exe</OutputType>`.  
  Why: BenchmarkDotNet executes benchmarks from a console entrypoint.
- Benchmark projects **SHOULD** live under the `benchmarks/` folder and **SHOULD NOT** be placed under `tests/`.  
  Why: Keeps correctness tests and performance tooling clearly separated.
- Benchmark projects **SHOULD** avoid non-deterministic inputs (wall-clock time, random without a fixed seed, network calls).  
  Why: Reduces noise and makes regressions detectable.
- Benchmark projects **SHOULD NOT** be wired into PR gates by default.  
  Why: Benchmarks are sensitive to machine variance and can produce flaky performance deltas in CI.
- Authors **MUST** add any BenchmarkDotNet packages via Central Package Management (`Directory.Packages.props`) and **MUST NOT** specify package versions in project files.  
  Why: The repository uses CPM; project-local versions create conflicts and warnings.

## Scope and Audience

**Audience:** Developers adding or running microbenchmarks for Mississippi libraries and samples.

**In scope:** Benchmark project naming, placement, and execution; BenchmarkDotNet package wiring conventions.

**Out of scope:** Performance investigation methodology, profiling tooling, and CI performance regression automation.

## At-a-Glance Quick-Start

Run a benchmark project directly (recommended for local work):

```bash
dotnet run -c Release --project benchmarks/<Product>.<Feature>.Benchmarks/<Product>.<Feature>.Benchmarks.csproj
```

```powershell
dotnet run -c Release --project benchmarks/<Product>.<Feature>.Benchmarks/<Product>.<Feature>.Benchmarks.csproj
```

Run all benchmarks in the repo by convention:

```powershell
pwsh ./benchmarks.ps1
```

Forward BenchmarkDotNet arguments to the benchmark program:

```powershell
pwsh ./benchmarks.ps1 -- --filter *Reducers*
```

> **Drift check:** Before relying on any “automatic wiring” described here, open `Directory.Build.props` and `Directory.Packages.props`; those files remain the source of truth.

## Project Format

Create a dedicated console project named `<Product>.<Feature>.Benchmarks`.

Minimal project file template:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\<Product>.<Feature>\<Product>.<Feature>.csproj" />
  </ItemGroup>

</Project>
```

Notes:

- Package references for `BenchmarkDotNet` are provided automatically for projects ending in `Benchmarks` (see `Directory.Build.props`).
- If benchmarks need access to `internal` members, prefer benchmarking public APIs; if internal access is required, ensure `InternalsVisibleTo` includes `$(AssemblyName).Benchmarks` (see `Directory.Build.props`).

## Recommended Folder Layout

```text
benchmarks/
  EventSourcing.Reducers.Benchmarks/
    EventSourcing.Reducers.Benchmarks.csproj
    Program.cs
    ReducersBenchmarks.cs
```
