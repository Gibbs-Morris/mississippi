---
applyTo: '**/*.ps*'
---

# PowerShell Scripting Best Practices

This document codifies our baseline expectations for any PowerShell script or module that ships with the Mississippi repository. Follow these rules to keep scripts predictable, composable, and aligned with the engineering standards we apply to C# code.

## At-a-Glance Quick-Start

- Start every script with `#!/usr/bin/env pwsh`, `Set-StrictMode -Version Latest`, and `$ErrorActionPreference = 'Stop'`.
- Declare `[CmdletBinding()]` and a typed `param(...)` block; favor explicit defaults and validation attributes.
- Import shared helpers from `eng/src/agent-scripts/RepositoryAutomation.psm1` instead of re-implementing repository plumbing.
- Validate changes with `pwsh ./eng/tests/orchestrate-powershell-tests.ps1` (use `-PassThru` for a programmatic summary).
- Prefer idempotent, cross-platform actions and emit deterministic exit codes (`0` success, non-zero failure).

> **Drift check:** Before citing a helper script, open it under `eng/src/agent-scripts/` (or `eng/tests/agent-scripts/`) to confirm parameters, behavior, and exit codes. Scripts remain the source of truth.

## Core Principles

- **Keep functions single-purpose** — mirror SOLID by decomposing scripts into small, testable functions.
- **Fail fast and loudly** — stop on the first error, surface actionable context, and avoid silent fallbacks.
- **Stay cross-platform** — use built-in cmdlets (`Join-Path`, `Resolve-Path`, `Test-Path`) instead of OS-specific path math.
- **Reuse shared automation** — centralize repository-aware logic in modules, not ad-hoc script copies.
- **Produce machine-friendly output** — pair human-readable status lines with structured summaries that other tooling can parse.

## Script Structure and Layout

1. Begin with the shebang to guarantee pwsh execution on every platform.
2. Add `[CmdletBinding()]` for advanced function semantics, common parameters, and consistent error behavior.
3. Keep the `param(...)` block at the top; annotate every parameter with types, defaults, and validators where practical.
4. Immediately enable strict mode and stop-on-error preferences. Never relax them.
5. Use `try { ... } catch { ...; exit 1 }` around top-level orchestration while keeping helper functions pure.
6. Return from functions explicitly; avoid reliance on PowerShell's implicit output pipelines for complex objects.
7. Place helper functions above the main execution path so readers see the contract before consumption.

**Why:** A consistent template shortens review time, prevents implicit output bugs, and mirrors the “constructor + property injection” patterns in our C# codebase.

## Parameters, Output, and Exit Codes

- Mark parameters as mandatory whenever omission would create undefined behavior. Use `[ValidateNotNullOrEmpty()]` and `[ValidateSet(...)]` to enforce intent.
- Expose switches for boolean toggles; avoid string flags like `"yes"`/`"no"`.
- Emit status with `Write-Host` (ansi colors optional), but return structured data with `Write-Output` or `[pscustomobject]` when other automation consumes the result.
- Summarize long-running operations using stable key/value pairs (`RESULT: PASS`, `TEST_TOTAL: 42`) to support LLM parsing and CI logs.
- Always call `exit 0` on success and `exit 1` (or higher) on failure; never rely on implicit exit codes.

**Why:** Consistent parameter contracts and exit codes mirror the explicit method signatures and return types we expect in C# services.

## Error Handling and Resilience

- Set `$ErrorActionPreference = 'Stop'` so non-terminating cmdlets become terminating.
- Wrap risky operations in `try/catch`, log context with `Write-Error`, then rethrow or exit to avoid hiding failures.
- Use `throw` without arguments to preserve the original stack, mirroring our C# exception rethrow rules.
- Prefer guard clauses over nested `if` blocks. Validate inputs as soon as they arrive.
- Do not swallow errors in helper functions; bubble them up and let the caller decide.

**Why:** Early, consistent failure keeps pipelines red when they should be red and prevents “half-success” states.

## Modules and Shared Helpers

- Consolidate reusable logic into `.psm1` modules (e.g., `RepositoryAutomation.psm1`) and import them with explicit paths from `$PSScriptRoot`.
- Use `Get-RepositoryRoot`, `Invoke-MississippiSolutionBuild`, and other shared helpers rather than shelling out or duplicating logic.
- Export nouns with approved prefixes (`Get-`, `Invoke-`, `Set-`). Avoid verb collisions and follow Microsoft’s approved verb list.
- Keep module scope clean: no global variables, no script-scoped state beyond intentional caches.

**Why:** Centralized helpers reduce drift, just like shared services and abstractions in our C# solution.

## Testing and Validation

- Add or update Pester suites under `eng/tests/agent-scripts/` whenever you introduce new script behavior; keep tests deterministic and isolated.
- Run `pwsh ./eng/tests/orchestrate-powershell-tests.ps1` locally before committing. Expect `RESULT: SUCCESS` on a clean run.
- When scripts generate scratchpad artifacts, ensure `.gitignore` already excludes them or document cleanup steps in the script summary.
- For scripts that wrap .NET builds/tests, mirror the zero-warning policy: fail the script if the underlying command emits warnings treated as errors in CI.

**Why:** Tests provide the same safety net for automation that unit tests provide for C# code. A failing script should block merges just like a failing build.

## Examples

### Minimal Agent Script Template

```powershell
#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$TaskId
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$automationModule = Join-Path $PSScriptRoot 'RepositoryAutomation.psm1'
Import-Module -Name $automationModule -Force

try {
    $repoRoot = Get-RepositoryRoot -StartPath $PSScriptRoot
    Invoke-TaskAutomation -RepoRoot $repoRoot -TaskId $TaskId
    Write-Host 'RESULT: SUCCESS' -ForegroundColor Green
    exit 0
}
catch {
    Write-Error "RESULT: FAIL :: $($_.Exception.Message)"
    exit 1
}
```

### Structured Summary Pattern

```powershell
Write-Host ''
Write-Host "=== QUALITY SUMMARY ($ProjectName) ===" -ForegroundColor Yellow
Write-Host ("RESULT: {0}" -f $resultFlag)
Write-Host ("TEST_TOTAL: {0}" -f $summary.Total)
Write-Host ("COVERAGE: {0}%" -f $coverage)
```

**Why:** These snippets mirror the conventions already used in `test-project-quality.ps1` and other automation scripts, making outputs predictable for reviewers and tooling.

## Anti-Patterns to Avoid

- ❌ Omitting strict mode or stop-on-error. ✅ Always enable both at the top of the script.
- ❌ Hard-coding repository-relative paths. ✅ Use `Get-RepositoryRoot`, `$PSScriptRoot`, and `Join-Path`.
- ❌ Swallowing exceptions or returning partial success without context. ✅ Surface failures with meaningful messages and non-zero exit codes.
- ❌ Mixing data and presentation output. ✅ Separate machine-readable data from human-facing status lines.
- ❌ Adding third-party PowerShell modules ad hoc. ✅ Propose them first; prefer built-in cmdlets and existing helper modules.

## Related Guidelines

This document should be read alongside:

- **Build Rules and Quality Standards** (`./build-rules.instructions.md`) — automation scripts must uphold the zero-warning policy and pipeline gates.
- **Testing Strategy and Quality Gates** (`./testing.instructions.md`) — replicate test rigor when scripts orchestrate .NET builds or tests.
- **Instruction Authoring Guide** (`./authoring.instructions.md`) — follow the standard template and sync policy when updating this file.
- **Mutation Testing Playbook** (`./mutation-testing.instructions.md`) — align script output with mutation-test expectations when orchestrating Stryker runs.

## References

- [Approved PowerShell verbs](https://learn.microsoft.com/powershell/scripting/learn/deep-dives/everything-about-verbs)
- [Set-StrictMode documentation](https://learn.microsoft.com/powershell/module/microsoft.powershell.core/set-strictmode)
- [Writing cross-platform PowerShell](https://learn.microsoft.com/powershell/scripting/learn/experimental-features?view=powershell-7.4)

---

**Last verified:** 2025-10-05
**Default branch:** main
