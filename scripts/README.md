# Scripts Guide

This folder groups together all PowerShell helper scripts that automate **build–test–quality** tasks for the repository.  
They are written for **PowerShell 7+ (`pwsh`)** and rely on the .NET SDK and several local tools restored via `dotnet tool restore` (e.g. **GitVersion**, **SLNGen**, **JetBrains ReSharper CLI**, **Stryker**).

## Prerequisites

• **.NET SDK specified in `global.json`** (currently `9.0.301`) must be available on the `PATH`; the .NET CLI automatically honours this file.  
• **PowerShell 7+** (`pwsh`)—all scripts use the she-bang `#!/usr/bin/env pwsh`.  
• No global tool installs required – `dotnet tool restore` brings:
  – GitVersion
  – SLNGen
  – JetBrains ReSharper command-line
  – Stryker.NET

---
## Recommended development loop

1. **Add a failing test** in the relevant *tests/* project (start with Mississippi).
2. **`pwsh ./scripts/unit-test-mississippi-solution.ps1`**  
   Confirm the new test fails (red).
3. **Write production code** to make the test pass.
4. **Run the unit-test script again** until all tests are green.
5. **(Optional) `pwsh ./scripts/mutation-test-mississippi-solution.ps1`** to validate the quality of your tests.
6. **`pwsh ./scripts/build-mississippi-solution.ps1`** to compile everything in Release mode.
7. **Repeat** the cycle.

Once the **Mississippi** solution is healthy, run the equivalent *sample* scripts to ensure the publicly-documented usage scenarios still work.  Mutation testing is **not** executed for samples.

> ℹ️  The catch-all `orchestrate-solutions.ps1` script wires the steps together in the correct order—useful for CI or a final local check.

---
## Script catalogue

| Script | Purpose | Typical call |
|-------|---------|--------------|
| **build-mississippi-solution.ps1** | Restore dependencies and compile `mississippi.slnx` in the chosen configuration (default `Release`). | `pwsh ./scripts/build-mississippi-solution.ps1 -Configuration Debug` |
| **unit-test-mississippi-solution.ps1** | Restore, then execute all unit & integration tests inside the Mississippi solution. Results are placed under `./test-results`. | `pwsh ./scripts/unit-test-mississippi-solution.ps1` |
| **mutation-test-mississippi-solution.ps1** | Generate `mississippi.sln` via **SLNGen**, then run **Stryker.NET** mutation analysis to measure test robustness. | `pwsh ./scripts/mutation-test-mississippi-solution.ps1` |
| **clean-up-mississippi-solution.ps1** | Generate a temporary `.sln`, then apply **ReSharper CleanupCode** with the repo-wide settings for formatting, ordering and inspections. Run before committing. | `pwsh ./scripts/clean-up-mississippi-solution.ps1` |
| **build-sample-solution.ps1** | Build the sample applications contained in `samples.slnx`. | `pwsh ./scripts/build-sample-solution.ps1` |
| **unit-test-sample-solution.ps1** | Execute the tests that accompany the samples—no mutation testing here. | `pwsh ./scripts/unit-test-sample-solution.ps1` |
| **clean-up-sample-solution.ps1** | ReSharper-based formatting pass on the sample code. | `pwsh ./scripts/clean-up-sample-solution.ps1` |
| **final-build-solutions.ps1** | Performs a strict build of *both* solutions with `--warnaserror`, ensuring zero compiler warnings before merge. | `pwsh ./scripts/final-build-solutions.ps1` |
| **orchestrate-solutions.ps1** | High-level pipeline that invokes the scripts above in this order:  
  1. Mississippi – build → test → mutate → cleanup  
  2. Sample – build → test → cleanup  
  3. Final build (warnings as errors)  
  The script aborts on the first failing step (non-zero exit code). | `pwsh ./scripts/orchestrate-solutions.ps1` |

---
## Example invocations

Windows / PowerShell:
```pwsh
pwsh ./scripts/unit-test-mississippi-solution.ps1 -Configuration Debug
pwsh ./scripts/final-build-solutions.ps1            # Release by default
```

Bash (if PowerShell 7 is installed):
```bash
pwsh ./scripts/orchestrate-solutions.ps1 | tee orchestration.log
```

## Output locations

| Artifact | Path |
|----------|------|
| Test results (TRX) | `./test-results` |
| Mutation reports   | `./StrykerOutput/<timestamp>` |

These folders are git-ignored but preserved between steps; feel free to parse or archive them in CI.

---
## Guidelines for AI agents

• **Prefer the smallest scoped script** needed for the task—don't run the full orchestrator if you only added a quick unit test.  
• **Fail fast**: scripts exit on errors thanks to `Set-StrictMode -Version Latest` and `$ErrorActionPreference = "Stop"`. Watch the exit code.  
• **Stay on Mississippi first**: always ensure the core library remains green before touching samples.  
• **Skip mutation tests on samples**: they purposefully illustrate usage, not coverage quality.  
• **Run `clean-up` scripts before committing** to keep the codebase consistent with the team's ReSharper rules.  
• **Cache the `.config/dotnet-tools.json` folder** between runs (if you are orchestrating inside CI) to speed up tool restoration.
• **Zero-warning policy**: `final-build-solutions.ps1` (automatically triggered at the end of the orchestrator) treats **all warnings as errors**.  Merges to *main* must pass this gate.
• **Return-code contract**: every script exits **non-zero** on any error; the orchestrator stops at the first failing step. Check `$LASTEXITCODE`.
• **Output folders documented above** are stable—agents can consume them.
• **CI-parity script**: `orchestrate-solutions.ps1` reproduces the same steps our GitHub Actions pipeline runs; execute it locally (e.g., from Cursor or GitHub Copilot) to verify changes before opening a PR.

Happy building! :rocket: 