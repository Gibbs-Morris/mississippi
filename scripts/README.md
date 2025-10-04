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
| **test-project-quality.ps1** | Run tests with coverage for a single test project and (optionally) run Stryker to compute a mutation score. Prints a concise, machine-readable summary for LLMs. | `pwsh ./scripts/test-project-quality.ps1 -TestProject Core.Abstractions.Tests` |
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
| **sync-instructions-to-mdc.ps1** | Sync `.github/instructions/*.instructions.md` files into Cursor `.mdc` rule files and add sync metadata automatically. Preferred automated way to keep instruction Markdown and `.mdc` files in parity. | `pwsh ./scripts/sync-instructions-to-mdc.ps1` |

---

### Scratchpad task helpers

| Script | Purpose | Typical call |
| --- | --- | --- |
| **tasks/new-scratchpad-task.ps1** | Create a JSON task file in `.scratchpad/tasks/pending` with normalized metadata. | `pwsh ./scripts/tasks/new-scratchpad-task.ps1 -Title "Fix analyzer" -Priority P1` |
| **tasks/list-scratchpad-tasks.ps1** | Inspect scratchpad tasks across statuses with optional filters. | `pwsh ./scripts/tasks/list-scratchpad-tasks.ps1 -Status pending -Tag build` |
| **tasks/claim-scratchpad-task.ps1** | Atomically move a pending task into `claimed/` and stamp ownership/attempt counters. | `pwsh ./scripts/tasks/claim-scratchpad-task.ps1 -Id <task-id> -Agent my-handle` |
| **tasks/complete-scratchpad-task.ps1** | Mark a claimed task as done with a completion summary and move to `done/`. | `pwsh ./scripts/tasks/complete-scratchpad-task.ps1 -Id <task-id> -Result "Analyzer warnings resolved"` |
| **tasks/defer-scratchpad-task.ps1** | Defer a pending or claimed task, recording reason and follow-up notes. | `pwsh ./scripts/tasks/defer-scratchpad-task.ps1 -Id <task-id> -Reason "Waiting on upstream"` |

Supporting test harness lives under `scripts-tests/`:

| Script | Purpose |
| --- | --- |
| **scripts-tests/run-scratchpad-task-tests.ps1** | Runs the Pester suite covering the scratchpad task helpers. |
| **scripts-tests/verify-scratchpad-task-scripts.ps1** | End-to-end smoke flow that exercises create → claim → complete/defer using a temporary scratchpad and cleans up. |

## Example invocations

Windows / PowerShell:

```pwsh
pwsh ./scripts/unit-test-mississippi-solution.ps1 -Configuration Debug
pwsh ./scripts/final-build-solutions.ps1            # Release by default
pwsh ./scripts/test-project-quality.ps1 -TestProject Core.Abstractions.Tests -SkipMutation   # fast test+coverage
pwsh ./scripts/test-project-quality.ps1 -TestProject Core.Abstractions.Tests                  # include mutation
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

---

## Syncing instruction files to Cursor rules (.mdc)

When you change files under `.github/instructions/` it's preferred to use the automated helper to mirror those changes into Cursor `.mdc` rule files and to populate the sync metadata header/footer.

Preferred: run the script which performs the mirror and metadata update for you:

```pwsh
pwsh ./scripts/sync-instructions-to-mdc.ps1
```

If the script cannot be used (e.g., on a constrained environment), follow the manual workflow described in the `Instruction ↔ Cursor MDC Sync Policy` document and ensure you add the sync metadata in both files.

## test-project-quality.ps1

Purpose: Quickly evaluate a single test project’s quality by running `dotnet test` with code coverage and (optionally) Stryker mutation testing. The script prints a compact summary that tools like Cursor or Copilot can parse.

Usage:

```pwsh
# Fast: tests + coverage only
pwsh ./scripts/test-project-quality.ps1 -TestProject Core.Abstractions.Tests -SkipMutation

# Full: tests + coverage + mutation
pwsh ./scripts/test-project-quality.ps1 -TestProject Core.Abstractions.Tests

# If inference fails or multiple <ProjectReference>s exist, provide the source project explicitly
pwsh ./scripts/test-project-quality.ps1 -TestProject Core.Abstractions.Tests -SourceProject ./src/Core.Abstractions/Core.Abstractions.csproj
```

Parameters:

- `-TestProject <name|path>`: Required. Accepts the test project name (e.g., `Core.Abstractions.Tests`) or path to the `.csproj`/directory. Convention is one test project per source assembly.
- `-SkipMutation`: Optional. When set, skips Stryker to speed up feedback.
- `-Configuration <Debug|Release>`: Optional. Defaults to `Release`.
- `-SourceProject <path>`: Optional. Overrides inferred source project when the test `.csproj` references multiple projects.
- `-NoBuild`: Optional. Passes `--no-build` to `dotnet test`.

Summary output (machine-readable):

```
=== QUALITY SUMMARY (<TestProjectName>) ===
RESULT: PASS|FAIL
TEST_TOTAL: <n>
TEST_PASSED: <n>
TEST_FAILED: <n>
TEST_SKIPPED: <n>
COVERAGE: <percent>%
MUTATION_SCORE: <percent>%|N/A
MUTATION_RESULT: PASS|FAIL
```

Artifacts:

- TRX and Cobertura files under `./test-results/<TestProjectName>/`
- Stryker reports under `./StrykerOutput/<timestamp>/reports/`

Exit codes:

- `0` when tests pass and (if run) mutation completes without breaking thresholds
- `1` on any error, test failure, or Stryker failure/break

Happy building! :rocket:
