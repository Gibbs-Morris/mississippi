---
applyTo: '**'
---

# Build Rules and Quality Standards

This project maintains **zero tolerance for warnings** and requires comprehensive test coverage. All code changes must pass the full quality pipeline before being considered complete.

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

The `scripts/` folder contains PowerShell automation for the build-test-quality pipeline:

### Core Build Scripts
- **`build-mississippi-solution.ps1`** - Compiles the main Mississippi solution in Release mode
- **`build-sample-solution.ps1`** - Builds sample applications demonstrating library usage
- **`final-build-solutions.ps1`** - Strict build with `--warnaserror` for both solutions

### Testing Scripts
- **`unit-test-mississippi-solution.ps1`** - Executes all unit and integration tests for Mississippi
- **`unit-test-sample-solution.ps1`** - Runs minimal tests for sample applications (examples only)
- **`mutation-test-mississippi-solution.ps1`** - Performs mutation testing with Stryker.NET (Mississippi ONLY)

### Quality Scripts
- **`clean-up-mississippi-solution.ps1`** - Applies ReSharper code formatting and inspections
- **`clean-up-sample-solution.ps1`** - Cleans up sample code formatting

### Orchestration
- **`orchestrate-solutions.ps1`** - Runs the complete pipeline in proper order
- **`go.ps1`** (root) - Convenience script that calls orchestrate-solutions.ps1

## Development Workflow Pattern

When making ANY code changes, follow this strict pattern:

### 1. Build → Clean → Fix → Repeat
```powershell
# Build to check for errors/warnings
pwsh ./scripts/build-mississippi-solution.ps1

# Run cleanup to fix formatting and detect issues
pwsh ./scripts/clean-up-mississippi-solution.ps1

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
pwsh ./scripts/unit-test-mississippi-solution.ps1

# Run mutation tests for quality validation (Mississippi ONLY)
pwsh ./scripts/mutation-test-mississippi-solution.ps1
```

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