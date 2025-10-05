# Agent Scripts Guide

`eng/src/agent-scripts` hosts the PowerShell helpers that automate **build → test → quality** workflows for the Mississippi repository. The scripts target **PowerShell 7+** (`pwsh`) and the .NET SDK declared in `global.json`, restoring any additional tools (GitVersion, SLNGen, JetBrains ReSharper CLI, Stryker.NET) via `dotnet tool restore`.

## Prerequisites

- Install the .NET SDK referenced in `global.json` (currently `9.0.301`).
- Use PowerShell 7+; every script starts with `#!/usr/bin/env pwsh`.
- Run `dotnet tool restore` from the repo root to provision the required local tools.

---

## Recommended development loop

1. Add or adjust a test in the corresponding `tests/` project (start with Mississippi).
2. `pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1` and watch it fail (red).
3. Implement the production change that makes the test pass.
4. Re-run the unit-test script until it is green.
5. Optionally run `pwsh ./eng/src/agent-scripts/mutation-test-mississippi-solution.ps1` to validate test quality.
6. `pwsh ./eng/src/agent-scripts/build-mississippi-solution.ps1` to confirm a clean Release build.
7. Repeat until the feature is complete, then mirror the steps on the Samples solution when relevant (no mutation testing there).

> ℹ️  `pwsh ./eng/src/agent-scripts/orchestrate-solutions.ps1` stitches the full CI-equivalent pipeline together—including the coverage and mutation summarizers that refresh `.scratchpad/tasks`—handy before pushing or in automation.

---

## Script catalogue

| Script | Purpose | Typical call |
| --- | --- | --- |
| **build-mississippi-solution.ps1** | Restore dependencies and compile `mississippi.slnx` (default `Release`). | `pwsh ./eng/src/agent-scripts/build-mississippi-solution.ps1 -Configuration Debug` |
| **unit-test-mississippi-solution.ps1** | Run all unit & integration tests for the Mississippi solution, emitting results under `.scratchpad/coverage-test-results`. | `pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1` |
| **mutation-test-mississippi-solution.ps1** | Generate `mississippi.sln` with SLNGen and execute Stryker.NET to measure mutation score. | `pwsh ./eng/src/agent-scripts/mutation-test-mississippi-solution.ps1` |
| **test-project-quality.ps1** | Run `dotnet test` (with coverage) for a single project and optionally Stryker; prints a machine-readable summary. | `pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject Core.Abstractions.Tests` |
| **clean-up-mississippi-solution.ps1** | Produce a temporary `.sln` and run ReSharper CleanupCode using repository settings. | `pwsh ./eng/src/agent-scripts/clean-up-mississippi-solution.ps1` |
| **build-sample-solution.ps1** | Build `samples.slnx`. | `pwsh ./eng/src/agent-scripts/build-sample-solution.ps1` |
| **unit-test-sample-solution.ps1** | Execute sample solution tests (no mutation testing). | `pwsh ./eng/src/agent-scripts/unit-test-sample-solution.ps1` |
| **clean-up-sample-solution.ps1** | Run ReSharper cleanup over the sample projects. | `pwsh ./eng/src/agent-scripts/clean-up-sample-solution.ps1` |
| **summarize-coverage-gaps.ps1** | Merge Cobertura coverage reports and emit `.scratchpad/tasks` entries for low-coverage files. | `pwsh ./eng/src/agent-scripts/summarize-coverage-gaps.ps1 -EmitTasks` |
| **summarize-mutation-survivors.ps1** | Parse the latest Stryker run (or rerun it) and sync survivor tasks into `.scratchpad/tasks`. | `pwsh ./eng/src/agent-scripts/summarize-mutation-survivors.ps1 -SkipMutationRun -GenerateTasks` |
| **final-build-solutions.ps1** | Build both solutions with `--warnaserror` as the final zero-warning gate. | `pwsh ./eng/src/agent-scripts/final-build-solutions.ps1` |
| **orchestrate-solutions.ps1** | Run the full Mississippi + Samples pipeline (build → test → mutate → summarize → cleanup → final build) and keep `.scratchpad/tasks` refreshed. | `pwsh ./eng/src/agent-scripts/orchestrate-solutions.ps1` |
| **sync-instructions-to-mdc.ps1** | Mirror `.github/instructions/*.instructions.md` into `.cursor/rules/*.mdc` with sync metadata. | `pwsh ./eng/src/agent-scripts/sync-instructions-to-mdc.ps1` |

### Scratchpad task helpers

| Script | Purpose | Typical call |
| --- | --- | --- |
| **tasks/new-scratchpad-task.ps1** | Create a normalized JSON task under `.scratchpad/tasks/pending`. | `pwsh ./eng/src/agent-scripts/tasks/new-scratchpad-task.ps1 -Title "Fix analyzer" -Priority P1` |
| **tasks/list-scratchpad-tasks.ps1** | Enumerate scratchpad tasks with optional filters. | `pwsh ./eng/src/agent-scripts/tasks/list-scratchpad-tasks.ps1 -Status pending -Tag build` |
| **tasks/claim-scratchpad-task.ps1** | Atomically move a pending task to `claimed/` and stamp ownership. | `pwsh ./eng/src/agent-scripts/tasks/claim-scratchpad-task.ps1 -Id <task-id> -Agent my-handle` |
| **tasks/complete-scratchpad-task.ps1** | Mark a claimed task as done with a result summary. | `pwsh ./eng/src/agent-scripts/tasks/complete-scratchpad-task.ps1 -Id <task-id> -Result "Analyzer warnings resolved"` |
| **tasks/defer-scratchpad-task.ps1** | Defer a task with reason and next steps. | `pwsh ./eng/src/agent-scripts/tasks/defer-scratchpad-task.ps1 -Id <task-id> -Reason "Waiting on upstream"` |

The supporting Pester harness lives in `eng/tests/agent-scripts/`:

| Script | Purpose |
| --- | --- |
| **run-scratchpad-task-tests.ps1** | Runs the Pester suite that covers the scratchpad helpers. |
| **verify-scratchpad-task-scripts.ps1** | End-to-end flow that creates → claims → completes/defers tasks using a temporary scratchpad. |
| (orchestrator) `../orchestrate-powershell-tests.ps1` | Runs all PowerShell test suites (Pester and script e2e) and exits non-zero on failure. |

---

## Example invocations

Windows / PowerShell:

```pwsh
pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1 -Configuration Debug
pwsh ./eng/src/agent-scripts/final-build-solutions.ps1            # Release by default
pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject Core.Abstractions.Tests -SkipMutation   # fast test+coverage
pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject Core.Abstractions.Tests                  # include mutation
pwsh ./eng/tests/orchestrate-powershell-tests.ps1                 # run all PowerShell test suites
```

Bash (with PowerShell 7 installed):

```bash
pwsh ./eng/src/agent-scripts/orchestrate-solutions.ps1 | tee orchestration.log
```

## Output locations

| Artifact | Path |
| --- | --- |
| Test results (TRX) | `.scratchpad/coverage-test-results` |
| Mutation reports | `.scratchpad/mutation-test-results/<timestamp>` |

These folders are git-ignored but persist across runs for inspection or archiving.

---

## Code cleanup: Local vs CI

The repository has both local cleanup scripts and CI workflows to maintain code formatting:

### Local cleanup

**Root-level convenience script** (recommended):
```pwsh
pwsh ./clean-up.ps1
```
This deterministic script uses the `RepositoryAutomation.psm1` module and works from any location.

**Individual solution cleanup**:
```pwsh
pwsh ./eng/src/agent-scripts/clean-up-mississippi-solution.ps1
pwsh ./eng/src/agent-scripts/clean-up-sample-solution.ps1
```

### CI workflows

| Workflow | Type | When it runs | What it does |
| --- | --- | --- | --- |
| **cleanup.yml** | Validation | Automatic (PR checks) | Runs cleanup and **fails** if changes detected. Ensures code is properly formatted before merge. |
| **auto-cleanup.yml** | Automation | Manual dispatch | Runs cleanup and **creates a PR** with changes. Use this to fix cleanup issues automatically. |

**Using auto-cleanup workflow:**
1. Go to Actions → "Auto Cleanup (Create PR)"
2. Click "Run workflow"
3. Optionally specify target branch (defaults to current)
4. Workflow creates a PR with cleanup changes if any exist

**Determinism fix:**
The cleanup now includes a `GlobalUsings.cs` file in `EventSourcing.Tests` to prevent ReSharper from incorrectly removing the `Microsoft.Extensions.DependencyInjection` using statement. This ensures consistent results between local runs and CI.

---

## Guidelines for AI agents

- Prefer the narrowest script for the job; avoid the orchestrator unless you need the full pipeline.
- Scripts use `Set-StrictMode -Version Latest` and `$ErrorActionPreference = 'Stop'`; check `$LASTEXITCODE` for failures.
- Keep the Mississippi solution green before touching samples.
- Skip mutation testing for samples—it's intentionally omitted.
- Run the cleanup scripts before committing so code matches ReSharper formatting and analyzer expectations.
- Cache `.config/dotnet-tools.json` across CI runs to speed up tool restoration.
- `final-build-solutions.ps1` enforces the zero-warning policy via `--warnaserror`.
- Output folders listed above are stable and safe for automation.
- The orchestrator mirrors the GitHub Actions pipeline—use it locally before opening a PR.

---

## Syncing instruction files to Cursor rules (`.mdc`)

When changing `.github/instructions/*.instructions.md`, run the helper to keep Cursor rules in sync:

```pwsh
pwsh ./eng/src/agent-scripts/sync-instructions-to-mdc.ps1
```

If the script is unavailable, follow the manual workflow in the Instruction ↔ Cursor MDC Sync Policy and replicate the sync metadata by hand.

## `test-project-quality.ps1`

Purpose: quickly evaluate a single test project's health by running `dotnet test` with coverage and (optionally) Stryker mutation testing. The script prints a concise summary consumed by automation tools.

```pwsh
# Fast: tests + coverage only
pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject Core.Abstractions.Tests -SkipMutation

# Full: tests + coverage + mutation
pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject Core.Abstractions.Tests

# If inference fails or multiple <ProjectReference>s exist, specify the source project explicitly
pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject Core.Abstractions.Tests -SourceProject ./src/Core.Abstractions/Core.Abstractions.csproj
```

### Parameters

- `-TestProject <name|path>`: required; supply the test project name or path.
- `-SkipMutation`: optional; skip Stryker for a faster loop.
- `-Configuration <Debug|Release>`: optional; defaults to `Release`.
- `-SourceProject <path>`: optional; override inferred source project when needed.
- `-NoBuild`: optional; pass-through to `dotnet test`.

### Summary output

```text
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

### Artifacts

- Coverage artifacts under `.scratchpad/coverage-test-results/<TestProjectName>/`.
- Mutation reports under `.scratchpad/mutation-test-results/<timestamp>/reports/`.

Exit code is `0` on success (including mutation thresholds) and `1` otherwise.

Happy building! 🚀

### RepositoryAutomation module

All command-line scripts in this folder are thin shims over the shared PowerShell module `RepositoryAutomation.psm1`. The module exposes advanced functions for build/test/cleanup orchestration so automation can be reused from other scripts, Pester tests, and CI workflows without spawning nested shells.

| Function | Responsibility |
| --- | --- |
| `Get-RepositoryRoot` | Resolve the repo root by walking upward from any start path (used by all scripts and tests). |
| `Write-AutomationBanner`, `Invoke-AutomationStep` | Standardise coloured logging, step numbering, and error handling across every script. |
| `Invoke-DotnetToolRestore`, `Invoke-SolutionRestore`, `Invoke-SolutionBuild`, `Invoke-SolutionTests` | Shared wrappers around `dotnet` CLI commands with consistent arguments, messaging, and metadata. |
| `Invoke-SlnGeneration`, `Invoke-ReSharperCleanup`, `Invoke-StrykerMutationTest` | Encapsulate SLNGen, ReSharper CleanupCode, and Stryker.NET flows. |
| `Invoke-MississippiSolution*`, `Invoke-SampleSolution*`, `Invoke-FinalSolutionsBuild`, `Invoke-SolutionsPipeline` | Canonical entry points that the `.ps1` shims expose on the command line. |

### How to extend automation

1. Add the new behaviour as an advanced function inside `RepositoryAutomation.psm1` and export it.
2. Create a thin `#!/usr/bin/env pwsh` wrapper only when a standalone CLI entry point is needed—the wrapper should import the module, call the function, and surface friendly errors.
3. Cover the logic with Pester (see `eng/tests/agent-scripts/RepositoryAutomation.Tests.ps1` for examples) and wire the suite into `eng/tests/orchestrate-powershell-tests.ps1` so CI runs it.

> Tip: when experimenting interactively you can `Import-Module ./eng/src/agent-scripts/RepositoryAutomation.psm1 -Force` and call the functions directly (for example `Invoke-MississippiSolutionBuild -Configuration Debug`).
