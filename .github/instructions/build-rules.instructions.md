---
applyTo: '**'
---

# Build Rules and Quality Standards

This project maintains **zero tolerance for warnings** and requires comprehensive test coverage. All code changes must pass the full quality pipeline before being considered complete.

## At-a-Glance Quick-Start

- Build → Clean → Fix until there are zero warnings.

```powershell
pwsh ./eng/src/agent-scripts/build-mississippi-solution.ps1
pwsh ./eng/src/agent-scripts/clean-up-mississippi-solution.ps1
```

- Add/update tests: comprehensive for Mississippi; minimal examples for Samples.
- Run tests (Mississippi solution).

```powershell
pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1
```

- Run mutation tests (Mississippi ONLY).

```powershell
pwsh ./eng/src/agent-scripts/mutation-test-mississippi-solution.ps1
```

- Final validation for both solutions.

```powershell
pwsh ./go.ps1
```

> **Drift check:** Before running any PowerShell script referenced here, open the script in `eng/src/agent-scripts/` (or the specified path) to confirm its current behavior matches this guidance. Treat this document as best-effort context—the scripts remain the source of truth for step ordering and options.

## 🚨 CRITICAL RULE: ZERO WARNINGS POLICY 🚨

**⚠️ ATTENTION: This rule is NON-NEGOTIABLE and applies to ALL code changes ⚠️**

### The Rule

- **NEVER disable warnings** - Fix the underlying code issues instead
- **NEVER suppress analyzer rules** - Improve the code to satisfy the analyzer
- **NEVER use `#pragma warning disable`** without explicit approval and exhaustive justification
- **NEVER add `[SuppressMessage]` attributes** to hide violations
- **ALWAYS fix compiler warnings** - They indicate real problems in your code
- **ALWAYS fix analyzer warnings** - They indicate code quality issues
- **ALWAYS fix StyleCop violations** - They indicate style inconsistencies

### Why This Matters

- **Warnings are treated as errors** in CI builds with `--warnaserror`
- **Builds will fail** if any warnings are present
- **PRs will be blocked** until all warnings are resolved
- **Code quality degrades** when warnings are suppressed instead of fixed
- **Technical debt accumulates** when issues are hidden rather than addressed

### What To Do Instead

1. **Read the warning message** - Understand what the analyzer is telling you
2. **Fix the underlying issue** - Improve your code to satisfy the rule
3. **Refactor if necessary** - Sometimes warnings indicate design problems
4. **Ask for help** - If you're unsure how to fix a warning, ask the team
5. **Document exceptions** - Only suppress warnings with explicit approval and justification

## Quality Standards

### Zero Warnings Policy

- **All compiler warnings must be resolved** - warnings are treated as errors in CI
- **No ReSharper code inspections warnings** - cleanup scripts must pass cleanly
- **No StyleCop violations** - code style must be consistent across the project
- Use `--warnaserror` flag in builds to enforce this standard

### Required Quality Gates

1. **Successful Build** - Code must compile without errors or warnings
2. **Unit Tests** - All tests must pass with 100% success rate
3. **Mutation Testing** - Must maintain high mutation score (Mississippi solution only)
4. **Code Cleanup** - ReSharper cleanup must pass without issues

## Repository Structure

This repository contains **two distinct solutions**:

1. **Mississippi Solution** (`mississippi.slnx`) - The core library with full test coverage requirements
2. **Samples Solution** (`samples.slnx`) - Includes all Mississippi projects PLUS sample applications

### Testing Strategy by Solution

- **Mississippi Solution**: Requires comprehensive unit tests, integration tests, and mutation testing
- **Samples Solution**: Only needs minimal unit tests as examples - **NO mutation testing required**

## Scripts Overview

The `eng/src/agent-scripts/` directory contains PowerShell automation for the build-test-quality pipeline:

### Core Build Scripts

- **`build-mississippi-solution.ps1`** - Compiles the main Mississippi solution in Release mode
- **`build-sample-solution.ps1`** - Builds sample applications demonstrating library usage
- **`final-build-solutions.ps1`** - Strict build with `--warnaserror` for both solutions

### Testing Scripts

- **`unit-test-mississippi-solution.ps1`** - Executes all unit and integration tests for Mississippi
- **`unit-test-sample-solution.ps1`** - Runs minimal tests for sample applications (examples only)
- **`mutation-test-mississippi-solution.ps1`** - Performs mutation testing with Stryker.NET (Mississippi ONLY)
- **`test-project-quality.ps1`** - Quickly evaluate a single test project: runs `dotnet test` with coverage and optionally Stryker mutation testing. Prints a concise, machine-readable summary (RESULT, TEST_*, COVERAGE, MUTATION_SCORE) to aid AI tools.

### Quality Scripts

- **`clean-up-mississippi-solution.ps1`** - Applies ReSharper code formatting and inspections
- **`clean-up-sample-solution.ps1`** - Cleans up sample code formatting

### Orchestration

- **`orchestrate-solutions.ps1`** - Runs the complete pipeline in proper order
- **`go.ps1`** (root) - Convenience script that calls orchestrate-solutions.ps1

### Commands Index (quick)

- **Build Mississippi**: `pwsh ./eng/src/agent-scripts/build-mississippi-solution.ps1`
- **Build Samples**: `pwsh ./eng/src/agent-scripts/build-sample-solution.ps1`
- **Cleanup Mississippi**: `pwsh ./eng/src/agent-scripts/clean-up-mississippi-solution.ps1`
- **Cleanup Samples**: `pwsh ./eng/src/agent-scripts/clean-up-sample-solution.ps1`
- **Unit tests (Mississippi)**: `pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1`
- **Unit tests (Samples)**: `pwsh ./eng/src/agent-scripts/unit-test-sample-solution.ps1`
- **Mutation tests (Mississippi)**: `pwsh ./eng/src/agent-scripts/mutation-test-mississippi-solution.ps1`
- **Per-project test quality**: `pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject <Name> [-SkipMutation]`
- **Final pipeline (both solutions)**: `pwsh ./go.ps1`

## Development Workflow Pattern

When making ANY code changes, follow this strict pattern:

### 1. Build → Clean → Fix → Repeat

```powershell
# Build to check for errors/warnings
pwsh ./eng/src/agent-scripts/build-mississippi-solution.ps1

# Run cleanup to fix formatting and detect issues
pwsh ./eng/src/agent-scripts/clean-up-mississippi-solution.ps1

# Fix any errors or warnings reported
# Repeat until clean
```

### 2. Add Unit Tests

- **ALWAYS add unit tests** for new functionality in Mississippi solution
- **Update existing tests** when modifying behavior
- **Sample projects** only need minimal unit tests to demonstrate testing patterns
- Aim for comprehensive test coverage of new code paths in Mississippi projects

### 3. Run Tests

```powershell
# Run unit tests to ensure functionality
pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1

# Run mutation tests for quality validation (Mississippi ONLY)
pwsh ./eng/src/agent-scripts/mutation-test-mississippi-solution.ps1
```

#### Per-project quick quality check (recommended during iteration)

```powershell
# Tests + coverage only (fast)
pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject Core.Abstractions.Tests -SkipMutation

# Tests + coverage + Stryker mutation score
pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject Core.Abstractions.Tests

# If the source project cannot be inferred from <ProjectReference>
pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject Core.Abstractions.Tests -SourceProject ./src/Core.Abstractions/Core.Abstractions.csproj
```

Notes:

- `-TestProject` accepts a test project name (one test project per assembly) or a path to the `.csproj`/directory.
- Exit code is non-zero if tests fail or Stryker breaks thresholds.

### 4. Final Validation

```powershell
# Run the complete pipeline for both solutions
pwsh ./go.ps1
```

## CI/CD Integration

The CI system runs multiple parallel workflows that enforce these quality standards:

### GitHub Actions Workflows

- **`full-build.yml`** - Builds both solutions with `--warnaserror`
- **`unit-tests.yml`** - Executes all unit tests
- **`stryker.yml`** - Runs mutation testing
- **`cleanup.yml`** - Validates ReSharper code cleanup
- **`sonar-cloud.yml`** - Performs static code analysis

### CI Failure Causes

The CI will **fail** if any of these conditions occur:

- Compiler warnings or errors
- Unit test failures
- Mutation score below threshold
- ReSharper cleanup detects issues
- SonarCloud quality gate failures

### Success Criteria

The CI considers code **ready for merge** when:
✅ Clean build with zero warnings
✅ All unit tests pass
✅ Mutation score meets requirements
✅ Code cleanup passes without changes needed
✅ SonarCloud quality gate passes

## Code Change Requirements

When modifying code, you MUST:

1. **Build and fix all warnings/errors**
2. **Run cleanup scripts and resolve any issues**
3. **Add or update unit tests for all changes** (comprehensive for Mississippi, minimal examples for samples)
4. **Ensure mutation testing maintains high scores** (Mississippi solution only)
5. **Run `pwsh ./go.ps1` to validate the complete pipeline passes**

This process should be repeated iteratively until all quality gates pass cleanly. The `go.ps1` script orchestrates the entire build-test-quality pipeline and ensures both solutions meet their respective quality standards.

## Tools Required

- **.NET SDK** (version specified in `global.json`)
- **PowerShell 7+** (`pwsh`)
- **Local tools** (installed via `dotnet tool restore`):
  - GitVersion
  - SLNGen
  - JetBrains ReSharper CLI
  - Stryker.NET

Run `dotnet tool restore` from the repository root to install all required tools.

## Related Guidelines

This document should be read in conjunction with:

- **C# General Development Best Practices** (`.github/instructions/csharp.instructions.md`) - For access control analyzer rules and code quality standards that are enforced by the build pipeline
- **Service Registration and Configuration** (`.github/instructions/service-registration.instructions.md`) - For quality standards on service registration methods and configuration validation
- **Logging Rules** (`.github/instructions/logging-rules.instructions.md`) - For LoggerExtensions pattern compliance and high-performance logging standards enforced by analyzers
- **Orleans Best Practices** (`.github/instructions/orleans.instructions.md`) - For Orleans analyzer compliance and POCO grain pattern enforcement
- **Orleans Serialization** (`.github/instructions/orleans-serialization.instructions.md`) - For Orleans serialization analyzer rules and build-time validation
- **Naming Conventions** (`.github/instructions/naming.instructions.md`) - For StyleCop analyzer enforcement and naming violation fixes
- **Project File Management** (`.github/instructions/projects.instructions.md`) - For centralized package management compliance and project file validation
- **Testing Strategy and Quality Gates** (`.github/instructions/testing.instructions.md`) - For L0–L4 testing levels, coverage targets, mutation testing, and CI expectations
- **Agent Scratchpad** (`.github/instructions/agent-scratchpad.instructions.md`) - Standard backlog slicing and agent handoff using `.scratchpad/tasks` for large fix lists
